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
    public static class DesktopHostPrototypeMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string WindowPrefabPath = "Assets/_Project/Prefabs/PrototypeDesktopWindow.prefab";
        private const string GameplayInputPath = "Assets/_Project/Settings/GameplayInputActions.inputactions";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PuzzleObjectLayerName = "PuzzleObject";

        [MenuItem("Tools/OwariNakiTobira/Create Desktop Host Prototype")]
        public static void CreateDesktopHostPrototype()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Overwrite Desktop Host Prototype", "DesktopHostPrototype already exists. Overwrite it?", "Overwrite", "Cancel"))
            {
                return;
            }

            EnsureProjectLayers();
            EnsurePrototypeWindowPrefab();
            AssetDatabase.ImportAsset(GameplayInputPath, ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject host = new GameObject("DesktopHost");

            GameObject systems = CreateChild(host.transform, "Systems");
            DesktopWindowManager manager = CreateChild(systems.transform, "DesktopWindowManager").AddComponent<DesktopWindowManager>();
            PlayerControlGate playerControlGate = CreateChild(systems.transform, "PlayerControlGate").AddComponent<PlayerControlGate>();
            RuntimeResetService runtimeResetService = CreateChild(systems.transform, "RuntimeResetService").AddComponent<RuntimeResetService>();
            GameFlowController gameFlowController = CreateChild(systems.transform, "GameFlowController").AddComponent<GameFlowController>();
            StoryFlagService storyFlagService = CreateChild(systems.transform, "StoryFlagService").AddComponent<StoryFlagService>();
            StorySequenceRunner storySequenceRunner = CreateChild(systems.transform, "StorySequenceRunner").AddComponent<StorySequenceRunner>();
            WindowDragPlayerLock dragPlayerLock = systems.AddComponent<WindowDragPlayerLock>();
            AssignObject(gameFlowController, "runtimeResetService", runtimeResetService);
            AssignObject(storyFlagService, "runtimeResetService", runtimeResetService);
            AssignObject(storySequenceRunner, "storyFlagService", storyFlagService);
            AssignObject(storySequenceRunner, "playerControlGate", playerControlGate);
            AssignObject(storySequenceRunner, "runtimeResetService", runtimeResetService);

            GameObject desktopWorld = CreateChild(host.transform, "DesktopWorld");
            AssignLayerRecursively(desktopWorld, DesktopWorldLayerName);
            Camera desktopCamera = CreateDesktopCamera(desktopWorld.transform);
            CreateDesktopFloor(desktopWorld.transform);
            CreatePlaceholderIcons(desktopWorld.transform);

            GameObject gameplayWorld = CreateChild(host.transform, "GameplayWorld");
            AssignLayerRecursively(gameplayWorld, GameplayWorldLayerName);
            GameWindowCamera gameWindowCamera = CreateGameplayCamera(gameplayWorld.transform);
            CreateGameplayFloor(gameplayWorld.transform);
            CreatePlaceholderBuildings(gameplayWorld.transform);
            WindowPuzzleTarget coverPuzzleWall = CreateCoverPuzzleWall(gameplayWorld.transform);
            CreateDestinationPlatform(gameplayWorld.transform);
            CreatePrototypePlayer(gameplayWorld.transform, playerControlGate);

            GameObject ui = CreateChild(host.transform, "UI");
            Canvas canvas = CreateCanvas(ui.transform, desktopCamera);
            CreateEventSystem(ui.transform);

            RectTransform desktopWindowLayer = CreateFullScreenLayer(canvas.transform, "DesktopWindowLayer");
            manager.SetDesktopBounds(desktopWindowLayer);
            AssignObject(dragPlayerLock, "windowManager", manager);
            AssignObject(dragPlayerLock, "playerControlGate", playerControlGate);

            DesktopWindowController mainGameWindow = CreateWindowInstance(desktopWindowLayer, manager, "MainGameWindow", "Main Game Window", new Vector2(-110f, 20f), new Vector2(900f, 520f), true);
            RawImage gameplayRawImage = AddGameWindowContent(mainGameWindow, gameWindowCamera);
            RuntimeRenderTexture runtimeRenderTexture = mainGameWindow.gameObject.AddComponent<RuntimeRenderTexture>();
            GameWindowView gameWindowView = mainGameWindow.gameObject.GetComponent<GameWindowView>();
            AssignObject(runtimeRenderTexture, "gameWindowCamera", gameWindowCamera);
            AssignObject(runtimeRenderTexture, "targetRawImage", gameplayRawImage);
            AssignObject(runtimeRenderTexture, "gameWindowView", gameWindowView);

            DesktopWindowController utilityWindow = CreateWindowInstance(desktopWindowLayer, manager, "UtilityWindow", "Utility Window", new Vector2(420f, -190f), new Vector2(320f, 220f), true);
            AddUtilityWindowContent(utilityWindow);
            CoverToEraseRule coverRule = CreateCoverPuzzleRule(systems.transform, utilityWindow, gameWindowCamera, gameWindowView, coverPuzzleWall);

            CreateFullScreenLayer(canvas.transform, "DialogueLayer");
            RectTransform fadeLayer = CreateFullScreenLayer(canvas.transform, "FadeLayer");
            Image fadeImage = fadeLayer.gameObject.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
            CanvasGroup fadeCanvasGroup = fadeLayer.gameObject.AddComponent<CanvasGroup>();
            ScreenFadeView screenFadeView = fadeLayer.gameObject.AddComponent<ScreenFadeView>();
            AssignObject(screenFadeView, "canvasGroup", fadeCanvasGroup);
            AssignObject(storySequenceRunner, "screenFadeView", screenFadeView);
            AssignObjectArray(runtimeResetService, "initialResetTargets", playerControlGate, storyFlagService, storySequenceRunner, coverRule, coverPuzzleWall);
            screenFadeView.SetAlpha(0f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Selection.activeGameObject = host;
        }

        private static void EnsurePrototypeWindowPrefab()
        {
            if (File.Exists(WindowPrefabPath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(WindowPrefabPath));
            GameObject window = CreateWindowObject("PrototypeDesktopWindow", "Prototype Window", new Vector2(480f, 300f));
            PrefabUtility.SaveAsPrefabAsset(window, WindowPrefabPath);
            UnityEngine.Object.DestroyImmediate(window);
            AssetDatabase.ImportAsset(WindowPrefabPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static DesktopWindowController CreateWindowInstance(RectTransform parent, DesktopWindowManager manager, string name, string title, Vector2 position, Vector2 size, bool draggable)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            GameObject window = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent)
                : CreateWindowObject(name, title, size);

            window.name = name;
            RectTransform rectTransform = window.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;

            DesktopWindowModel model = window.GetComponent<DesktopWindowModel>();
            model.RegenerateId();
            model.SetTitle(title);
            model.SetPosition(position);
            model.SetSize(size);
            model.SetDraggingAllowed(draggable);
            model.SetVisible(true);

            DesktopWindowController controller = window.GetComponent<DesktopWindowController>();
            controller.SetManager(manager);
            controller.ApplyModelToView();
            return controller;
        }

        private static GameObject CreateWindowObject(string name, string title, Vector2 size)
        {
            GameObject window = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform root = window.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;
            Image background = window.GetComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

            RectTransform titleBar = CreateRectChild(root, "TitleBar");
            titleBar.anchorMin = new Vector2(0f, 1f);
            titleBar.anchorMax = new Vector2(1f, 1f);
            titleBar.pivot = new Vector2(0.5f, 1f);
            titleBar.sizeDelta = new Vector2(0f, 34f);
            titleBar.anchoredPosition = Vector2.zero;
            Image titleBarImage = titleBar.gameObject.AddComponent<Image>();
            titleBarImage.color = new Color(0.13f, 0.15f, 0.18f, 1f);

            TextMeshProUGUI titleText = CreateText(titleBar, "Title", title, 18f, TextAlignmentOptions.MidlineLeft);
            RectTransform titleTextRect = titleText.rectTransform;
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.offsetMin = new Vector2(12f, 0f);
            titleTextRect.offsetMax = new Vector2(-42f, 0f);

            Button closeButton = CreateCloseButton(titleBar);

            RectTransform contentRoot = CreateRectChild(root, "ContentRoot");
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = new Vector2(8f, 8f);
            contentRoot.offsetMax = new Vector2(-8f, -42f);

            DesktopWindowModel model = window.AddComponent<DesktopWindowModel>();
            model.SetTitle(title);
            model.SetSize(size);
            model.SetMinimumSize(new Vector2(220f, 140f));

            DesktopWindowView view = window.AddComponent<DesktopWindowView>();
            AssignObject(view, "windowRoot", root);
            AssignObject(view, "titleBar", titleBar);
            AssignObject(view, "contentRoot", contentRoot);
            AssignObject(view, "titleText", titleText);
            AssignObject(view, "closeButton", closeButton);
            AssignGraphicArray(view, "borderVisuals", background, titleBarImage);

            window.AddComponent<DesktopWindowController>();
            return window;
        }

        private static RawImage AddGameWindowContent(DesktopWindowController window, GameWindowCamera gameWindowCamera)
        {
            RectTransform contentRoot = window.View.ContentRoot;
            GameObject rawObject = new GameObject("GameplayRenderTexture", typeof(RectTransform), typeof(RawImage));
            RectTransform rawRect = rawObject.GetComponent<RectTransform>();
            rawRect.SetParent(contentRoot, false);
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;
            RawImage rawImage = rawObject.GetComponent<RawImage>();
            rawImage.color = Color.white;

            GameWindowView gameWindowView = window.gameObject.AddComponent<GameWindowView>();
            AssignObject(gameWindowView, "rawImage", rawImage);
            AssignVector2Int(gameWindowView, "renderResolution", gameWindowCamera.RenderResolution);
            gameWindowView.ApplyAspectRatio();
            return rawImage;
        }

        private static void AddUtilityWindowContent(DesktopWindowController window)
        {
            TextMeshProUGUI text = CreateText(window.View.ContentRoot, "PlaceholderContent", "Utility window\nDrag this by its title bar.", 16f, TextAlignmentOptions.Center);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Camera CreateDesktopCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "DesktopCamera");
            cameraObject.transform.position = new Vector3(0f, 5f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.depth = 0f;
            DesktopCameraController controller = cameraObject.AddComponent<DesktopCameraController>();
            AssignObject(controller, "desktopCamera", camera);
            AssignLayerMask(controller, "desktopLayers", LayerMask.GetMask(DesktopWorldLayerName));
            return camera;
        }

        private static GameWindowCamera CreateGameplayCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "GameplayCamera");
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.depth = -10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            GameWindowCamera gameWindowCamera = cameraObject.AddComponent<GameWindowCamera>();
            AssignObject(gameWindowCamera, "gameplayCamera", camera);
            AssignLayerMask(gameWindowCamera, "gameplayLayers", LayerMask.GetMask(GameplayWorldLayerName, GameplayPlayerLayerName, PuzzleObjectLayerName));
            AssignVector2Int(gameWindowCamera, "renderResolution", new Vector2Int(1280, 720));
            return gameWindowCamera;
        }

        private static void CreateDesktopFloor(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DesktopFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -1.2f, 0f);
            floor.transform.localScale = new Vector3(14f, 0.25f, 4f);
            AssignLayerRecursively(floor, DesktopWorldLayerName);
        }

        private static void CreatePlaceholderIcons(Transform parent)
        {
            GameObject iconsRoot = CreateChild(parent, "PlaceholderIcons");
            AssignLayerRecursively(iconsRoot, DesktopWorldLayerName);
            for (int i = 0; i < 5; i++)
            {
                GameObject icon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                icon.name = "PlaceholderIcon_" + (i + 1);
                icon.transform.SetParent(iconsRoot.transform, false);
                icon.transform.position = new Vector3(-5.5f + i * 1.2f, 0f, 1.1f);
                icon.transform.localScale = new Vector3(0.55f, 0.55f, 0.08f);
                AssignLayerRecursively(icon, DesktopWorldLayerName);
            }
        }

        private static void CreateGameplayFloor(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "GameplayFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -1.25f, 0f);
            floor.transform.localScale = new Vector3(15f, 0.4f, 2f);
            AssignLayerRecursively(floor, GameplayWorldLayerName);
        }

        private static void CreatePlaceholderBuildings(Transform parent)
        {
            GameObject buildingsRoot = CreateChild(parent, "PlaceholderBuildings");
            AssignLayerRecursively(buildingsRoot, GameplayWorldLayerName);
            for (int i = 0; i < 6; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "PlaceholderBuilding_" + (i + 1);
                building.transform.SetParent(buildingsRoot.transform, false);
                building.transform.position = new Vector3(-5f + i * 2f, 0.6f + i % 3 * 0.25f, 0.8f);
                building.transform.localScale = new Vector3(0.8f, 2.4f + i % 3, 0.6f);
                AssignLayerRecursively(building, GameplayWorldLayerName);
            }
        }

        private static WindowPuzzleTarget CreateCoverPuzzleWall(Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CoverPuzzleWall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(-1.1f, 0.6f, 0f);
            wall.transform.localScale = new Vector3(0.75f, 3.1f, 1.2f);
            AssignLayerRecursively(wall, PuzzleObjectLayerName);

            WindowPuzzleTarget target = wall.AddComponent<WindowPuzzleTarget>();
            target.SetAffectedObjects(
                new[] { wall.GetComponent<Renderer>() },
                new[] { wall.GetComponent<Collider>() });
            return target;
        }

        private static void CreateDestinationPlatform(Transform parent)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "DestinationPlatform";
            platform.transform.SetParent(parent, false);
            platform.transform.position = new Vector3(4.2f, 0.25f, 0f);
            platform.transform.localScale = new Vector3(2.6f, 0.3f, 1.5f);
            AssignLayerRecursively(platform, GameplayWorldLayerName);
        }

        private static CoverToEraseRule CreateCoverPuzzleRule(Transform parent, DesktopWindowController utilityWindow, GameWindowCamera gameWindowCamera, GameWindowView gameWindowView, WindowPuzzleTarget target)
        {
            GameObject ruleObject = CreateChild(parent, "CoverToEraseRule");
            CoverToEraseRule rule = ruleObject.AddComponent<CoverToEraseRule>();
            AssignObject(rule, "coveringWindow", utilityWindow);
            AssignObject(rule, "gameWindowCamera", gameWindowCamera);
            AssignObject(rule, "gameWindowView", gameWindowView);
            AssignObjectArray(rule, "targets", target);

            PuzzleRuleEnableController enableController = ruleObject.AddComponent<PuzzleRuleEnableController>();
            AssignObjectArray(enableController, "rules", rule);

            WindowPuzzleDebugOverlay overlay = ruleObject.AddComponent<WindowPuzzleDebugOverlay>();
            AssignObject(overlay, "rule", rule);
            return rule;
        }

        private static void CreatePrototypePlayer(Transform parent, PlayerControlGate playerControlGate)
        {
            GameObject player = new GameObject("PrototypePlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-5.5f, 0.1f, 0f);
            AssignLayerRecursively(player, GameplayPlayerLayerName);

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.45f;

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
            UnityEngine.Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
            AssignLayerRecursively(visualRoot, GameplayPlayerLayerName);

            GameObject groundCheck = CreateChild(player.transform, "GroundCheck");
            groundCheck.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            AssignLayerRecursively(groundCheck, GameplayPlayerLayerName);

            AssignObject(inputReader, "inputActions", actions);
            AssignObject(motor, "groundCheck", groundCheck.transform);
            AssignObject(facing, "visualRoot", visualRoot.transform);
            AssignObject(stateMachine, "inputReader", inputReader);
            AssignObject(stateMachine, "motor", motor);
            AssignObject(stateMachine, "controlGate", playerControlGate);
            AssignObject(stateMachine, "facingController", facing);
            AssignObject(stateMachine, "animatorBridge", animatorBridge);
        }

        private static Canvas CreateCanvas(Transform parent, Camera desktopCamera)
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
            GameObject eventSystemObject = CreateChild(parent, "EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static RectTransform CreateFullScreenLayer(Transform parent, string name)
        {
            RectTransform rectTransform = CreateRectChild(parent, name);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
        }

        private static Button CreateCloseButton(RectTransform parent)
        {
            GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-6f, 0f);
            rect.sizeDelta = new Vector2(26f, 24f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.45f, 0.12f, 0.14f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI label = CreateText(rect, "Label", "X", 14f, TextAlignmentOptions.Center);
            label.raycastTarget = false;
            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
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
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void EnsureProjectLayers()
        {
            EnsureLayer(DesktopWorldLayerName);
            EnsureLayer(GameplayWorldLayerName);
            EnsureLayer(GameplayPlayerLayerName);
            EnsureLayer(PuzzleObjectLayerName);
        }

        private static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
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

        private static void AssignLayerRecursively(GameObject root, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer '{layerName}' is missing. '{root.name}' remains on Default until the layer is added manually.");
                return;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = layer;
            }
        }

        private static void AssignObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignLayerMask(UnityEngine.Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignVector2Int(UnityEngine.Object target, string propertyName, Vector2Int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector2IntValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignGraphicArray(UnityEngine.Object target, string propertyName, params Graphic[] values)
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

        private static void AssignObjectArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
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
