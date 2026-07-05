using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OwariNakiTobira.Editor
{
    public static class LiveUtilityWindowMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string UtilityCameraName = "UtilityViewCamera";
        private const string UtilityRawImageName = "UtilityGameRawImage";
        private const string MainWindowName = "MainGameWindow";
        private const string UtilityWindowName = "UtilityWindow";
        private const string ErasablePuzzleLayerName = "ErasablePuzzle";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PuzzleObjectLayerName = "PuzzleObject";

        [MenuItem("Tools/OwariNakiTobira/Convert Utility Window To Live View")]
        public static void ConvertUtilityWindowToLiveView()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"DesktopHostPrototype scene is missing at {ScenePath}.");
                return;
            }

            EnsureSceneOpen();
            EnsureLayer(ErasablePuzzleLayerName);

            if (!TryFindWindows(out DesktopWindowController mainWindow, out DesktopWindowController utilityWindow)
                || !TryFindGameplayCamera(out GameWindowCamera gameWindowCamera)
                || !TryFindGameWindowView(mainWindow, out GameWindowView gameWindowView))
            {
                return;
            }

            Camera gameplayCamera = gameWindowCamera.GameplayCamera;
            if (gameplayCamera == null)
            {
                Debug.LogError("GameplayCamera reference is missing on GameWindowCamera.");
                return;
            }

            Camera utilityCamera = EnsureUtilityViewCamera(gameplayCamera);
            if (utilityCamera == null)
            {
                return;
            }

            RawImage utilityRawImage = EnsureUtilityRawImage(utilityWindow);
            if (utilityRawImage == null)
            {
                return;
            }

            ConfigureCameraLayers(gameWindowCamera, utilityCamera);
            WindowPuzzleTarget target = FindPrimaryPuzzleTarget();
            if (target != null)
            {
                AssignTargetRenderersToErasableLayer(target);
            }

            UtilityWindowRenderController renderController = utilityWindow.GetComponent<UtilityWindowRenderController>();
            if (renderController == null)
            {
                renderController = utilityWindow.gameObject.AddComponent<UtilityWindowRenderController>();
            }

            AssignObject(renderController, "sourceGameplayCamera", gameplayCamera);
            AssignObject(renderController, "utilityViewCamera", utilityCamera);
            AssignObject(renderController, "targetRawImage", utilityRawImage);
            AssignVector2Int(renderController, "renderResolution", new Vector2Int(512, 288));
            AssignLayerMask(renderController, "utilityCullingMask", GetUtilityCameraMask());

            UtilityWindowViewportMapper mapper = utilityWindow.GetComponent<UtilityWindowViewportMapper>();
            if (mapper == null)
            {
                mapper = utilityWindow.gameObject.AddComponent<UtilityWindowViewportMapper>();
            }

            AssignObject(mapper, "mainGameWindowView", gameWindowView);
            AssignObject(mapper, "utilityRawImage", utilityRawImage);
            AssignObject(mapper, "canvas", utilityRawImage.GetComponentInParent<Canvas>());

            CoverToEraseRule rule = EnsureCoverToEraseRule(utilityWindow, gameWindowCamera, gameWindowView, target, utilityRawImage.rectTransform);
            AssignObject(mapper, "debugCoverRule", rule);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("UtilityWindow now uses a live UtilityViewCamera RenderTexture with spatial UV mapping and CoverToEraseRule integration.");
        }

        [MenuItem("Tools/OwariNakiTobira/Validate Live Utility Window")]
        public static void ValidateLiveUtilityWindow()
        {
            EnsureSceneOpen();
            List<string> errors = new List<string>();

            TryFindWindows(out DesktopWindowController mainWindow, out DesktopWindowController utilityWindow);
            TryFindGameplayCamera(out GameWindowCamera gameWindowCamera);
            TryFindGameWindowView(mainWindow, out GameWindowView gameWindowView);

            UtilityWindowRenderController renderController = utilityWindow != null ? utilityWindow.GetComponent<UtilityWindowRenderController>() : null;
            UtilityWindowViewportMapper mapper = utilityWindow != null ? utilityWindow.GetComponent<UtilityWindowViewportMapper>() : null;
            RawImage utilityRawImage = mapper != null ? mapper.UtilityRawImage : FindUtilityRawImage(utilityWindow);
            Camera[] utilityCameras = FindObjectsByName<Camera>(UtilityCameraName);
            WindowPuzzleTarget[] targets = Object.FindObjectsByType<WindowPuzzleTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (mainWindow == null)
            {
                errors.Add("MainGameWindow was not found.");
            }

            if (utilityWindow == null)
            {
                errors.Add("UtilityWindow was not found.");
            }

            if (gameWindowCamera == null || gameWindowCamera.GameplayCamera == null)
            {
                errors.Add("GameWindowCamera or its GameplayCamera reference is missing.");
            }

            if (gameWindowView == null || gameWindowView.RawImage == null)
            {
                errors.Add("MainGameWindow is missing GameWindowView or its RawImage.");
            }

            if (renderController == null)
            {
                errors.Add("UtilityWindowRenderController is missing.");
            }
            else
            {
                if (renderController.TargetRawImage == null)
                {
                    errors.Add("UtilityWindowRenderController has no target RawImage.");
                }

                if (renderController.SourceGameplayCamera == null || renderController.UtilityViewCamera == null)
                {
                    errors.Add("UtilityWindowRenderController is missing source or utility camera reference.");
                }
            }

            if (mapper == null || utilityRawImage == null)
            {
                errors.Add("UtilityWindowViewportMapper or UtilityGameRawImage is missing.");
            }

            if (utilityCameras.Length != 1)
            {
                errors.Add($"Expected exactly one {UtilityCameraName}; found {utilityCameras.Length}.");
            }

            if (utilityCameras.Length == 1)
            {
                Camera utilityCamera = utilityCameras[0];
                int erasableLayer = LayerMask.NameToLayer(ErasablePuzzleLayerName);
                if (erasableLayer >= 0 && (utilityCamera.cullingMask & (1 << erasableLayer)) != 0)
                {
                    errors.Add("UtilityViewCamera includes the ErasablePuzzle layer.");
                }

                if (renderController != null && Application.isPlaying && utilityCamera.targetTexture == null)
                {
                    errors.Add("UtilityViewCamera has no target texture while in Play Mode.");
                }
            }

            if (gameWindowCamera != null)
            {
                int erasableLayer = LayerMask.NameToLayer(ErasablePuzzleLayerName);
                if (erasableLayer >= 0 && (gameWindowCamera.GameplayLayers.value & (1 << erasableLayer)) == 0)
                {
                    errors.Add("GameplayCamera layer mask does not include ErasablePuzzle.");
                }
            }

            if (!Application.isPlaying && utilityRawImage != null && utilityRawImage.texture != null)
            {
                errors.Add("UtilityGameRawImage has a static texture assigned in Edit Mode. The live RenderTexture should be runtime-created by UtilityWindowRenderController.");
            }

            if (gameWindowView != null && utilityRawImage != null && Application.isPlaying && gameWindowView.RawImage != null && gameWindowView.RawImage.texture == utilityRawImage.texture)
            {
                errors.Add("MainGameWindow and UtilityWindow share the same RenderTexture object.");
            }

            CoverToEraseRule rule = Object.FindObjectsByType<CoverToEraseRule>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (rule == null || rule.CoveringWindow != utilityWindow)
            {
                errors.Add("CoverToEraseRule is missing or does not reference UtilityWindow.");
            }
            else if (rule.CoveringRectOverride == null)
            {
                errors.Add("CoverToEraseRule is not using the UtilityGameRawImage content rectangle.");
            }

            if (targets.Length == 0)
            {
                errors.Add("No WindowPuzzleTarget exists for the erasable obstacle.");
            }
            else if (!TargetsHaveRenderersAndColliders(targets))
            {
                errors.Add("At least one WindowPuzzleTarget lacks both Renderer and Collider coverage.");
            }

            if (CountActive<PlayerInput>() > 1)
            {
                errors.Add("More than one active PlayerInput exists.");
            }

            if (CountActive<EventSystem>() > 1)
            {
                errors.Add("More than one active EventSystem exists.");
            }

            if (IsGameplayWorldParentedUnderCamera())
            {
                errors.Add("GameplayWorld is parented under a camera.");
            }

            if (errors.Count == 0)
            {
                Debug.Log("Live UtilityWindow validation passed. In Play Mode, the utility RenderTexture is runtime-created and assigned by UtilityWindowRenderController.");
                return;
            }

            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError(errors[i]);
            }
        }

        private static bool TryFindWindows(out DesktopWindowController mainWindow, out DesktopWindowController utilityWindow)
        {
            mainWindow = FindWindow(MainWindowName);
            utilityWindow = FindWindow(UtilityWindowName);

            if (mainWindow == null)
            {
                Debug.LogError($"{MainWindowName} was not found.");
            }

            if (utilityWindow == null)
            {
                Debug.LogError($"{UtilityWindowName} was not found.");
            }

            return mainWindow != null && utilityWindow != null;
        }

        private static bool TryFindGameplayCamera(out GameWindowCamera gameWindowCamera)
        {
            gameWindowCamera = Object.FindObjectsByType<GameWindowCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (gameWindowCamera == null)
            {
                Debug.LogError("GameWindowCamera was not found.");
                return false;
            }

            return true;
        }

        private static bool TryFindGameWindowView(DesktopWindowController mainWindow, out GameWindowView gameWindowView)
        {
            gameWindowView = mainWindow != null ? mainWindow.GetComponent<GameWindowView>() : null;
            if (gameWindowView == null)
            {
                Debug.LogError("MainGameWindow is missing GameWindowView.");
                return false;
            }

            return true;
        }

        private static DesktopWindowController FindWindow(string name)
        {
            DesktopWindowController[] windows = Object.FindObjectsByType<DesktopWindowController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null && windows[i].name == name)
                {
                    return windows[i];
                }
            }

            return null;
        }

        private static Camera EnsureUtilityViewCamera(Camera gameplayCamera)
        {
            Camera[] existing = FindObjectsByName<Camera>(UtilityCameraName);
            if (existing.Length > 1)
            {
                Debug.LogError($"Expected zero or one {UtilityCameraName}; found {existing.Length}. Remove duplicates before conversion.");
                return null;
            }

            if (existing.Length == 1)
            {
                return existing[0];
            }

            Transform parent = gameplayCamera.transform.parent != null ? gameplayCamera.transform.parent : gameplayCamera.transform;
            GameObject cameraObject = new GameObject(UtilityCameraName);
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Utility View Camera");
            cameraObject.transform.SetParent(parent, false);
            Camera utilityCamera = cameraObject.AddComponent<Camera>();
            UtilityWindowRenderController.ApplySynchronizationSnapshot(
                utilityCamera,
                UtilityWindowRenderController.CreateSynchronizationSnapshot(gameplayCamera),
                GetUtilityCameraMask());
            utilityCamera.depth = gameplayCamera.depth - 1f;
            return utilityCamera;
        }

        private static RawImage EnsureUtilityRawImage(DesktopWindowController utilityWindow)
        {
            if (utilityWindow == null || utilityWindow.View == null || utilityWindow.View.ContentRoot == null)
            {
                Debug.LogError("UtilityWindow ContentRoot is missing.");
                return null;
            }

            RectTransform contentRoot = utilityWindow.View.ContentRoot;
            RawImage rawImage = FindUtilityRawImage(utilityWindow);
            if (rawImage == null)
            {
                GameObject rawObject = new GameObject(UtilityRawImageName, typeof(RectTransform), typeof(RawImage));
                Undo.RegisterCreatedObjectUndo(rawObject, "Create Utility RawImage");
                RectTransform rawRect = rawObject.GetComponent<RectTransform>();
                rawRect.SetParent(contentRoot, false);
                rawRect.anchorMin = Vector2.zero;
                rawRect.anchorMax = Vector2.one;
                rawRect.offsetMin = Vector2.zero;
                rawRect.offsetMax = Vector2.zero;
                rawImage = rawObject.GetComponent<RawImage>();
            }

            rawImage.name = UtilityRawImageName;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            RectTransform rect = rawImage.rectTransform;
            rect.SetParent(contentRoot, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            DisablePlaceholderChildren(contentRoot, rawImage.gameObject);
            if (contentRoot.GetComponent<RectMask2D>() == null)
            {
                contentRoot.gameObject.AddComponent<RectMask2D>();
            }

            return rawImage;
        }

        private static RawImage FindUtilityRawImage(DesktopWindowController utilityWindow)
        {
            if (utilityWindow == null)
            {
                return null;
            }

            RawImage[] rawImages = utilityWindow.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < rawImages.Length; i++)
            {
                if (rawImages[i] != null && rawImages[i].name == UtilityRawImageName)
                {
                    return rawImages[i];
                }
            }

            return null;
        }

        private static CoverToEraseRule EnsureCoverToEraseRule(
            DesktopWindowController utilityWindow,
            GameWindowCamera gameWindowCamera,
            GameWindowView gameWindowView,
            WindowPuzzleTarget target,
            RectTransform coveringRectOverride)
        {
            CoverToEraseRule rule = Object.FindObjectsByType<CoverToEraseRule>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (rule == null)
            {
                GameObject systems = FindGameObjectByName("Systems");
                GameObject ruleObject = new GameObject("CoverToEraseRule");
                Undo.RegisterCreatedObjectUndo(ruleObject, "Create CoverToEraseRule");
                if (systems != null)
                {
                    ruleObject.transform.SetParent(systems.transform, false);
                }

                rule = ruleObject.AddComponent<CoverToEraseRule>();
            }

            AssignObject(rule, "coveringWindow", utilityWindow);
            AssignObject(rule, "coveringRectOverride", coveringRectOverride);
            AssignObject(rule, "gameWindowCamera", gameWindowCamera);
            AssignObject(rule, "gameWindowView", gameWindowView);
            if (target != null)
            {
                AssignObjectArray(rule, "targets", target);
            }

            return rule;
        }

        private static void ConfigureCameraLayers(GameWindowCamera gameWindowCamera, Camera utilityCamera)
        {
            int gameplayMask = LayerMask.GetMask(GameplayWorldLayerName, GameplayPlayerLayerName, PuzzleObjectLayerName, ErasablePuzzleLayerName);
            if (gameplayMask != 0)
            {
                gameWindowCamera.SetGameplayLayers(gameplayMask);
            }

            utilityCamera.cullingMask = GetUtilityCameraMask();
        }

        private static int GetUtilityCameraMask()
        {
            int mask = LayerMask.GetMask(GameplayWorldLayerName, GameplayPlayerLayerName, PuzzleObjectLayerName);
            int erasableLayer = LayerMask.NameToLayer(ErasablePuzzleLayerName);
            if (mask == 0)
            {
                mask = ~0;
            }

            return erasableLayer >= 0 ? mask & ~(1 << erasableLayer) : mask;
        }

        private static WindowPuzzleTarget FindPrimaryPuzzleTarget()
        {
            WindowPuzzleTarget[] targets = Object.FindObjectsByType<WindowPuzzleTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return targets.FirstOrDefault(target => target != null && target.name == "CoverPuzzleWall") ?? targets.FirstOrDefault();
        }

        private static void AssignTargetRenderersToErasableLayer(WindowPuzzleTarget target)
        {
            int layer = LayerMask.NameToLayer(ErasablePuzzleLayerName);
            if (layer < 0 || target == null)
            {
                Debug.LogWarning($"Layer '{ErasablePuzzleLayerName}' is missing. Assign puzzle obstacle renderers manually.");
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].gameObject.layer = layer;
                }
            }
        }

        private static bool TargetsHaveRenderersAndColliders(WindowPuzzleTarget[] targets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                if (targets[i].GetComponentsInChildren<Renderer>(true).Length == 0
                    || targets[i].GetComponentsInChildren<Collider>(true).Length == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsGameplayWorldParentedUnderCamera()
        {
            GameObject gameplayWorld = FindGameObjectByName("GameplayWorld");
            if (gameplayWorld == null)
            {
                return false;
            }

            Transform current = gameplayWorld.transform.parent;
            while (current != null)
            {
                if (current.GetComponent<Camera>() != null)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static int CountActive<T>() where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return components.Length;
        }

        private static T[] FindObjectsByName<T>(string name) where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component != null && component.name == name)
                .ToArray();
        }

        private static GameObject FindGameObjectByName(string name)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == name)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static void DisablePlaceholderChildren(RectTransform contentRoot, GameObject keep)
        {
            for (int i = 0; i < contentRoot.childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                if (child != null && child.gameObject != keep && child.gameObject.name != "NoSignalOverlay")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void EnsureSceneOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == ScenePath)
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            Debug.LogWarning($"Could not create layer '{layerName}'. Add it manually in Project Settings > Tags and Layers.");
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignObjectArray(Object target, string propertyName, params Object[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignVector2Int(Object target, string propertyName, Vector2Int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector2IntValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
