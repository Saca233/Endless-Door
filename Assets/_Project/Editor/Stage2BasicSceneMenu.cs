using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OwariNakiTobira.Editor
{
    public static class Stage2BasicSceneMenu
    {
        private const string Stage2ScenePath = "Assets/_Project/Scenes/stage2.unity";
        private const string GameplayInputPath = "Assets/_Project/Settings/GameplayInputActions.inputactions";
        private const string NoFrictionPath = "Assets/_Project/NoFriction.physicMaterial";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";

        [MenuItem("Tools/OwariNakiTobira/Rebuild Stage2 Basic Scene")]
        public static void RebuildStage2BasicScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureLayer(DesktopWorldLayerName);
            EnsureLayer(GameplayWorldLayerName);
            EnsureLayer(GameplayPlayerLayerName);
            EnsureNoFrictionMaterial();
            AssetDatabase.ImportAsset(GameplayInputPath, ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Stage2_Basic");

            GameObject systems = CreateChild(root.transform, "Systems");
            DesktopWindowManager manager = CreateChild(systems.transform, "DesktopWindowManager").AddComponent<DesktopWindowManager>();
            PlayerControlGate controlGate = CreateChild(systems.transform, "PlayerControlGate").AddComponent<PlayerControlGate>();

            GameObject desktopWorld = CreateChild(root.transform, "DesktopWorld");
            AssignLayerRecursively(desktopWorld, DesktopWorldLayerName);
            CreateDesktopCamera(desktopWorld.transform);
            CreateDesktopBackdrop(desktopWorld.transform);

            GameObject gameplayWorld = CreateChild(root.transform, "GameplayWorld");
            AssignLayerRecursively(gameplayWorld, GameplayWorldLayerName);
            Transform environment = CreateChild(gameplayWorld.transform, "Environment").transform;
            Transform platforms = CreateChild(gameplayWorld.transform, "Platforms").transform;
            Transform obstacles = CreateChild(gameplayWorld.transform, "Obstacles").transform;
            Transform playerGroup = CreateChild(gameplayWorld.transform, "Player").transform;
            root.AddComponent<StageCoverPuzzleAutoBinder>();
            CreateStage2Spawn(gameplayWorld.transform);
            CreateGameplayGround(environment);
            CreateStage2Silhouettes(environment);
            CreateStage2Platforms(platforms);
            CreateStage2CoverPuzzleWall(obstacles);
            Transform player = CreatePrototypePlayer(playerGroup, controlGate);

            GameObject cameraRig = CreateChild(root.transform, "GameplayCameraRig");
            cameraRig.transform.position = new Vector3(0f, 2.6f, -10f);
            GameWindowCamera gameWindowCamera = CreateGameplayCamera(cameraRig.transform);
            ConfigureCameraRig(cameraRig, gameWindowCamera.GameplayCamera, player);

            GameObject ui = CreateChild(root.transform, "UI");
            Canvas canvas = CreateCanvas(ui.transform);
            CreateEventSystem(ui.transform);
            RectTransform desktopWindowLayer = CreateFullScreenLayer(canvas.transform, "DesktopWindowLayer");
            manager.SetDesktopBounds(desktopWindowLayer);

            DesktopWindowController mainWindow = CreateWindow(desktopWindowLayer, manager);
            RawImage rawImage = AddMainGameWindowContent(mainWindow, gameWindowCamera);
            RuntimeRenderTexture renderTexture = mainWindow.gameObject.AddComponent<RuntimeRenderTexture>();
            AssignObject(renderTexture, "gameWindowCamera", gameWindowCamera);
            AssignObject(renderTexture, "targetRawImage", rawImage);
            AssignObject(renderTexture, "gameWindowView", mainWindow.GetComponent<GameWindowView>());

            Directory.CreateDirectory(Path.GetDirectoryName(Stage2ScenePath));
            EditorSceneManager.SaveScene(scene, Stage2ScenePath);
            EnsureSceneInBuildSettings(Stage2ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log("[Stage2] Rebuilt clean basic stage2 scene with one MainGameWindow and no UtilityWindow.");
        }

        private static void CreateDesktopCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "DesktopCamera");
            cameraObject.transform.position = new Vector3(0f, 4.2f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.012f, 0.018f, 1f);
            camera.cullingMask = LayerMask.GetMask(DesktopWorldLayerName);
            DesktopCameraController controller = cameraObject.AddComponent<DesktopCameraController>();
            AssignObject(controller, "desktopCamera", camera);
            AssignLayerMask(controller, "desktopLayers", LayerMask.GetMask(DesktopWorldLayerName));
        }

        private static void CreateDesktopBackdrop(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Stage2DesktopBackdrop";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -1.35f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.22f, 5f);
            AssignLayerRecursively(floor, DesktopWorldLayerName);
            SetRendererColor(floor, new Color(0.025f, 0.028f, 0.04f, 1f));
        }

        private static void CreateGameplayGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Stage2Ground";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(4f, -1.25f, 0f);
            ground.transform.localScale = new Vector3(30f, 0.42f, 2f);
            AssignLayerRecursively(ground, GameplayWorldLayerName);
            AssignNoFriction(ground);
            SetRendererColor(ground, new Color(0.12f, 0.13f, 0.16f, 1f));
        }

        private static void CreateStage2Silhouettes(Transform parent)
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "Stage2Building_" + (i + 1);
                block.transform.SetParent(parent, false);
                block.transform.position = new Vector3(-8f + i * 3.2f, 0.7f + i % 3 * 0.35f, 1f);
                block.transform.localScale = new Vector3(0.9f, 2.6f + i % 4, 0.55f);
                AssignLayerRecursively(block, GameplayWorldLayerName);
                Object.DestroyImmediate(block.GetComponent<Collider>());
                SetRendererColor(block, new Color(0.045f, 0.055f, 0.075f, 1f));
            }
        }

        private static void CreateStage2Platforms(Transform parent)
        {
            CreatePlatform(parent, "Stage2Platform_01", new Vector3(3.5f, 0.1f, 0f), new Vector3(2.8f, 0.28f, 1.5f));
            CreatePlatform(parent, "Stage2Platform_02", new Vector3(8.5f, 0.85f, 0f), new Vector3(2.6f, 0.28f, 1.5f));
        }

        private static void CreateStage2Spawn(Transform parent)
        {
            GameObject spawn = CreateChild(parent, "Stage2Spawn");
            spawn.transform.position = new Vector3(-6f, 0.05f, 0f);
        }

        private static void CreateStage2CoverPuzzleWall(Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CoverPuzzleWall_Stage2_01";
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(6.1f, 0.15f, 0f);
            wall.transform.localScale = new Vector3(0.8f, 2.5f, 1.2f);
            AssignLayerRecursively(wall, GameplayWorldLayerName);
            AssignNoFriction(wall);
            SetRendererColor(wall, new Color(0.2f, 0.23f, 0.3f, 1f));

            WindowPuzzleTarget target = wall.AddComponent<WindowPuzzleTarget>();
            target.SetAffectedObjects(
                new[] { wall.GetComponent<Renderer>() },
                new[] { wall.GetComponent<Collider>() });
        }

        private static void CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.transform.localScale = scale;
            AssignLayerRecursively(platform, GameplayWorldLayerName);
            AssignNoFriction(platform);
            SetRendererColor(platform, new Color(0.16f, 0.17f, 0.2f, 1f));
        }

        private static Transform CreatePrototypePlayer(Transform parent, PlayerControlGate controlGate)
        {
            GameObject player = new GameObject("PrototypePlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-6f, 0.05f, 0f);
            AssignLayerRecursively(player, GameplayPlayerLayerName);
            player.tag = "Player";

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.45f;
            AssignNoFriction(player);

            PlayerInput playerInput = player.AddComponent<PlayerInput>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(GameplayInputPath);
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Gameplay";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            PlayerInputReader inputReader = player.AddComponent<PlayerInputReader>();
            SideScrollerMotor motor = player.AddComponent<SideScrollerMotor>();
            PlayerFacingController facing = player.AddComponent<PlayerFacingController>();
            PlayerAnimatorBridge animatorBridge = player.AddComponent<PlayerAnimatorBridge>();
            PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();

            GameObject visualRoot = CreateChild(player.transform, "VisualRoot");
            GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleVisual.name = "CapsuleVisual";
            capsuleVisual.transform.SetParent(visualRoot.transform, false);
            Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
            AssignLayerRecursively(visualRoot, GameplayPlayerLayerName);
            SetRendererColor(capsuleVisual, new Color(0.65f, 0.78f, 1f, 1f));

            GameObject groundCheck = CreateChild(player.transform, "GroundCheck");
            groundCheck.transform.localPosition = new Vector3(0f, -1.05f, 0f);

            AssignObject(inputReader, "inputActions", actions);
            AssignObject(motor, "groundCheck", groundCheck.transform);
            AssignFloat(motor, "maximumSpeed", 4f);
            AssignFloat(motor, "jumpVelocity", 6f);
            AssignFloat(motor, "coyoteTime", 0.08f);
            AssignFloat(motor, "jumpBufferTime", 0.1f);
            AssignFloat(motor, "groundCheckRadius", 0.12f);
            AssignLayerMask(motor, "groundLayers", LayerMask.GetMask(GameplayWorldLayerName));
            AssignObject(facing, "visualRoot", visualRoot.transform);
            AssignObject(stateMachine, "inputReader", inputReader);
            AssignObject(stateMachine, "motor", motor);
            AssignObject(stateMachine, "controlGate", controlGate);
            AssignObject(stateMachine, "facingController", facing);
            AssignObject(stateMachine, "animatorBridge", animatorBridge);
            return player.transform;
        }

        private static GameWindowCamera CreateGameplayCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "GameplayCamera");
            cameraObject.transform.localPosition = Vector3.zero;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.018f, 0.028f, 1f);
            GameWindowCamera gameWindowCamera = cameraObject.AddComponent<GameWindowCamera>();
            AssignObject(gameWindowCamera, "gameplayCamera", camera);
            AssignLayerMask(gameWindowCamera, "gameplayLayers", LayerMask.GetMask(GameplayWorldLayerName, GameplayPlayerLayerName));
            AssignVector2Int(gameWindowCamera, "renderResolution", new Vector2Int(1280, 720));
            return gameWindowCamera;
        }

        private static void ConfigureCameraRig(GameObject cameraRig, Camera gameplayCamera, Transform followTarget)
        {
            SideScrollerCameraBounds bounds = cameraRig.AddComponent<SideScrollerCameraBounds>();
            bounds.Configure(new Vector2(-10f, -2.5f), new Vector2(18f, 6f), true);

            SideScrollerCameraController controller = cameraRig.AddComponent<SideScrollerCameraController>();
            AssignObject(controller, "cameraRig", cameraRig.transform);
            AssignObject(controller, "gameplayCamera", gameplayCamera);
            AssignObject(controller, "followTarget", followTarget);
            AssignObject(controller, "bounds", bounds);
            AssignBool(controller, "followY", false);
            AssignFloat(controller, "smoothTime", 0.18f);
            AssignFloat(controller, "lookAheadDistance", 1.2f);
            AssignFloat(controller, "deadZoneWidth", 1.2f);
            AssignFloat(controller, "deadZoneHeight", 0.7f);
            AssignFloat(controller, "fixedZPosition", -10f);
            controller.SnapToTarget();
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = CreateChild(parent, "Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem(Transform parent)
        {
            GameObject eventSystem = CreateChild(parent, "EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static DesktopWindowController CreateWindow(RectTransform parent, DesktopWindowManager manager)
        {
            GameObject window = new GameObject("MainGameWindow", typeof(RectTransform), typeof(Image));
            RectTransform root = window.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(960f, 560f);
            Image background = window.GetComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            RectTransform titleBar = CreateRectChild(root, "TitleBar");
            titleBar.anchorMin = new Vector2(0f, 1f);
            titleBar.anchorMax = new Vector2(1f, 1f);
            titleBar.pivot = new Vector2(0.5f, 1f);
            titleBar.sizeDelta = new Vector2(0f, 34f);
            Image titleImage = titleBar.gameObject.AddComponent<Image>();
            titleImage.color = new Color(0.13f, 0.15f, 0.18f, 1f);

            TextMeshProUGUI titleText = CreateText(titleBar, "Title", "Stage 2", 18f, TextAlignmentOptions.MidlineLeft);
            titleText.rectTransform.anchorMin = Vector2.zero;
            titleText.rectTransform.anchorMax = Vector2.one;
            titleText.rectTransform.offsetMin = new Vector2(12f, 0f);
            titleText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            titleText.raycastTarget = false;

            RectTransform contentRoot = CreateRectChild(root, "ContentRoot");
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = new Vector2(8f, 8f);
            contentRoot.offsetMax = new Vector2(-8f, -42f);

            DesktopWindowModel model = window.AddComponent<DesktopWindowModel>();
            model.RegenerateId();
            model.SetTitle("Stage 2");
            model.SetPosition(root.anchoredPosition);
            model.SetSize(root.sizeDelta);
            model.SetDraggingAllowed(false);
            model.SetVisible(true);

            DesktopWindowView view = window.AddComponent<DesktopWindowView>();
            AssignObject(view, "windowRoot", root);
            AssignObject(view, "titleBar", titleBar);
            AssignObject(view, "contentRoot", contentRoot);
            AssignObject(view, "titleText", titleText);
            AssignGraphicArray(view, "borderVisuals", background, titleImage);

            DesktopWindowController controller = window.AddComponent<DesktopWindowController>();
            controller.SetManager(manager);
            controller.ApplyModelToView();
            return controller;
        }

        private static RawImage AddMainGameWindowContent(DesktopWindowController window, GameWindowCamera camera)
        {
            GameObject rawObject = new GameObject("GameplayRenderTexture", typeof(RectTransform), typeof(RawImage));
            RectTransform rawRect = rawObject.GetComponent<RectTransform>();
            rawRect.SetParent(window.View.ContentRoot, false);
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;
            RawImage rawImage = rawObject.GetComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;

            GameWindowView view = window.gameObject.AddComponent<GameWindowView>();
            AssignObject(view, "rawImage", rawImage);
            AssignVector2Int(view, "renderResolution", camera.RenderResolution);
            view.ApplyAspectRatio();
            return rawImage;
        }

        private static RectTransform CreateFullScreenLayer(Transform parent, string name)
        {
            RectTransform rect = CreateRectChild(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = alignment;
            return label;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static RectTransform CreateRectChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void EnsureNoFrictionMaterial()
        {
            if (File.Exists(NoFrictionPath))
            {
                return;
            }

            PhysicsMaterial material = new PhysicsMaterial("NoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
        }

        private static void AssignNoFriction(GameObject target)
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(NoFrictionPath);
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].sharedMaterial = material;
            }
        }

        private static void SetRendererColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color
            };
            renderer.sharedMaterial = material;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
            {
                next[i] = scenes[i];
            }

            next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
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
            }
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

        private static void AssignBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignGraphicArray(Object target, string propertyName, params Graphic[] values)
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
    }
}
