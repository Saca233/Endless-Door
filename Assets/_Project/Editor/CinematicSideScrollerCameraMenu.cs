using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OwariNakiTobira.Editor
{
    public static class CinematicSideScrollerCameraMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string GameplayWorldLayerName = "GameplayWorld";

        [MenuItem("Tools/OwariNakiTobira/Apply Cinematic Side-Scroller Camera")]
        public static void ApplyCinematicSideScrollerCamera()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Cinematic Camera] Missing scene: " + ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> repairs = new List<string>();
            List<string> errors = new List<string>();
            if (!ApplyToScene(scene, repairs, errors))
            {
                LogErrors(errors);
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Cinematic Camera] Applied cinematic side-scroller camera.\n- " + string.Join("\n- ", repairs));
        }

        [MenuItem("Tools/OwariNakiTobira/Validate Cinematic Camera")]
        public static void ValidateCinematicCamera()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[Cinematic Camera] Missing scene: " + ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> errors = new List<string>();
            ValidateScene(scene, errors);
            if (errors.Count == 0)
            {
                Debug.Log("[Cinematic Camera] Validation passed.");
                return;
            }

            LogErrors(errors);
        }

        private static bool ApplyToScene(Scene scene, List<string> repairs, List<string> errors)
        {
            Transform host = FindSceneTransform(scene, "DesktopHostPrototype");
            Transform gameplayWorld = FindSceneTransform(scene, "GameplayWorld");
            Transform rig = FindSceneTransform(scene, "GameplayCameraRig");
            GameWindowCamera gameWindowCamera = FindSingleGameWindowCamera(scene, errors);
            Camera gameplayCamera = gameWindowCamera != null ? gameWindowCamera.GameplayCamera : null;
            Transform player = FindUnambiguousPlayer(scene, errors);

            if (host == null)
            {
                errors.Add("Missing DesktopHostPrototype root.");
            }

            if (gameplayWorld == null)
            {
                errors.Add("Missing GameplayWorld.");
            }

            if (rig == null)
            {
                errors.Add("Missing GameplayCameraRig.");
            }

            if (gameplayCamera == null)
            {
                errors.Add("Missing GameplayCamera reference.");
            }

            if (errors.Count > 0)
            {
                return false;
            }

            if (gameplayWorld.IsChildOf(rig) || gameplayWorld.IsChildOf(gameplayCamera.transform))
            {
                errors.Add("GameplayWorld must remain fixed and cannot be parented under GameplayCameraRig or GameplayCamera.");
                return false;
            }

            EnsureLayer(GameplayWorldLayerName);
            EnsureCameraUnderRig(rig, gameplayCamera.transform, repairs);
            ConfigureGameplayCamera(gameplayCamera, gameWindowCamera, repairs);
            ExpandLevelForWideCamera(gameplayWorld, repairs);

            CameraBounds2D bounds = GetOrCreateComponent<CameraBounds2D>(rig.gameObject, repairs);
            bounds.Configure(new Vector2(-22f, -3.5f), new Vector2(34f, 9.5f), true);
            EditorUtility.SetDirty(bounds);

            SideScrollerCameraController oldController = rig.GetComponent<SideScrollerCameraController>();
            if (oldController != null && oldController.enabled)
            {
                oldController.enabled = false;
                EditorUtility.SetDirty(oldController);
                repairs.Add("Disabled old SideScrollerCameraController to avoid double-moving GameplayCameraRig");
            }

            CinematicSideScrollerCameraController controller = GetOrCreateComponent<CinematicSideScrollerCameraController>(rig.gameObject, repairs);
            ConfigureCinematicController(controller, rig, gameplayCamera, bounds, player, repairs);
            if (player != null)
            {
                controller.SnapToTarget();
                EditorUtility.SetDirty(rig);
            }

            ParallaxLayer[] parallaxLayers = CreateOrConfigureParallaxLayers(gameplayWorld, repairs);
            ParallaxBackgroundController parallaxController = GetOrCreateComponent<ParallaxBackgroundController>(rig.gameObject, repairs);
            ConfigureParallaxController(parallaxController, rig, parallaxLayers, repairs);

            ValidateScene(scene, errors);
            return errors.Count == 0;
        }

        private static void ConfigureGameplayCamera(Camera gameplayCamera, GameWindowCamera gameWindowCamera, List<string> repairs)
        {
            bool changed = false;
            if (!gameplayCamera.orthographic)
            {
                gameplayCamera.orthographic = true;
                changed = true;
            }

            if (!Mathf.Approximately(gameplayCamera.orthographicSize, 6.25f))
            {
                gameplayCamera.orthographicSize = 6.25f;
                changed = true;
            }

            if (changed)
            {
                repairs.Add("Configured GameplayCamera as a wide orthographic camera");
                EditorUtility.SetDirty(gameplayCamera);
            }

            if (gameWindowCamera != null)
            {
                gameWindowCamera.SetRenderResolution(new Vector2Int(1280, 720));
                EditorUtility.SetDirty(gameWindowCamera);
            }
        }

        private static void ConfigureCinematicController(
            CinematicSideScrollerCameraController controller,
            Transform rig,
            Camera gameplayCamera,
            CameraBounds2D bounds,
            Transform player,
            List<string> repairs)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            bool changed = false;
            changed |= SetObject(serializedObject, "cameraRig", rig);
            changed |= SetObject(serializedObject, "gameplayCamera", gameplayCamera);
            changed |= SetObject(serializedObject, "followTarget", player);
            changed |= SetObject(serializedObject, "bounds", bounds);
            changed |= SetFloat(serializedObject, "orthographicSize", 6.25f);
            changed |= SetFloat(serializedObject, "fixedZPosition", -10f);
            changed |= SetBool(serializedObject, "followY", false);
            changed |= SetFloat(serializedObject, "xSmoothTime", 0.35f);
            changed |= SetFloat(serializedObject, "ySmoothTime", 0.8f);
            changed |= SetVector2(serializedObject, "targetScreenOffset", new Vector2(0.18f, -0.15f));
            changed |= SetFloat(serializedObject, "walkLookAheadDistance", 1.25f);
            changed |= SetFloat(serializedObject, "runLookAheadDistance", 2f);
            changed |= SetFloat(serializedObject, "stoppingLookAheadDistance", 0.75f);
            changed |= SetFloat(serializedObject, "lookAheadSmoothTime", 0.45f);
            changed |= SetFloat(serializedObject, "walkSpeedThreshold", 0.1f);
            changed |= SetFloat(serializedObject, "runSpeedThreshold", 4f);
            changed |= SetFloat(serializedObject, "deadZoneWidth", 2.5f);
            changed |= SetFloat(serializedObject, "deadZoneHeight", 1.5f);
            changed |= SetFloat(serializedObject, "softZoneWidth", 4.25f);
            changed |= SetFloat(serializedObject, "softZoneHeight", 2.75f);
            changed |= SetBool(serializedObject, "positionLocked", false);
            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Configured CinematicSideScrollerCameraController");
            }

            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureParallaxController(ParallaxBackgroundController controller, Transform rig, ParallaxLayer[] layers, List<string> repairs)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            bool changed = false;
            changed |= SetObject(serializedObject, "cameraRig", rig);
            SerializedProperty layersProperty = serializedObject.FindProperty("layers");
            if (layersProperty != null)
            {
                layersProperty.arraySize = layers.Length;
                for (int i = 0; i < layers.Length; i++)
                {
                    SerializedProperty item = layersProperty.GetArrayElementAtIndex(i);
                    if (item.objectReferenceValue != layers[i])
                    {
                        item.objectReferenceValue = layers[i];
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                repairs.Add("Configured ParallaxBackgroundController");
            }

            EditorUtility.SetDirty(controller);
        }

        private static void EnsureCameraUnderRig(Transform rig, Transform gameplayCamera, List<string> repairs)
        {
            if (gameplayCamera.parent != rig)
            {
                gameplayCamera.SetParent(rig, true);
                repairs.Add("Moved GameplayCamera under GameplayCameraRig");
            }

            gameplayCamera.localPosition = Vector3.zero;
            gameplayCamera.localRotation = Quaternion.identity;
            gameplayCamera.localScale = Vector3.one;
            Vector3 rigPosition = rig.position;
            rigPosition.z = -10f;
            rig.position = rigPosition;
            EditorUtility.SetDirty(gameplayCamera);
            EditorUtility.SetDirty(rig);
        }

        private static void ExpandLevelForWideCamera(Transform gameplayWorld, List<string> repairs)
        {
            Transform environment = GetOrCreateChild(gameplayWorld, "Environment", repairs);
            Transform platforms = GetOrCreateChild(gameplayWorld, "Platforms", repairs);
            Transform floor = FindInChildrenByName(environment, "GameplayFloor");
            if (floor != null)
            {
                floor.position = new Vector3(6f, floor.position.y, floor.position.z);
                floor.localScale = new Vector3(Mathf.Max(floor.localScale.x, 72f), floor.localScale.y, floor.localScale.z);
                EditorUtility.SetDirty(floor);
                repairs.Add("Expanded GameplayFloor for wider camera travel");
            }

            EnsureVisualCube(environment, "ScaleReferenceTower_Left", new Vector3(-17f, 2.2f, 0.9f), new Vector3(1.4f, 6.8f, 0.7f), new Color(0.08f, 0.1f, 0.13f, 1f), repairs);
            EnsureVisualCube(environment, "ScaleReferenceTower_Right", new Vector3(23f, 2.4f, 0.9f), new Vector3(1.6f, 7.2f, 0.7f), new Color(0.08f, 0.1f, 0.13f, 1f), repairs);
            EnsurePlatformCube(platforms, "CinematicCameraPlatform_Right", new Vector3(18f, 0.4f, 0f), new Vector3(5.2f, 0.32f, 1.5f), repairs);
        }

        private static ParallaxLayer[] CreateOrConfigureParallaxLayers(Transform gameplayWorld, List<string> repairs)
        {
            Transform environment = GetOrCreateChild(gameplayWorld, "Environment", repairs);
            ParallaxLayer far = ConfigureLayer(environment, "Background_Far", new Vector2(0.15f, 0.15f), -1.9f, new Color(0.015f, 0.018f, 0.026f, 1f), repairs);
            ParallaxLayer mid = ConfigureLayer(environment, "Background_Mid", new Vector2(0.35f, 0.25f), -1.25f, new Color(0.025f, 0.035f, 0.05f, 1f), repairs);
            ParallaxLayer near = ConfigureLayer(environment, "Background_Near", new Vector2(0.55f, 0.35f), -0.65f, new Color(0.045f, 0.06f, 0.08f, 1f), repairs);
            ParallaxLayer foreground = ConfigureLayer(environment, "Foreground_Silhouette", new Vector2(0.85f, 0.5f), -0.2f, new Color(0.01f, 0.012f, 0.016f, 1f), repairs);
            return new[] { far, mid, near, foreground };
        }

        private static ParallaxLayer ConfigureLayer(Transform parent, string name, Vector2 factor, float z, Color color, List<string> repairs)
        {
            Transform root = FindDirectChild(parent, name);
            if (root == null)
            {
                GameObject rootObject = new GameObject(name);
                root = rootObject.transform;
                root.SetParent(parent, false);
                repairs.Add("Created parallax layer " + name);
            }

            root.localPosition = new Vector3(0f, 0f, z);
            AssignLayerRecursively(root.gameObject, GameplayWorldLayerName);

            if (root.childCount == 0)
            {
                CreateLayerSilhouettes(root, name, color);
                repairs.Add("Created placeholder silhouettes for " + name);
            }

            RemoveColliders(root);
            ParallaxLayer layer = GetOrCreateComponent<ParallaxLayer>(root.gameObject, repairs);
            SerializedObject serializedObject = new SerializedObject(layer);
            SetObject(serializedObject, "layerRoot", root);
            SetVector2(serializedObject, "parallaxFactor", factor);
            SetBool(serializedObject, "applyYParallax", false);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layer);
            EditorUtility.SetDirty(root);
            return layer;
        }

        private static void CreateLayerSilhouettes(Transform parent, string name, Color color)
        {
            float baseHeight = name.Contains("Far") ? 3.2f : name.Contains("Mid") ? 4.4f : 5.8f;
            for (int i = 0; i < 9; i++)
            {
                float x = -24f + i * 7f;
                float height = baseHeight + (i % 3) * 1.3f;
                EnsureVisualCube(parent, name + "_Block_" + (i + 1), new Vector3(x, height * 0.5f - 1.4f, 0f), new Vector3(2.2f + i % 2, height, 0.25f), color, null);
            }

            EnsureVisualCube(parent, name + "_Bridge", new Vector3(4f, 4.1f, 0f), new Vector3(22f, 0.28f, 0.22f), color, null);
            EnsureVisualCube(parent, name + "_Pipe", new Vector3(-8f, 2.2f, 0f), new Vector3(14f, 0.2f, 0.18f), color, null);
        }

        private static void EnsureVisualCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color, List<string> repairs)
        {
            Transform existing = FindInChildrenByName(parent, name);
            GameObject cube;
            if (existing == null)
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = name;
                cube.transform.SetParent(parent, false);
                Object.DestroyImmediate(cube.GetComponent<Collider>());
                repairs?.Add("Created visual block " + name);
            }
            else
            {
                cube = existing.gameObject;
                RemoveColliders(cube.transform);
            }

            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            AssignLayerRecursively(cube, GameplayWorldLayerName);
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
            }

            EditorUtility.SetDirty(cube);
        }

        private static void EnsurePlatformCube(Transform parent, string name, Vector3 position, Vector3 scale, List<string> repairs)
        {
            Transform existing = FindInChildrenByName(parent, name);
            GameObject cube = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (existing == null)
            {
                cube.name = name;
                cube.transform.SetParent(parent, false);
                repairs.Add("Created platform " + name);
            }

            cube.transform.position = position;
            cube.transform.localScale = scale;
            AssignLayerRecursively(cube, GameplayWorldLayerName);
            EditorUtility.SetDirty(cube);
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color
            };
            return material;
        }

        private static void ValidateScene(Scene scene, List<string> errors)
        {
            Transform gameplayWorld = FindSceneTransform(scene, "GameplayWorld");
            Transform rig = FindSceneTransform(scene, "GameplayCameraRig");
            GameWindowCamera gameWindowCamera = FindSingleGameWindowCamera(scene, errors);
            Camera gameplayCamera = gameWindowCamera != null ? gameWindowCamera.GameplayCamera : null;
            CinematicSideScrollerCameraController controller = rig != null ? rig.GetComponent<CinematicSideScrollerCameraController>() : null;
            CameraBounds2D bounds = rig != null ? rig.GetComponent<CameraBounds2D>() : null;

            if (gameplayCamera == null)
            {
                errors.Add("GameplayCamera is missing.");
                return;
            }

            if (!gameplayCamera.orthographic)
            {
                errors.Add("GameplayCamera is not orthographic.");
            }

            if (gameplayCamera.targetTexture == null && !HasRuntimeRenderTextureTarget(scene, gameWindowCamera))
            {
                errors.Add("GameplayCamera has no target RenderTexture or RuntimeRenderTexture assignment.");
            }

            if (rig == null || !gameplayCamera.transform.IsChildOf(rig))
            {
                errors.Add("GameplayCamera is not under GameplayCameraRig.");
            }

            if (gameplayWorld != null && rig != null && gameplayWorld.IsChildOf(rig))
            {
                errors.Add("GameplayWorld is under GameplayCameraRig.");
            }

            if (gameplayWorld != null && gameplayWorld.IsChildOf(gameplayCamera.transform))
            {
                errors.Add("GameplayWorld is under GameplayCamera.");
            }

            if (controller == null)
            {
                errors.Add("CinematicSideScrollerCameraController is missing.");
            }
            else
            {
                if (controller.CameraRig != rig)
                {
                    errors.Add("Camera controller is not assigned to control GameplayCameraRig.");
                }

                if (!IsValidPlayerRoot(controller.FollowTarget))
                {
                    errors.Add("Follow target should be PrototypePlayer or a player root, not a model bone.");
                }
            }

            if (bounds == null)
            {
                errors.Add("CameraBounds2D is missing.");
            }

            if (!IsPlayerVisible(gameplayCamera, controller != null ? controller.FollowTarget : null))
            {
                errors.Add("Player is not visible inside GameplayCamera view.");
            }

            if (!MainGameWindowReceivesGameplayOutput(scene, gameWindowCamera))
            {
                errors.Add("MainGameWindow does not receive GameplayCamera output.");
            }

            if (!UtilityViewCameraSyncLooksValid(scene, gameplayCamera))
            {
                errors.Add("UtilityViewCamera sync references are missing or invalid.");
            }

            if (CountActiveGameplayCameras(scene) > 1)
            {
                errors.Add("More than one active GameplayCamera/GameWindowCamera exists.");
            }

            if (CountActive<PlayerInput>(scene) > 1)
            {
                errors.Add("More than one active PlayerInput exists.");
            }

            ValidateParallaxLayers(scene, errors);
        }

        private static bool UtilityViewCameraSyncLooksValid(Scene scene, Camera gameplayCamera)
        {
            Camera utilityCamera = null;
            Transform utilityTransform = FindSceneTransform(scene, "UtilityViewCamera");
            if (utilityTransform != null)
            {
                utilityCamera = utilityTransform.GetComponent<Camera>();
            }

            if (utilityCamera == null)
            {
                return true;
            }

            UtilityWindowRenderController[] renderControllers = FindComponentsInScene<UtilityWindowRenderController>(scene);
            for (int i = 0; i < renderControllers.Length; i++)
            {
                if (renderControllers[i].SourceGameplayCamera == gameplayCamera && renderControllers[i].UtilityViewCamera == utilityCamera)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateParallaxLayers(Scene scene, List<string> errors)
        {
            ParallaxLayer[] layers = FindComponentsInScene<ParallaxLayer>(scene);
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null || layers[i].name.Contains("Platform"))
                {
                    continue;
                }

                Collider[] colliders = layers[i].GetComponentsInChildren<Collider>(true);
                if (colliders.Length > 0)
                {
                    errors.Add("Parallax layer " + layers[i].name + " contains colliders.");
                }
            }
        }

        private static bool IsPlayerVisible(Camera camera, Transform player)
        {
            if (camera == null || player == null)
            {
                return false;
            }

            Vector3 viewportPoint = camera.WorldToViewportPoint(player.position);
            return viewportPoint.z > 0f
                && viewportPoint.x >= 0f
                && viewportPoint.x <= 1f
                && viewportPoint.y >= 0f
                && viewportPoint.y <= 1f;
        }

        private static bool IsValidPlayerRoot(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            return target.name == "PrototypePlayer"
                || target.GetComponent<PlayerStateMachine>() != null
                || target.GetComponent<PlayerInput>() != null
                || target.GetComponent<Rigidbody>() != null;
        }

        private static bool MainGameWindowReceivesGameplayOutput(Scene scene, GameWindowCamera gameWindowCamera)
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
                    && rawImageProperty.objectReferenceValue is RawImage)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRuntimeRenderTextureTarget(Scene scene, GameWindowCamera gameWindowCamera)
        {
            return MainGameWindowReceivesGameplayOutput(scene, gameWindowCamera);
        }

        private static Transform FindUnambiguousPlayer(Scene scene, List<string> errors)
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
                errors.Add("More than one PlayerStateMachine found; follow target was not assigned.");
            }

            return namedPlayer;
        }

        private static GameWindowCamera FindSingleGameWindowCamera(Scene scene, List<string> errors)
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
                errors.Add("Multiple GameWindowCamera components found and none is named GameplayCamera.");
            }

            return namedCamera;
        }

        private static int CountActiveGameplayCameras(Scene scene)
        {
            GameWindowCamera[] cameras = FindComponentsInScene<GameWindowCamera>(scene);
            int count = 0;
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.activeInHierarchy && cameras[i].GameplayCamera != null && cameras[i].GameplayCamera.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActive<T>(Scene scene) where T : Component
        {
            T[] components = FindComponentsInScene<T>(scene);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
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
            repairs.Add("Created " + parent.name + "/" + name);
            return childObject.transform;
        }

        private static T GetOrCreateComponent<T>(GameObject gameObject, List<string> repairs) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = gameObject.AddComponent<T>();
            repairs.Add("Added " + typeof(T).Name + " to " + gameObject.name);
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

        private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
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

        private static void RemoveColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }
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

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning("[Cinematic Camera] Could not create layer '" + layerName + "'. Add it manually in Project Settings > Tags and Layers.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
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
        }

        private static void LogErrors(List<string> errors)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError("[Cinematic Camera] " + errors[i]);
            }
        }
    }
}
