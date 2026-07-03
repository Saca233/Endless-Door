using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OwariNakiTobira.Editor
{
    public static class PrototypeGameplayCameraFixMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PuzzleObjectLayerName = "PuzzleObject";

        [MenuItem("Tools/OwariNakiTobira/Fix Prototype Gameplay Camera")]
        public static void FixPrototypeGameplayCameraMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool success = FixPrototypeGameplayCamera();
            EditorUtility.DisplayDialog(
                success ? "Gameplay Camera Fixed" : "Gameplay Camera Repair Failed",
                success ? "DesktopHostPrototype gameplay camera hierarchy was repaired." : "Repair failed. See Console for validation errors.",
                "OK");
        }

        public static bool FixPrototypeGameplayCamera()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Fix Prototype Gameplay Camera] Missing scene: " + ScenePath);
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> repairs = new List<string>();
            List<string> validationErrors = new List<string>();

            bool repaired = RepairScene(scene, repairs, validationErrors);
            if (!repaired)
            {
                for (int i = 0; i < validationErrors.Count; i++)
                {
                    Debug.LogError("[Fix Prototype Gameplay Camera] " + validationErrors[i]);
                }

                return false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (repairs.Count == 0)
            {
                Debug.Log("[Fix Prototype Gameplay Camera] No repair changes were needed.");
            }
            else
            {
                Debug.Log("[Fix Prototype Gameplay Camera] Repaired:\n- " + string.Join("\n- ", repairs));
            }

            return true;
        }

        private static bool RepairScene(Scene scene, List<string> repairs, List<string> validationErrors)
        {
            Transform host = FindSceneTransform(scene, "DesktopHostPrototype") ?? FindSceneTransform(scene, "DesktopHost");
            Transform gameplayWorld = FindSceneTransform(scene, "GameplayWorld");
            GameWindowCamera gameWindowCamera = FindSingleGameWindowCamera(scene, validationErrors);
            Camera gameplayCamera = gameWindowCamera != null ? gameWindowCamera.GameplayCamera : null;

            if (host == null)
            {
                validationErrors.Add("Missing root object DesktopHostPrototype/DesktopHost.");
            }

            if (gameplayWorld == null)
            {
                validationErrors.Add("Missing GameplayWorld.");
            }

            if (gameWindowCamera == null || gameplayCamera == null)
            {
                validationErrors.Add("Missing GameplayCamera/GameWindowCamera.");
            }

            if (validationErrors.Count > 0)
            {
                return false;
            }

            if (host.name != "DesktopHostPrototype")
            {
                host.name = "DesktopHostPrototype";
                repairs.Add("Renamed root object to DesktopHostPrototype");
            }

            EnsureLayer(GameplayWorldLayerName);
            EnsureLayer(GameplayPlayerLayerName);
            EnsureLayer(PuzzleObjectLayerName);
            EnsureLayer(DesktopWorldLayerName);

            Transform rig = EnsureCameraRig(scene, host, gameplayCamera.transform, repairs);
            DetachGameplayWorld(host, gameplayWorld, rig, gameplayCamera.transform, repairs);
            ReparentGameplayCameraToRig(rig, gameplayCamera.transform, repairs);

            Transform environment = GetOrCreateChild(gameplayWorld, "Environment", repairs);
            Transform platforms = GetOrCreateChild(gameplayWorld, "Platforms", repairs);
            Transform obstacles = GetOrCreateChild(gameplayWorld, "Obstacles", repairs);
            Transform playerGroup = GetOrCreateChild(gameplayWorld, "Player", repairs);
            OrganizeGameplayWorld(scene, gameplayWorld, environment, platforms, obstacles, playerGroup, repairs);
            ExpandPrototypeGameplayLevel(environment, platforms, repairs);

            Transform followTarget = FindUnambiguousPlayer(scene, validationErrors);
            SideScrollerCameraBounds bounds = GetOrCreateComponent<SideScrollerCameraBounds>(rig.gameObject, "GameplayCameraRig.SideScrollerCameraBounds", repairs);
            bounds.Configure(new Vector2(-16f, -2f), new Vector2(22f, 8f), true);
            EditorUtility.SetDirty(bounds);

            SideScrollerCameraController controller = GetOrCreateComponent<SideScrollerCameraController>(rig.gameObject, "GameplayCameraRig.SideScrollerCameraController", repairs);
            ConfigureCameraController(controller, rig, gameplayCamera, bounds, followTarget, repairs);
            if (followTarget != null)
            {
                controller.SnapToTarget();
            }

            AssignLayerRecursively(gameplayWorld.gameObject, GameplayWorldLayerName);
            Transform player = FindSceneTransform(scene, "PrototypePlayer");
            if (player != null)
            {
                AssignLayerRecursively(player.gameObject, GameplayPlayerLayerName);
            }

            Transform coverPuzzleWall = FindSceneTransform(scene, "CoverPuzzleWall");
            if (coverPuzzleWall != null)
            {
                AssignLayerRecursively(coverPuzzleWall.gameObject, PuzzleObjectLayerName);
            }

            ValidateRepairedScene(scene, gameplayWorld, rig, gameplayCamera, gameWindowCamera, validationErrors);
            for (int i = 0; i < validationErrors.Count; i++)
            {
                Debug.LogError("[Fix Prototype Gameplay Camera] " + validationErrors[i]);
            }

            return validationErrors.Count == 0;
        }

        private static Transform EnsureCameraRig(Scene scene, Transform host, Transform gameplayCamera, List<string> repairs)
        {
            Transform rig = FindSceneTransform(scene, "GameplayCameraRig");
            if (rig == null)
            {
                GameObject rigObject = new GameObject("GameplayCameraRig");
                rig = rigObject.transform;
                rig.SetParent(host, false);
                rig.SetPositionAndRotation(gameplayCamera.position, gameplayCamera.rotation);
                repairs.Add("Created GameplayCameraRig");
            }
            else if (rig.parent != host)
            {
                rig.SetParent(host, true);
                repairs.Add("Moved GameplayCameraRig under DesktopHostPrototype");
            }

            rig.localScale = Vector3.one;
            return rig;
        }

        private static void DetachGameplayWorld(Transform host, Transform gameplayWorld, Transform rig, Transform gameplayCamera, List<string> repairs)
        {
            bool invalidParent = gameplayWorld.IsChildOf(gameplayCamera)
                || gameplayWorld.IsChildOf(rig)
                || gameplayWorld.GetComponentInParent<Camera>() != null
                || gameplayWorld.parent != host;

            if (!invalidParent)
            {
                return;
            }

            gameplayWorld.SetParent(host, true);
            repairs.Add("Moved GameplayWorld to DesktopHostPrototype root while preserving world transforms");
        }

        private static void ReparentGameplayCameraToRig(Transform rig, Transform gameplayCamera, List<string> repairs)
        {
            Vector3 worldPosition = gameplayCamera.position;
            Quaternion worldRotation = gameplayCamera.rotation;
            rig.SetPositionAndRotation(worldPosition, worldRotation);

            if (gameplayCamera.parent != rig)
            {
                gameplayCamera.SetParent(rig, true);
                repairs.Add("Moved GameplayCamera under GameplayCameraRig");
            }

            gameplayCamera.localPosition = Vector3.zero;
            gameplayCamera.localRotation = Quaternion.identity;
            gameplayCamera.localScale = Vector3.one;
            EditorUtility.SetDirty(gameplayCamera);
            EditorUtility.SetDirty(rig);
        }

        private static void OrganizeGameplayWorld(
            Scene scene,
            Transform gameplayWorld,
            Transform environment,
            Transform platforms,
            Transform obstacles,
            Transform playerGroup,
            List<string> repairs)
        {
            MoveIfFound(scene, gameplayWorld, "GameplayFloor", environment, repairs);
            MoveIfFound(scene, gameplayWorld, "PlaceholderBuildings", environment, repairs);
            MoveIfFound(scene, gameplayWorld, "DestinationPlatform", platforms, repairs);
            MoveIfFound(scene, gameplayWorld, "CoverPuzzleWall", obstacles, repairs);
            MoveIfFound(scene, gameplayWorld, "PrototypePlayer", playerGroup, repairs);
        }

        private static void MoveIfFound(Scene scene, Transform gameplayWorld, string objectName, Transform newParent, List<string> repairs)
        {
            Transform target = FindSceneTransform(scene, objectName);
            if (target == null || target == newParent || target.IsChildOf(newParent) || !target.IsChildOf(gameplayWorld))
            {
                return;
            }

            target.SetParent(newParent, true);
            repairs.Add("Moved " + objectName + " under GameplayWorld/" + newParent.name);
            EditorUtility.SetDirty(target);
        }

        private static void ExpandPrototypeGameplayLevel(Transform environment, Transform platforms, List<string> repairs)
        {
            Transform floor = FindInChildrenByName(environment, "GameplayFloor");
            if (floor != null)
            {
                Vector3 position = floor.position;
                Vector3 scale = floor.localScale;
                bool changed = false;
                if (!Mathf.Approximately(position.x, 3f))
                {
                    position.x = 3f;
                    changed = true;
                }

                if (scale.x < 38f)
                {
                    scale.x = 38f;
                    changed = true;
                }

                if (changed)
                {
                    floor.position = position;
                    floor.localScale = scale;
                    repairs.Add("Expanded GameplayFloor to cover camera exploration bounds");
                    EditorUtility.SetDirty(floor);
                }
            }

            EnsurePlaceholderCube(environment, "CameraTestBuilding_Left", new Vector3(-11f, 0.9f, 0.8f), new Vector3(0.9f, 3.2f, 0.6f), GameplayWorldLayerName, repairs);
            EnsurePlaceholderCube(environment, "CameraTestBuilding_Right", new Vector3(14f, 1.1f, 0.8f), new Vector3(1.1f, 3.8f, 0.6f), GameplayWorldLayerName, repairs);
            EnsurePlaceholderCube(platforms, "CameraTestPlatform_Right", new Vector3(12f, 0.2f, 0f), new Vector3(3.2f, 0.28f, 1.5f), GameplayWorldLayerName, repairs);
        }

        private static void EnsurePlaceholderCube(Transform parent, string name, Vector3 position, Vector3 scale, string layerName, List<string> repairs)
        {
            Transform existing = FindInChildrenByName(parent, name);
            GameObject cube;
            if (existing == null)
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                cube.transform.SetParent(parent, false);
                repairs.Add("Created " + name);
            }
            else
            {
                cube = existing.gameObject;
            }

            cube.transform.position = position;
            cube.transform.localScale = scale;
            AssignLayerRecursively(cube, layerName);
            EditorUtility.SetDirty(cube.transform);
        }

        private static Transform FindUnambiguousPlayer(Scene scene, List<string> validationErrors)
        {
            PlayerStateMachine[] players = FindComponentsInScene<PlayerStateMachine>(scene);
            if (players.Length == 1)
            {
                return players[0].transform;
            }

            Transform namedPlayer = FindSceneTransform(scene, "PrototypePlayer");
            if (players.Length == 0 && namedPlayer != null)
            {
                return namedPlayer;
            }

            if (players.Length > 1)
            {
                validationErrors.Add("More than one PlayerStateMachine found; follow target was not assigned automatically.");
            }

            return null;
        }

        private static void ConfigureCameraController(
            SideScrollerCameraController controller,
            Transform rig,
            Camera gameplayCamera,
            SideScrollerCameraBounds bounds,
            Transform followTarget,
            List<string> repairs)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            bool changed = false;
            changed |= SetSerializedObject(serializedObject, "cameraRig", rig);
            changed |= SetSerializedObject(serializedObject, "gameplayCamera", gameplayCamera);
            changed |= SetSerializedObject(serializedObject, "followTarget", followTarget);
            changed |= SetSerializedObject(serializedObject, "bounds", bounds);
            changed |= SetSerializedBool(serializedObject, "followY", false);
            changed |= SetSerializedFloat(serializedObject, "smoothTime", 0.15f);
            changed |= SetSerializedFloat(serializedObject, "lookAheadDistance", 1.1f);
            changed |= SetSerializedFloat(serializedObject, "deadZoneWidth", 0.75f);
            changed |= SetSerializedFloat(serializedObject, "deadZoneHeight", 0.6f);
            changed |= SetSerializedFloat(serializedObject, "fixedZPosition", rig.position.z);
            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Configured SideScrollerCameraController");
            }

            EditorUtility.SetDirty(controller);
        }

        private static void ValidateRepairedScene(
            Scene scene,
            Transform gameplayWorld,
            Transform rig,
            Camera gameplayCamera,
            GameWindowCamera gameWindowCamera,
            List<string> validationErrors)
        {
            if (gameplayWorld.IsChildOf(gameplayCamera.transform))
            {
                validationErrors.Add("GameplayWorld is under GameplayCamera.");
            }

            if (gameplayWorld.IsChildOf(rig))
            {
                validationErrors.Add("GameplayWorld is under GameplayCameraRig.");
            }

            if (gameplayCamera.GetComponentInParent<RectTransform>() != null)
            {
                validationErrors.Add("GameplayCamera is under a UI RectTransform.");
            }

            DesktopWindowController mainGameWindow = FindWindow(scene, "MainGameWindow");
            if (mainGameWindow != null && DraggingWindowWouldMoveCamera(mainGameWindow, gameplayCamera.transform))
            {
                validationErrors.Add("MainGameWindow dragging would move GameplayCamera because the camera is inside the UI hierarchy.");
            }

            if (gameplayCamera.targetTexture == null && !HasRuntimeRenderTextureTarget(scene, gameWindowCamera))
            {
                validationErrors.Add("GameplayCamera has no RenderTexture target or RuntimeRenderTexture assignment.");
            }

            if (CountActiveGameplayCameras(scene) > 1)
            {
                validationErrors.Add("More than one active GameplayCamera exists.");
            }

            DesktopCameraController desktopCamera = FindFirst<DesktopCameraController>(scene);
            if (desktopCamera != null && desktopCamera.DesktopCamera == gameplayCamera)
            {
                validationErrors.Add("DesktopCamera is mistakenly assigned as the gameplay render camera.");
            }
        }

        private static bool DraggingWindowWouldMoveCamera(DesktopWindowController window, Transform gameplayCamera)
        {
            if (window == null || window.View == null || window.View.WindowRoot == null || gameplayCamera == null)
            {
                return false;
            }

            RectTransform windowRoot = window.View.WindowRoot;
            Vector2 originalPosition = windowRoot.anchoredPosition;
            Vector3 cameraPosition = gameplayCamera.position;
            windowRoot.anchoredPosition = originalPosition + Vector2.right;
            bool moved = Vector3.SqrMagnitude(gameplayCamera.position - cameraPosition) > 0.0001f;
            windowRoot.anchoredPosition = originalPosition;
            return moved;
        }

        private static bool HasRuntimeRenderTextureTarget(Scene scene, GameWindowCamera gameWindowCamera)
        {
            RuntimeRenderTexture[] renderTextures = FindComponentsInScene<RuntimeRenderTexture>(scene);
            for (int i = 0; i < renderTextures.Length; i++)
            {
                SerializedObject serializedObject = new SerializedObject(renderTextures[i]);
                SerializedProperty cameraProperty = serializedObject.FindProperty("gameWindowCamera");
                SerializedProperty rawImageProperty = serializedObject.FindProperty("targetRawImage");
                if (cameraProperty != null
                    && cameraProperty.objectReferenceValue == gameWindowCamera
                    && rawImageProperty != null
                    && rawImageProperty.objectReferenceValue != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountActiveGameplayCameras(Scene scene)
        {
            GameWindowCamera[] gameWindowCameras = FindComponentsInScene<GameWindowCamera>(scene);
            int count = 0;
            for (int i = 0; i < gameWindowCameras.Length; i++)
            {
                Camera camera = gameWindowCameras[i].GameplayCamera;
                if (gameWindowCameras[i].gameObject.activeInHierarchy && camera != null && camera.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static GameWindowCamera FindSingleGameWindowCamera(Scene scene, List<string> validationErrors)
        {
            GameWindowCamera[] cameras = FindComponentsInScene<GameWindowCamera>(scene);
            if (cameras.Length == 0)
            {
                return null;
            }

            if (cameras.Length == 1)
            {
                return cameras[0];
            }

            GameWindowCamera namedCamera = null;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].name == "GameplayCamera")
                {
                    namedCamera = cameras[i];
                    break;
                }
            }

            if (namedCamera == null)
            {
                validationErrors.Add("Multiple GameWindowCamera components found and none is named GameplayCamera.");
                return null;
            }

            return namedCamera;
        }

        private static DesktopWindowController FindWindow(Scene scene, string name)
        {
            Transform transform = FindSceneTransform(scene, name);
            return transform != null ? transform.GetComponent<DesktopWindowController>() : null;
        }

        private static Transform GetOrCreateChild(Transform parent, string name, List<string> repairs)
        {
            Transform child = FindDirectChild(parent, name);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(name);
            childObject.transform.SetParent(parent, false);
            repairs.Add("Created GameplayWorld/" + name);
            return childObject.transform;
        }

        private static T GetOrCreateComponent<T>(GameObject gameObject, string label, List<string> repairs) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = gameObject.AddComponent<T>();
            repairs.Add(label);
            return component;
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

        private static bool SetSerializedObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetSerializedBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            return true;
        }

        private static bool SetSerializedFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning("[Fix Prototype Gameplay Camera] Could not create layer '" + layerName + "'. Add it manually in Project Settings > Tags and Layers.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null)
            {
                Debug.LogWarning("[Fix Prototype Gameplay Camera] Could not create layer '" + layerName + "'. Add it manually in Project Settings > Tags and Layers.");
                return;
            }

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

            Debug.LogWarning("[Fix Prototype Gameplay Camera] No empty user layer slot for '" + layerName + "'. Add it manually in Project Settings > Tags and Layers.");
        }

        private static void AssignLayerRecursively(GameObject root, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = layer;
                EditorUtility.SetDirty(children[i].gameObject);
            }
        }
    }
}
