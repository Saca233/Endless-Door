using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OwariNakiTobira.Editor
{
    public static class UtilityWindowDraggingFixMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string UtilityWindowName = "UtilityWindow";
        private const string MainGameWindowName = "MainGameWindow";
        private const string UtilityRawImageName = "UtilityGameRawImage";
        private const string DesktopWindowLayerName = "DesktopWindowLayer";

        [MenuItem("Tools/OwariNakiTobira/Fix Utility Window Dragging")]
        public static void FixUtilityWindowDragging()
        {
            if (!TryOpenScene(out Scene scene))
            {
                return;
            }

            List<string> repairs = new List<string>();
            List<string> errors = new List<string>();
            if (!RepairScene(scene, repairs, errors))
            {
                LogErrors(errors);
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            if (repairs.Count == 0)
            {
                Debug.Log("[UtilityWindow Dragging] No repair changes were needed.");
            }
            else
            {
                Debug.Log("[UtilityWindow Dragging] Repaired:\n- " + string.Join("\n- ", repairs));
            }
        }

        [MenuItem("Tools/OwariNakiTobira/Validate Utility Window Dragging")]
        public static void ValidateUtilityWindowDragging()
        {
            if (!TryOpenScene(out Scene scene))
            {
                return;
            }

            List<string> errors = new List<string>();
            ValidateScene(scene, errors);
            if (errors.Count == 0)
            {
                Debug.Log("[UtilityWindow Dragging] Validation passed.");
                return;
            }

            LogErrors(errors);
        }

        private static bool RepairScene(Scene scene, List<string> repairs, List<string> errors)
        {
            DesktopWindowController utilityWindow = FindSingleWindow(scene, UtilityWindowName, errors);
            if (utilityWindow == null)
            {
                return false;
            }

            DesktopWindowManager manager = FindSingleComponent<DesktopWindowManager>(scene, errors, "DesktopWindowManager");
            RectTransform desktopWindowLayer = FindSceneTransform(scene, DesktopWindowLayerName) as RectTransform;
            Canvas canvas = utilityWindow.GetComponentInParent<Canvas>();
            EventSystem eventSystem = FindSingleComponent<EventSystem>(scene, errors, "EventSystem");

            RepairViewReferences(utilityWindow, repairs);
            RepairControllerReferences(utilityWindow, manager, repairs);
            RepairModel(utilityWindow, repairs);
            RepairTitleBar(utilityWindow, repairs);
            RepairContentRaycasts(utilityWindow, repairs);
            RepairCanvas(canvas, repairs, errors);
            RepairEventSystem(eventSystem, repairs, errors);
            RepairManager(manager, desktopWindowLayer, repairs);
            RepairOverlayRaycasts(scene, repairs);
            RepairSiblingOrder(utilityWindow, repairs);
            ClampWindowIntoCanvas(utilityWindow, desktopWindowLayer, repairs);

            ValidateScene(scene, errors);
            return errors.Count == 0;
        }

        private static void RepairViewReferences(DesktopWindowController window, List<string> repairs)
        {
            DesktopWindowView view = window.GetComponent<DesktopWindowView>();
            if (view == null)
            {
                return;
            }

            RectTransform root = window.transform as RectTransform;
            RectTransform titleBar = FindDirectChild(window.transform, "TitleBar") as RectTransform;
            RectTransform contentRoot = FindDirectChild(window.transform, "ContentRoot") as RectTransform;
            Button closeButton = null;

            TMPro.TextMeshProUGUI title = titleBar != null ? titleBar.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) : null;
            if (titleBar != null)
            {
                closeButton = titleBar.GetComponentInChildren<Button>(true);
            }

            SerializedObject serializedObject = new SerializedObject(view);
            bool changed = false;
            changed |= SetObject(serializedObject, "windowRoot", root);
            changed |= SetObject(serializedObject, "titleBar", titleBar);
            changed |= SetObject(serializedObject, "contentRoot", contentRoot);
            changed |= SetObject(serializedObject, "titleText", title);
            changed |= SetObject(serializedObject, "closeButton", closeButton);
            changed |= EnsureBorderVisuals(serializedObject, window);
            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Repaired DesktopWindowView references");
                EditorUtility.SetDirty(view);
            }
        }

        private static void RepairControllerReferences(DesktopWindowController window, DesktopWindowManager manager, List<string> repairs)
        {
            SerializedObject serializedObject = new SerializedObject(window);
            bool changed = false;
            changed |= SetObject(serializedObject, "manager", manager);
            changed |= SetObject(serializedObject, "model", window.GetComponent<DesktopWindowModel>());
            changed |= SetObject(serializedObject, "view", window.GetComponent<DesktopWindowView>());
            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Repaired DesktopWindowController references");
                EditorUtility.SetDirty(window);
            }

            window.ClearMovementLocks();
        }

        private static void RepairModel(DesktopWindowController window, List<string> repairs)
        {
            DesktopWindowModel model = window.GetComponent<DesktopWindowModel>();
            if (model == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(model);
            bool changed = false;
            changed |= SetBool(serializedObject, "draggingAllowed", true);
            changed |= SetBool(serializedObject, "visible", true);

            RectTransform root = window.transform as RectTransform;
            if (root != null)
            {
                changed |= SetVector2(serializedObject, "currentPosition", root.anchoredPosition);
                changed |= SetVector2(serializedObject, "currentSize", root.sizeDelta);
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Enabled dragging on UtilityWindow model");
                EditorUtility.SetDirty(model);
            }
        }

        private static void RepairTitleBar(DesktopWindowController window, List<string> repairs)
        {
            DesktopWindowView view = window.GetComponent<DesktopWindowView>();
            RectTransform titleBar = view != null ? view.TitleBar : null;
            if (titleBar == null)
            {
                return;
            }

            Graphic graphic = titleBar.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = titleBar.gameObject.AddComponent<Image>();
                image.color = new Color(0.13f, 0.15f, 0.18f, 1f);
                image.raycastTarget = true;
                repairs.Add("Added raycastable Image to UtilityWindow TitleBar");
                EditorUtility.SetDirty(image);
            }
            else if (!graphic.raycastTarget)
            {
                graphic.raycastTarget = true;
                repairs.Add("Enabled TitleBar Raycast Target");
                EditorUtility.SetDirty(graphic);
            }

            Graphic[] childGraphics = titleBar.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                if (childGraphics[i] != null && childGraphics[i] != graphic && childGraphics[i].name != "CloseButton")
                {
                    childGraphics[i].raycastTarget = false;
                    EditorUtility.SetDirty(childGraphics[i]);
                }
            }
        }

        private static void RepairContentRaycasts(DesktopWindowController window, List<string> repairs)
        {
            DesktopWindowView view = window.GetComponent<DesktopWindowView>();
            if (view == null || view.ContentRoot == null)
            {
                return;
            }

            Graphic[] contentGraphics = view.ContentRoot.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < contentGraphics.Length; i++)
            {
                Graphic graphic = contentGraphics[i];
                if (graphic == null)
                {
                    continue;
                }

                bool shouldBlock = false;
                if (graphic.raycastTarget != shouldBlock)
                {
                    graphic.raycastTarget = shouldBlock;
                    repairs.Add("Disabled raycast target on " + graphic.name);
                    EditorUtility.SetDirty(graphic);
                }
            }

            RawImage utilityRawImage = FindUtilityRawImage(window);
            if (utilityRawImage != null && utilityRawImage.raycastTarget)
            {
                utilityRawImage.raycastTarget = false;
                repairs.Add("Disabled UtilityGameRawImage Raycast Target");
                EditorUtility.SetDirty(utilityRawImage);
            }
        }

        private static void RepairCanvas(Canvas canvas, List<string> repairs, List<string> errors)
        {
            if (canvas == null)
            {
                errors.Add("Missing Canvas above UtilityWindow.");
                return;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                repairs.Add("Added GraphicRaycaster to Canvas");
                EditorUtility.SetDirty(canvas.gameObject);
            }
        }

        private static void RepairEventSystem(EventSystem eventSystem, List<string> repairs, List<string> errors)
        {
            if (eventSystem == null)
            {
                errors.Add("Missing EventSystem.");
                return;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                repairs.Add("Added InputSystemUIInputModule to EventSystem");
                EditorUtility.SetDirty(eventSystem.gameObject);
            }
        }

        private static void RepairManager(DesktopWindowManager manager, RectTransform desktopWindowLayer, List<string> repairs)
        {
            if (manager == null || desktopWindowLayer == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(manager);
            if (SetObject(serializedObject, "desktopBounds", desktopWindowLayer))
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Repaired DesktopWindowManager desktop bounds");
                EditorUtility.SetDirty(manager);
            }
        }

        private static void RepairOverlayRaycasts(Scene scene, List<string> repairs)
        {
            string[] overlayNames = { "FadeLayer", "BootLayer", "LoopMenuLayer" };
            for (int i = 0; i < overlayNames.Length; i++)
            {
                Transform overlay = FindSceneTransform(scene, overlayNames[i]);
                if (overlay == null)
                {
                    continue;
                }

                CanvasGroup group = overlay.GetComponent<CanvasGroup>();
                if (group != null && group.alpha <= 0.001f && group.blocksRaycasts)
                {
                    group.blocksRaycasts = false;
                    repairs.Add("Disabled raycast blocking on hidden " + overlayNames[i]);
                    EditorUtility.SetDirty(group);
                }

                Graphic graphic = overlay.GetComponent<Graphic>();
                if (graphic != null && graphic.color.a <= 0.001f && graphic.raycastTarget)
                {
                    graphic.raycastTarget = false;
                    repairs.Add("Disabled transparent overlay raycast target on " + overlayNames[i]);
                    EditorUtility.SetDirty(graphic);
                }
            }
        }

        private static void RepairSiblingOrder(DesktopWindowController window, List<string> repairs)
        {
            DesktopWindowView view = window.GetComponent<DesktopWindowView>();
            if (view == null || view.TitleBar == null || view.ContentRoot == null)
            {
                return;
            }

            if (view.TitleBar.GetSiblingIndex() < view.ContentRoot.GetSiblingIndex())
            {
                view.TitleBar.SetAsLastSibling();
                repairs.Add("Moved TitleBar above ContentRoot in sibling order");
                EditorUtility.SetDirty(view.TitleBar);
            }
        }

        private static void ClampWindowIntoCanvas(DesktopWindowController window, RectTransform desktopWindowLayer, List<string> repairs)
        {
            RectTransform root = window.transform as RectTransform;
            if (root == null || desktopWindowLayer == null)
            {
                return;
            }

            Vector2 clamped = DesktopWindowGeometry.ClampAnchoredPosition(desktopWindowLayer.rect, root.anchoredPosition, root.rect.size, root.pivot);
            if ((clamped - root.anchoredPosition).sqrMagnitude > 0.0001f)
            {
                root.anchoredPosition = clamped;
                DesktopWindowModel model = window.GetComponent<DesktopWindowModel>();
                if (model != null)
                {
                    model.SetPosition(clamped);
                    EditorUtility.SetDirty(model);
                }

                repairs.Add("Clamped UtilityWindow inside DesktopWindowLayer");
                EditorUtility.SetDirty(root);
            }
        }

        private static void ValidateScene(Scene scene, List<string> errors)
        {
            DesktopWindowController utilityWindow = FindSingleWindow(scene, UtilityWindowName, errors);
            if (utilityWindow == null)
            {
                return;
            }

            if (utilityWindow.GetComponent<DesktopWindowController>() == null)
            {
                errors.Add("Missing DesktopWindowController.");
            }

            DesktopWindowView view = utilityWindow.GetComponent<DesktopWindowView>();
            if (view == null)
            {
                errors.Add("Missing DesktopWindowView.");
                return;
            }

            if (view.WindowRoot == null)
            {
                errors.Add("DesktopWindowView missing WindowRoot reference.");
            }

            if (view.TitleBar == null)
            {
                errors.Add("DesktopWindowView missing TitleBar reference.");
            }
            else
            {
                Graphic titleGraphic = view.TitleBar.GetComponent<Graphic>();
                if (titleGraphic == null)
                {
                    errors.Add("TitleBar is missing a Graphic component.");
                }
                else if (!titleGraphic.raycastTarget)
                {
                    errors.Add("TitleBar Raycast Target is disabled.");
                }
            }

            if (view.ContentRoot == null)
            {
                errors.Add("DesktopWindowView missing ContentRoot reference.");
            }

            DesktopWindowModel model = utilityWindow.GetComponent<DesktopWindowModel>();
            if (model == null)
            {
                errors.Add("Missing DesktopWindowModel.");
            }
            else if (!model.DraggingAllowed)
            {
                errors.Add("UtilityWindow movement is disabled by DesktopWindowModel.draggingAllowed.");
            }

            if (utilityWindow.MovementLocked)
            {
                errors.Add("UtilityWindow movement is locked.");
            }

            Canvas canvas = utilityWindow.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                errors.Add("UtilityWindow is outside Canvas.");
            }
            else if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                errors.Add("Canvas is missing GraphicRaycaster.");
            }

            EventSystem[] eventSystems = FindComponentsInScene<EventSystem>(scene);
            if (eventSystems.Length == 0)
            {
                errors.Add("Missing EventSystem.");
            }
            else if (eventSystems.Length > 1)
            {
                errors.Add("Duplicate EventSystem objects found.");
            }
            else if (eventSystems[0].GetComponent<BaseInputModule>() == null)
            {
                errors.Add("EventSystem is missing a UI input module.");
            }

            if (FindComponentsInScene<DesktopWindowManager>(scene).Length > 1)
            {
                errors.Add("Duplicate DesktopWindowManager objects found.");
            }

            RawImage utilityRawImage = FindUtilityRawImage(utilityWindow);
            if (utilityRawImage != null && utilityRawImage.raycastTarget)
            {
                errors.Add("UtilityGameRawImage is blocking raycasts.");
            }

            if (view.TitleBar != null && view.ContentRoot != null && view.TitleBar.GetSiblingIndex() < view.ContentRoot.GetSiblingIndex())
            {
                errors.Add("TitleBar is below ContentRoot in sibling order.");
            }

            CoverToEraseRule rule = FindFirst<CoverToEraseRule>(scene);
            if (rule == null || rule.CoveringWindow != utilityWindow)
            {
                errors.Add("CoverToEraseRule is not connected to UtilityWindow.");
            }

            WindowDragPlayerLock dragLock = FindFirst<WindowDragPlayerLock>(scene);
            if (dragLock == null)
            {
                errors.Add("WindowDragPlayerLock is missing; player input may not lock during dragging.");
            }
        }

        private static bool TryOpenScene(out Scene scene)
        {
            scene = default;
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[UtilityWindow Dragging] Missing scene: " + ScenePath);
                return false;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static DesktopWindowController FindSingleWindow(Scene scene, string windowName, List<string> errors)
        {
            DesktopWindowController[] windows = FindComponentsInScene<DesktopWindowController>(scene);
            DesktopWindowController found = null;
            int count = 0;
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null && windows[i].name == windowName)
                {
                    found = windows[i];
                    count++;
                }
            }

            if (count == 0)
            {
                errors.Add("Missing " + windowName + ".");
                return null;
            }

            if (count > 1)
            {
                errors.Add("Duplicate " + windowName + " objects found.");
            }

            return found;
        }

        private static T FindSingleComponent<T>(Scene scene, List<string> errors, string label) where T : Component
        {
            T[] components = FindComponentsInScene<T>(scene);
            if (components.Length == 0)
            {
                errors.Add("Missing " + label + ".");
                return null;
            }

            if (components.Length > 1)
            {
                errors.Add("Duplicate " + label + " objects found.");
            }

            return components[0];
        }

        private static RawImage FindUtilityRawImage(DesktopWindowController window)
        {
            RawImage[] rawImages = window.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < rawImages.Length; i++)
            {
                if (rawImages[i] != null && rawImages[i].name == UtilityRawImageName)
                {
                    return rawImages[i];
                }
            }

            return null;
        }

        private static Transform FindSceneTransform(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindInChildrenByName(roots[i].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindInChildrenByName(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindInChildrenByName(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T FindFirst<T>(Scene scene) where T : Component
        {
            T[] components = FindComponentsInScene<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                components.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        private static bool EnsureBorderVisuals(SerializedObject serializedObject, DesktopWindowController window)
        {
            SerializedProperty property = serializedObject.FindProperty("borderVisuals");
            if (property == null)
            {
                return false;
            }

            Image rootImage = window.GetComponent<Image>();
            DesktopWindowView view = window.GetComponent<DesktopWindowView>();
            Image titleImage = view != null && view.TitleBar != null ? view.TitleBar.GetComponent<Image>() : null;
            int count = rootImage != null && titleImage != null ? 2 : rootImage != null || titleImage != null ? 1 : 0;
            if (property.arraySize == count && count > 0)
            {
                return false;
            }

            property.arraySize = count;
            int index = 0;
            if (rootImage != null)
            {
                property.GetArrayElementAtIndex(index++).objectReferenceValue = rootImage;
            }

            if (titleImage != null)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = titleImage;
            }

            return count > 0;
        }

        private static bool SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            return true;
        }

        private static bool SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.vector2Value == value)
            {
                return false;
            }

            property.vector2Value = value;
            return true;
        }

        private static void LogErrors(List<string> errors)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError("[UtilityWindow Dragging] " + errors[i]);
            }
        }
    }
}
