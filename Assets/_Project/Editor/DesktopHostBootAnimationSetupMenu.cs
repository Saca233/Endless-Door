using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OwariNakiTobira.Editor
{
    public static class DesktopHostBootAnimationSetupMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string HostName = "DesktopHostPrototype";
        private const string CanvasName = "Canvas";
        private const string DesktopWindowLayerName = "DesktopWindowLayer";
        private const string BootLayerName = "BootLayer";

        [MenuItem("Tools/OwariNakiTobira/Setup DesktopHost Boot Animation")]
        public static void SetupDesktopHostBootAnimation()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Boot Animation] Missing DesktopHostPrototype scene at " + ScenePath);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform host = FindSceneTransform(scene, HostName);
            Transform canvasTransform = FindSceneTransform(scene, CanvasName);
            RectTransform desktopWindowLayer = FindSceneTransform(scene, DesktopWindowLayerName) as RectTransform;

            if (host == null || canvasTransform == null)
            {
                Debug.LogError("[Boot Animation] Missing DesktopHostPrototype or UI Canvas.");
                return;
            }

            Canvas canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }

            RectTransform bootLayer = GetOrCreateFullScreenLayer(canvasTransform, BootLayerName);
            bootLayer.SetAsLastSibling();

            Image background = GetOrCreateImage(bootLayer, "BootBackground");
            background.color = Color.black;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();

            Image scanlines = GetOrCreateImage(bootLayer, "BootScanlines");
            scanlines.color = new Color(0.1f, 0.8f, 0.55f, 0.08f);
            scanlines.raycastTarget = false;

            TextMeshProUGUI bootText = GetOrCreateBootText(bootLayer);
            scanlines.transform.SetSiblingIndex(Mathf.Min(1, bootLayer.childCount - 1));
            bootText.transform.SetAsLastSibling();
            CanvasGroup bootGroup = GetOrCreateCanvasGroup(bootLayer.gameObject);
            bootGroup.alpha = 0f;
            bootGroup.interactable = false;
            bootGroup.blocksRaycasts = false;

            CanvasGroup desktopWindowGroup = desktopWindowLayer != null
                ? GetOrCreateCanvasGroup(desktopWindowLayer.gameObject)
                : null;

            DesktopHostStartupBootAnimation animation = bootLayer.GetComponent<DesktopHostStartupBootAnimation>();
            if (animation == null)
            {
                animation = bootLayer.gameObject.AddComponent<DesktopHostStartupBootAnimation>();
            }

            SerializedObject serializedAnimation = new SerializedObject(animation);
            SetObject(serializedAnimation, "bootCanvasGroup", bootGroup);
            SetObject(serializedAnimation, "bootLogText", bootText);
            SetObject(serializedAnimation, "bootBackground", background);
            SetObject(serializedAnimation, "scanlineImage", scanlines);
            SetObject(serializedAnimation, "desktopWindowLayerGroup", desktopWindowGroup);
            SetBool(serializedAnimation, "playOnStart", true);
            SetBool(serializedAnimation, "hideDesktopWindowsDuringBoot", true);
            SetStringArray(serializedAnimation, "bootLines",
                "OWARI BIOS v0.02",
                "Checking memory... OK",
                "Mounting /desktop",
                "Loading 終わりなき扉.exe",
                "Restoring window manager",
                "Opening simulated desktop",
                "Boot complete.");
            serializedAnimation.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(animation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Boot Animation] DesktopHostPrototype boot animation setup complete.");
        }

        private static RectTransform GetOrCreateFullScreenLayer(Transform canvasTransform, string name)
        {
            Transform existing = FindDirectChild(canvasTransform, name);
            RectTransform rect = existing as RectTransform;
            if (rect == null)
            {
                GameObject layer = new GameObject(name, typeof(RectTransform));
                rect = layer.GetComponent<RectTransform>();
                rect.SetParent(canvasTransform, false);
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image GetOrCreateImage(RectTransform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            RectTransform rect;
            Image image;
            if (existing == null)
            {
                GameObject child = new GameObject(name, typeof(RectTransform), typeof(Image));
                rect = child.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                image = child.GetComponent<Image>();
            }
            else
            {
                rect = existing as RectTransform;
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
            }

            Stretch(rect);
            return image;
        }

        private static TextMeshProUGUI GetOrCreateBootText(RectTransform parent)
        {
            Transform existing = FindDirectChild(parent, "BootLogText");
            RectTransform rect;
            TextMeshProUGUI text;
            if (existing == null)
            {
                GameObject child = new GameObject("BootLogText", typeof(RectTransform), typeof(TextMeshProUGUI));
                rect = child.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                text = child.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                rect = existing as RectTransform;
                text = existing.GetComponent<TextMeshProUGUI>();
                if (text == null)
                {
                    text = existing.gameObject.AddComponent<TextMeshProUGUI>();
                }
            }

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(64f, 64f);
            rect.offsetMax = new Vector2(-64f, -64f);
            text.text = string.Empty;
            text.fontSize = 24f;
            text.color = new Color(0.46f, 1f, 0.72f, 1f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static CanvasGroup GetOrCreateCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.AddComponent<CanvasGroup>();
            }

            return group;
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

        private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetStringArray(SerializedObject serializedObject, string propertyName, params string[] values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}
