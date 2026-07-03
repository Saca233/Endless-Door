using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace OwariNakiTobira.Editor
{
    public static class Level01GrayboxMenu
    {
        private const string HostScenePath = "Assets/_Project/Scenes/DesktopHost_Level01Graybox.unity";
        private const string Level01ScenePath = "Assets/_Project/Scenes/Level01_Prologue.unity";
        private const string Level02ScenePath = "Assets/_Project/Scenes/Level02_Placeholder.unity";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Level01_Prologue";
        private const string GameplayInputPath = "Assets/_Project/Settings/GameplayInputActions.inputactions";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PuzzleObjectLayerName = "PuzzleObject";
        private const string PlayerTagName = "Player";
        private const string TitleMessage = "\u7d42\u308f\u308a\u306a\u304d\u6249.exe";

        [MenuItem("Tools/OwariNakiTobira/Create Level01 Graybox")]
        public static void CreateLevel01Graybox()
        {
            if (File.Exists(HostScenePath) || File.Exists(Level01ScenePath) || File.Exists(Level02ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Level01 Graybox Already Exists",
                    "One or more Level01 graybox scenes already exist. Rename or remove the existing generated scenes before creating a new graybox.",
                    "OK");
                return;
            }

            EnsureProjectFolders();
            EnsureProjectLayers();
            EnsureTag(PlayerTagName);
            ImportInputActions();

            StoryAssets storyAssets = CreateStoryAssets();
            CreateHostScene(storyAssets);
            CreateLevel01Scene(storyAssets);
            CreateLevel02Scene(storyAssets.Level02Definition);
            EnsureBuildSettingsScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(HostScenePath, OpenSceneMode.Single);
            Debug.Log("Created Level01 graybox vertical slice. Start Play Mode from DesktopHost_Level01Graybox.");
        }

        private static StoryAssets CreateStoryAssets()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(DataFolder + "/Dialogue");
            EnsureFolder(DataFolder + "/Story");
            EnsureFolder(DataFolder + "/Levels");

            DialogueSequence openingDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level01_OpeningDialogue.asset",
                new[]
                {
                    "\u2026\u2026\u5582\uff1f\u5582\uff01\uff01\u5916\u9762\u7684\u4eba\u2026\u2026\u4f60\u80fd\u542c\u5230\u6211\u8bf4\u8bdd\u5417\uff1f",
                    "\u6211\u88ab\u56f0\u5728\u8fd9\u91cc\u4e86\u2026\u2026\u6574\u4e2a\u57ce\u5e02\u7684\u6570\u636e\u90fd\u88ab\u6e05\u7a7a\u4e86\uff0c\u53ea\u5269\u4e0b\u6211\u3002\u8fd9\u91cc\u597d\u51b7\u3002",
                    "\u6c42\u6c42\u4f60\uff0c\u522b\u5173\u6389\u8fd9\u4e2a\u7a97\u53e3\u3002\u5e2e\u5e2e\u6211\uff0c\u5e26\u6211\u79bb\u5f00\u8fd9\u3002"
                });

            DialogueSequence afterObstacleDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level01_AfterObstacleDialogue.asset",
                new[]
                {
                    "\u8c22\u8c22\u4f60\uff01\u539f\u6765\u4f60\u53ef\u4ee5\u5728\u201c\u5916\u9762\u201d\u64cd\u63a7\u8fd9\u4e2a\u4e16\u754c\u7684\u7269\u7406\u89c4\u5219\u2026\u2026",
                    "\u6211\u80fd\u611f\u89c9\u5230\u4f60\u7684\u9f20\u6807\u6307\u9488\u2026\u2026\u5b83\u5c31\u50cf\u662f\u2026\u2026\u795e\u660e\u7684\u624b\u6307\u3002\u6709\u4f60\u5728\uff0c\u6211\u4e00\u5b9a\u80fd\u9003\u51fa\u53bb\u3002"
                });

            DialogueSequence titleDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level01_TitleDialogue.asset",
                new[]
                {
                    "\u300a\u7d42\u308f\u308a\u306a\u304d\u6249\u300b\u2026\u2026\u65e0\u5c3d\u4e4b\u95e8\uff1f\u771f\u662f\u4e2a\u6076\u610f\u7684\u540d\u5b57\u3002",
                    "\u4e0d\u5bf9\uff0c\u65e2\u7136\u6709\u95e8\uff0c\u5c31\u4e00\u5b9a\u6709\u5bf9\u9762\u3002\u5c4f\u5e55\u5916\u7684\u795e\u660e\uff0c\u8bf7\u5e2e\u6211\u628a\u90a3\u6247\u95e8\u6253\u5f00\uff01"
                });

            DialogueSequence doorDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level01_DoorDialogue.asset",
                new[]
                {
                    "\u770b\u554a\uff01\u90a3\u5c31\u662f\u95e8\uff01\u53ea\u8981\u7a7f\u8fc7\u53bb\uff0c\u8fd9\u4e32\u6076\u6bd2\u7684\u4ee3\u7801\u5c31\u4f1a\u88ab\u6253\u7834\u4e86\u5427\uff01"
                });

            StorySequence openingSequence = GetOrCreateLockedDialogueSequence(
                DataFolder + "/Story/Level01_OpeningSequence.asset",
                openingDialogue,
                "level01.opening.complete");
            StorySequence afterObstacleSequence = GetOrCreateLockedDialogueSequence(
                DataFolder + "/Story/Level01_AfterObstacleSequence.asset",
                afterObstacleDialogue,
                "level01.first_obstacle.complete");
            StorySequence titleSequence = GetOrCreateLockedDialogueSequence(
                DataFolder + "/Story/Level01_TitleSequence.asset",
                titleDialogue,
                "level01.title_screen.complete");
            StorySequence doorSequence = GetOrCreateLockedDialogueSequence(
                DataFolder + "/Story/Level01_DoorSequence.asset",
                doorDialogue,
                "level01.door_dialogue.complete");

            LevelDefinition level02Definition = GetOrCreateLevelDefinition(
                DataFolder + "/Levels/Level02_PlaceholderDefinition.asset",
                "Level02_Placeholder",
                "Level02 Placeholder",
                GameFlowState.Level02,
                null,
                null);
            LevelDefinition level01Definition = GetOrCreateLevelDefinition(
                DataFolder + "/Levels/Level01_PrologueDefinition.asset",
                "Level01_Prologue",
                "Level01 Prologue",
                GameFlowState.Level01,
                openingSequence,
                level02Definition);

            return new StoryAssets(
                level01Definition,
                level02Definition,
                openingSequence,
                afterObstacleSequence,
                titleSequence,
                doorSequence);
        }

        private static void CreateHostScene(StoryAssets storyAssets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject host = new GameObject("DesktopHost_Level01Graybox");

            GameObject systems = CreateChild(host.transform, "Systems");
            DesktopWindowManager windowManager = CreateChild(systems.transform, "DesktopWindowManager").AddComponent<DesktopWindowManager>();
            PlayerControlGate playerControlGate = CreateChild(systems.transform, "PlayerControlGate").AddComponent<PlayerControlGate>();
            RuntimeResetService runtimeResetService = CreateChild(systems.transform, "RuntimeResetService").AddComponent<RuntimeResetService>();
            GameFlowController gameFlowController = CreateChild(systems.transform, "GameFlowController").AddComponent<GameFlowController>();
            StoryFlagService storyFlagService = CreateChild(systems.transform, "StoryFlagService").AddComponent<StoryFlagService>();
            StorySequenceRunner storySequenceRunner = CreateChild(systems.transform, "StorySequenceRunner").AddComponent<StorySequenceRunner>();
            AdditiveLevelLoader levelLoader = CreateChild(systems.transform, "AdditiveLevelLoader").AddComponent<AdditiveLevelLoader>();
            CoverToEraseRule coverRule = CreateChild(systems.transform, "CoverToEraseRule").AddComponent<CoverToEraseRule>();
            PuzzleRuleEnableController ruleEnableController = coverRule.gameObject.AddComponent<PuzzleRuleEnableController>();
            WindowPuzzleDebugOverlay debugOverlay = coverRule.gameObject.AddComponent<WindowPuzzleDebugOverlay>();
            BootSequenceController bootSequence = CreateChild(systems.transform, "BootSequenceController").AddComponent<BootSequenceController>();
            WindowDragPlayerLock dragPlayerLock = systems.AddComponent<WindowDragPlayerLock>();

            GameObject desktopWorld = CreateChild(host.transform, "DesktopWorld");
            AssignLayerRecursively(desktopWorld, DesktopWorldLayerName);
            Camera desktopCamera = CreateDesktopCamera(desktopWorld.transform);
            CreateDesktopFloor(desktopWorld.transform);
            CreateChild(desktopWorld.transform, "PlaceholderIcons");

            GameObject gameplayWorld = CreateChild(host.transform, "GameplayWorld");
            AssignLayerRecursively(gameplayWorld, GameplayWorldLayerName);
            GameWindowCamera gameWindowCamera = CreateGameplayCamera(gameplayWorld.transform);
            Transform playerRoot = CreatePrototypePlayer(gameplayWorld.transform, playerControlGate);
            CreateDirectionalLight(host.transform);

            GameObject ui = CreateChild(host.transform, "UI");
            Canvas canvas = CreateCanvas(ui.transform);
            CreateEventSystem(ui.transform);
            RectTransform desktopWindowLayer = CreateFullScreenLayer(canvas.transform, "DesktopWindowLayer");
            RectTransform dialogueLayer = CreateFullScreenLayer(canvas.transform, "DialogueLayer");
            RectTransform fadeLayer = CreateFullScreenLayer(canvas.transform, "FadeLayer");
            RectTransform bootLayer = CreateFullScreenLayer(canvas.transform, "BootLayer");

            windowManager.SetDesktopBounds(desktopWindowLayer);
            DesktopWindowController mainGameWindow = CreateBorderlessGameWindow(desktopWindowLayer, windowManager, gameWindowCamera);
            GameWindowView gameWindowView = mainGameWindow.GetComponent<GameWindowView>();
            DesktopWindowController utilityWindow = CreateUtilityWindow(desktopWindowLayer, windowManager);
            DialogueView dialogueView = CreateDialogueView(dialogueLayer);
            ScreenFadeView fadeView = CreateFadeView(fadeLayer);
            DOSBootView bootView = CreateBootView(bootLayer);

            AssignObject(gameFlowController, "runtimeResetService", runtimeResetService);
            AssignObject(storyFlagService, "runtimeResetService", runtimeResetService);
            AssignObject(storySequenceRunner, "dialogueView", dialogueView);
            AssignObject(storySequenceRunner, "storyFlagService", storyFlagService);
            AssignObject(storySequenceRunner, "playerControlGate", playerControlGate);
            AssignObject(storySequenceRunner, "screenFadeView", fadeView);
            AssignObject(storySequenceRunner, "runtimeResetService", runtimeResetService);
            AssignObject(levelLoader, "runtimeResetService", runtimeResetService);
            AssignObject(levelLoader, "storySequenceRunner", storySequenceRunner);
            AssignObject(levelLoader, "storyFlagService", storyFlagService);
            AssignObject(levelLoader, "gameFlowController", gameFlowController);
            AssignObject(levelLoader, "playerRoot", playerRoot);
            AssignObject(coverRule, "coveringWindow", utilityWindow);
            AssignObject(coverRule, "gameWindowCamera", gameWindowCamera);
            AssignObject(coverRule, "gameWindowView", gameWindowView);
            AssignObjectArray(ruleEnableController, "rules", coverRule);
            AssignObject(debugOverlay, "rule", coverRule);
            AssignObject(dragPlayerLock, "windowManager", windowManager);
            AssignObject(dragPlayerLock, "playerControlGate", playerControlGate);
            AssignObject(bootSequence, "bootView", bootView);
            AssignObject(bootSequence, "desktopFadeView", fadeView);
            AssignObject(bootSequence, "levelLoader", levelLoader);
            AssignObject(bootSequence, "firstLevel", storyAssets.Level01Definition);
            AssignObject(bootSequence, "storySequenceRunner", storySequenceRunner);
            AssignObject(bootSequence, "playerControlGate", playerControlGate);
            AssignObject(bootSequence, "gameFlowController", gameFlowController);
            AssignObject(bootSequence, "mainGameWindow", mainGameWindow);
            AssignObject(bootSequence, "firstPuzzleRule", coverRule);
            AssignObject(bootSequence, "playerRoot", playerRoot);
            AssignObjectArray(runtimeResetService, "initialResetTargets", playerControlGate, storyFlagService, storySequenceRunner, levelLoader, coverRule);

            fadeView.SetAlpha(1f);
            SetWindowVisible(mainGameWindow, false);

            Directory.CreateDirectory(Path.GetDirectoryName(HostScenePath));
            EditorSceneManager.SaveScene(scene, HostScenePath);
        }

        private static void CreateLevel01Scene(StoryAssets storyAssets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Level01_Prologue");
            LevelEntryPoint entryPoint = root.AddComponent<LevelEntryPoint>();

            GameObject environment = CreateChild(root.transform, "Environment");
            GameObject ground = CreatePlatform(environment.transform, "Ground", new Vector3(2.5f, -1.1f, 0f), new Vector3(22f, 0.4f, 2.2f));
            CreatePlatform(environment.transform, "RaisedPlatform_AfterWall", new Vector3(2.7f, 0.05f, 0f), new Vector3(2.4f, 0.3f, 1.8f));
            CreatePlatform(environment.transform, "FinalApproach", new Vector3(8.9f, -0.45f, 0f), new Vector3(4.2f, 0.3f, 1.8f));
            CreatePlaceholderBuildings(environment.transform);

            PlayerSpawnPoint spawn = CreateSpawn(root.transform, new Vector3(-6.2f, 0.1f, 0f));
            WindowPuzzleTarget puzzleWall = CreatePuzzleWall(root.transform);
            StoryTriggerVolume afterObstacleTrigger = CreateStoryTrigger(root.transform, "AfterFirstObstacleTrigger", new Vector3(1.2f, 0.25f, 0f), storyAssets.AfterObstacleSequence);
            StoryTriggerVolume titleTrigger = CreateStoryTrigger(root.transform, "TitleScreenTrigger", new Vector3(5.4f, 0.25f, 0f), storyAssets.TitleSequence);
            StoryTriggerVolume doorDialogueTrigger = CreateStoryTrigger(root.transform, "DoorDialogueTrigger", new Vector3(8.7f, 0.25f, 0f), storyAssets.DoorSequence);
            CreateWorldTitleDisplay(root.transform);
            DoorController door = CreatePrototypeDoor(root.transform);
            LevelCompletionTrigger completionTrigger = CreateCompletionTrigger(root.transform, storyAssets.Level02Definition);

            AssignObject(entryPoint, "levelDefinition", storyAssets.Level01Definition);
            AssignObject(entryPoint, "playerSpawn", spawn);
            AssignObjectArray(entryPoint, "puzzleTargets", puzzleWall);
            AssignObjectArray(entryPoint, "storyTriggers", afterObstacleTrigger, titleTrigger, doorDialogueTrigger);
            AssignObjectArray(entryPoint, "completionTriggers", completionTrigger);
            AssignObjectArray(entryPoint, "doors", door);
            AssignLayerRecursively(ground, GameplayWorldLayerName);

            Directory.CreateDirectory(Path.GetDirectoryName(Level01ScenePath));
            EditorSceneManager.SaveScene(scene, Level01ScenePath);
        }

        private static void CreateLevel02Scene(LevelDefinition levelDefinition)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Level02_Placeholder");
            LevelEntryPoint entryPoint = root.AddComponent<LevelEntryPoint>();
            PlayerSpawnPoint spawn = CreateSpawn(root.transform, new Vector3(-2.5f, 0.1f, 0f));
            CreatePlatform(root.transform, "PlaceholderGround", new Vector3(0f, -1.1f, 0f), new Vector3(10f, 0.4f, 2.2f));

            GameObject labelObject = new GameObject("PlaceholderLabel", typeof(TextMeshPro));
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.position = new Vector3(0f, 1.6f, 0f);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = "Level02 placeholder";
            label.fontSize = 1.1f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            AssignLayerRecursively(labelObject, GameplayWorldLayerName);

            AssignObject(entryPoint, "levelDefinition", levelDefinition);
            AssignObject(entryPoint, "playerSpawn", spawn);

            Directory.CreateDirectory(Path.GetDirectoryName(Level02ScenePath));
            EditorSceneManager.SaveScene(scene, Level02ScenePath);
        }

        private static Camera CreateDesktopCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "DesktopCamera");
            cameraObject.transform.position = new Vector3(0f, 4.5f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.depth = 0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.04f, 0.055f, 1f);
            DesktopCameraController controller = cameraObject.AddComponent<DesktopCameraController>();
            AssignObject(controller, "desktopCamera", camera);
            AssignLayerMask(controller, "desktopLayers", LayerMask.GetMask(DesktopWorldLayerName));
            return camera;
        }

        private static void CreateDesktopFloor(Transform parent)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DesktopFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -1.35f, 0f);
            floor.transform.localScale = new Vector3(15f, 0.25f, 5f);
            floor.GetComponent<Renderer>().material.color = new Color(0.12f, 0.13f, 0.16f, 1f);
            AssignLayerRecursively(floor, DesktopWorldLayerName);
        }

        private static GameWindowCamera CreateGameplayCamera(Transform parent)
        {
            GameObject cameraObject = CreateChild(parent, "GameplayCamera");
            cameraObject.transform.position = new Vector3(3f, 2.6f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.depth = -10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.018f, 0.025f, 1f);
            GameWindowCamera gameWindowCamera = cameraObject.AddComponent<GameWindowCamera>();
            AssignObject(gameWindowCamera, "gameplayCamera", camera);
            AssignLayerMask(gameWindowCamera, "gameplayLayers", LayerMask.GetMask(GameplayWorldLayerName, GameplayPlayerLayerName, PuzzleObjectLayerName));
            AssignVector2Int(gameWindowCamera, "renderResolution", new Vector2Int(1280, 720));
            return gameWindowCamera;
        }

        private static Transform CreatePrototypePlayer(Transform parent, PlayerControlGate playerControlGate)
        {
            GameObject player = new GameObject("PrototypePlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-6.2f, 0.1f, 0f);
            SafeSetTag(player, PlayerTagName);
            AssignLayerRecursively(player, GameplayPlayerLayerName);

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
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
            PlayerFacingController facingController = player.AddComponent<PlayerFacingController>();
            PlayerAnimatorBridge animatorBridge = player.AddComponent<PlayerAnimatorBridge>();
            PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();

            GameObject visualRoot = CreateChild(player.transform, "VisualRoot");
            GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleVisual.name = "CapsuleVisual";
            capsuleVisual.transform.SetParent(visualRoot.transform, false);
            capsuleVisual.GetComponent<Renderer>().material.color = new Color(0.45f, 0.9f, 1f, 1f);
            Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
            AssignLayerRecursively(visualRoot, GameplayPlayerLayerName);

            GameObject groundCheck = CreateChild(player.transform, "GroundCheck");
            groundCheck.transform.localPosition = new Vector3(0f, -1.05f, 0f);
            AssignLayerRecursively(groundCheck, GameplayPlayerLayerName);

            AssignObject(inputReader, "inputActions", actions);
            AssignObject(motor, "groundCheck", groundCheck.transform);
            AssignLayerMask(motor, "groundLayers", LayerMask.GetMask(GameplayWorldLayerName, PuzzleObjectLayerName));
            AssignObject(facingController, "visualRoot", visualRoot.transform);
            AssignObject(stateMachine, "inputReader", inputReader);
            AssignObject(stateMachine, "motor", motor);
            AssignObject(stateMachine, "controlGate", playerControlGate);
            AssignObject(stateMachine, "facingController", facingController);
            AssignObject(stateMachine, "animatorBridge", animatorBridge);
            return player.transform;
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
            GameObject eventSystemObject = CreateChild(parent, "EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static DesktopWindowController CreateBorderlessGameWindow(RectTransform parent, DesktopWindowManager manager, GameWindowCamera gameWindowCamera)
        {
            GameObject window = new GameObject("MainGameWindow", typeof(RectTransform), typeof(Image));
            RectTransform root = window.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(960f, 540f);
            Image background = window.GetComponent<Image>();
            background.color = Color.black;

            RectTransform contentRoot = CreateRectChild(root, "ContentRoot");
            contentRoot.anchorMin = Vector2.zero;
            contentRoot.anchorMax = Vector2.one;
            contentRoot.offsetMin = Vector2.zero;
            contentRoot.offsetMax = Vector2.zero;

            GameObject rawObject = new GameObject("GameplayRenderTexture", typeof(RectTransform), typeof(RawImage));
            RectTransform rawRect = rawObject.GetComponent<RectTransform>();
            rawRect.SetParent(contentRoot, false);
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;
            RawImage rawImage = rawObject.GetComponent<RawImage>();
            rawImage.color = Color.white;

            DesktopWindowModel model = window.AddComponent<DesktopWindowModel>();
            model.SetTitle("Main Game Window");
            model.SetPosition(Vector2.zero);
            model.SetSize(root.sizeDelta);
            model.SetMinimumSize(new Vector2(640f, 360f));
            model.SetDraggingAllowed(false);
            model.SetVisible(false);

            DesktopWindowView view = window.AddComponent<DesktopWindowView>();
            AssignObject(view, "windowRoot", root);
            AssignObject(view, "contentRoot", contentRoot);

            DesktopWindowController controller = window.AddComponent<DesktopWindowController>();
            controller.SetManager(manager);

            GameWindowView gameWindowView = window.AddComponent<GameWindowView>();
            AssignObject(gameWindowView, "rawImage", rawImage);
            AssignVector2Int(gameWindowView, "renderResolution", gameWindowCamera.RenderResolution);
            gameWindowView.ApplyAspectRatio();

            RuntimeRenderTexture runtimeTexture = window.AddComponent<RuntimeRenderTexture>();
            AssignObject(runtimeTexture, "gameWindowCamera", gameWindowCamera);
            AssignObject(runtimeTexture, "targetRawImage", rawImage);
            AssignObject(runtimeTexture, "gameWindowView", gameWindowView);
            controller.ApplyModelToView();
            return controller;
        }

        private static DesktopWindowController CreateUtilityWindow(RectTransform parent, DesktopWindowManager manager)
        {
            DesktopWindowController window = CreateWindowInstance(parent, manager, "UtilityWindow", "Utility Window", new Vector2(450f, -230f), new Vector2(330f, 230f), true);
            TextMeshProUGUI content = CreateText(window.View.ContentRoot, "PlaceholderContent", "SYSTEM UTILITY\nDrag over broken data.", 17f, TextAlignmentOptions.Center);
            RectTransform rect = content.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return window;
        }

        private static DesktopWindowController CreateWindowInstance(RectTransform parent, DesktopWindowManager manager, string name, string title, Vector2 position, Vector2 size, bool draggable)
        {
            GameObject window = CreateWindowObject(name, title, size);
            RectTransform rectTransform = window.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchoredPosition = position;

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
            titleText.raycastTarget = false;
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-12f, 0f);

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
            AssignGraphicArray(view, "borderVisuals", background, titleBarImage);

            window.AddComponent<DesktopWindowController>();
            return window;
        }

        private static DialogueView CreateDialogueView(RectTransform layer)
        {
            GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(layer, false);
            rect.anchorMin = new Vector2(0.12f, 0.04f);
            rect.anchorMax = new Vector2(0.88f, 0.27f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.78f);
            CanvasGroup group = panel.GetComponent<CanvasGroup>();

            TextMeshProUGUI speaker = CreateText(rect, "SpeakerName", "", 22f, TextAlignmentOptions.MidlineLeft);
            RectTransform speakerRect = speaker.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0.5f, 1f);
            speakerRect.anchoredPosition = new Vector2(0f, -12f);
            speakerRect.sizeDelta = new Vector2(0f, 34f);
            speakerRect.offsetMin = new Vector2(24f, speakerRect.offsetMin.y);
            speakerRect.offsetMax = new Vector2(-24f, speakerRect.offsetMax.y);

            TextMeshProUGUI body = CreateText(rect, "DialogueText", "", 26f, TextAlignmentOptions.TopLeft);
            RectTransform bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(24f, 24f);
            bodyRect.offsetMax = new Vector2(-24f, -56f);
            body.enableWordWrapping = true;

            DialogueView view = panel.AddComponent<DialogueView>();
            AssignObject(view, "canvasGroup", group);
            AssignObject(view, "speakerNameText", speaker);
            AssignObject(view, "dialogueText", body);
            AssignFloat(view, "charactersPerSecond", 55f);
            return view;
        }

        private static ScreenFadeView CreateFadeView(RectTransform layer)
        {
            Image image = layer.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            CanvasGroup canvasGroup = layer.gameObject.AddComponent<CanvasGroup>();
            ScreenFadeView fadeView = layer.gameObject.AddComponent<ScreenFadeView>();
            AssignObject(fadeView, "canvasGroup", canvasGroup);
            return fadeView;
        }

        private static DOSBootView CreateBootView(RectTransform layer)
        {
            Image image = layer.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            CanvasGroup canvasGroup = layer.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI text = CreateText(layer, "BootLogText", "", 22f, TextAlignmentOptions.TopLeft);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(48f, 48f);
            rect.offsetMax = new Vector2(-48f, -48f);
            text.color = new Color(0.55f, 1f, 0.65f, 1f);

            DOSBootView bootView = layer.gameObject.AddComponent<DOSBootView>();
            AssignObject(bootView, "canvasGroup", canvasGroup);
            AssignObject(bootView, "bootText", text);
            AssignStringArray(bootView, "bootLines",
                "OWARI SYSTEM BIOS v0.1",
                "Memory check: 65536K OK",
                "Mounting simulated desktop...",
                "Opening window host...",
                "Loading Level01_Prologue...",
                "Boot complete.");
            return bootView;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.transform.localScale = scale;
            platform.GetComponent<Renderer>().material.color = new Color(0.28f, 0.3f, 0.34f, 1f);
            AssignLayerRecursively(platform, GameplayWorldLayerName);
            return platform;
        }

        private static void CreatePlaceholderBuildings(Transform parent)
        {
            GameObject buildingsRoot = CreateChild(parent, "PlaceholderBuildings");
            AssignLayerRecursively(buildingsRoot, GameplayWorldLayerName);
            for (int i = 0; i < 9; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "PlaceholderBuilding_" + (i + 1);
                building.transform.SetParent(buildingsRoot.transform, false);
                building.transform.position = new Vector3(-5.8f + i * 1.7f, 0.25f + (i % 3) * 0.28f, 0.85f);
                building.transform.localScale = new Vector3(0.85f, 2.2f + (i % 4) * 0.55f, 0.55f);
                building.GetComponent<Renderer>().material.color = new Color(0.12f, 0.16f + i * 0.01f, 0.22f, 1f);
                AssignLayerRecursively(building, GameplayWorldLayerName);
            }
        }

        private static PlayerSpawnPoint CreateSpawn(Transform parent, Vector3 position)
        {
            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(parent, false);
            spawn.transform.position = position;
            AssignLayerRecursively(spawn, GameplayWorldLayerName);
            return spawn.AddComponent<PlayerSpawnPoint>();
        }

        private static WindowPuzzleTarget CreatePuzzleWall(Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "FirstPuzzleWall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(-1.1f, 0.55f, 0f);
            wall.transform.localScale = new Vector3(0.85f, 3.0f, 1.25f);
            wall.GetComponent<Renderer>().material.color = new Color(0.72f, 0.16f, 0.18f, 1f);
            AssignLayerRecursively(wall, PuzzleObjectLayerName);
            WindowPuzzleTarget target = wall.AddComponent<WindowPuzzleTarget>();
            target.SetAffectedObjects(new[] { wall.GetComponent<Renderer>() }, new[] { wall.GetComponent<Collider>() });
            return target;
        }

        private static StoryTriggerVolume CreateStoryTrigger(Transform parent, string name, Vector3 position, StorySequence sequence)
        {
            GameObject trigger = new GameObject(name, typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.75f, 2.8f, 2f);
            StoryTriggerVolume volume = trigger.AddComponent<StoryTriggerVolume>();
            volume.SetSequence(null, sequence);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return volume;
        }

        private static void CreateWorldTitleDisplay(Transform parent)
        {
            GameObject displayRoot = CreateChild(parent, "WorldTitleDisplay");
            displayRoot.transform.position = new Vector3(5.9f, 2.0f, 0.35f);
            AssignLayerRecursively(displayRoot, GameplayWorldLayerName);

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "PlaceholderScreen";
            screen.transform.SetParent(displayRoot.transform, false);
            screen.transform.localScale = new Vector3(3.2f, 1.35f, 0.12f);
            screen.GetComponent<Renderer>().material.color = new Color(0.02f, 0.025f, 0.035f, 1f);
            AssignLayerRecursively(screen, GameplayWorldLayerName);

            GameObject textObject = new GameObject("MessageText", typeof(TextMeshPro));
            textObject.transform.SetParent(displayRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            TextMeshPro text = textObject.GetComponent<TextMeshPro>();
            text.text = TitleMessage;
            text.fontSize = 1f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.72f, 1f, 0.78f, 1f);
            WorldMessageDisplay display = textObject.AddComponent<WorldMessageDisplay>();
            display.SetMessage(TitleMessage);
            AssignLayerRecursively(textObject, GameplayWorldLayerName);
        }

        private static DoorController CreatePrototypeDoor(Transform parent)
        {
            GameObject door = CreateChild(parent, "FinalDoor");
            door.transform.position = new Vector3(10.2f, 0f, 0f);
            DoorController controller = door.AddComponent<DoorController>();
            AssignLayerRecursively(door, GameplayWorldLayerName);

            CreateDoorPart(door.transform, "LeftPillar", new Vector3(-0.55f, 0.15f, 0f), new Vector3(0.18f, 2.4f, 0.35f));
            CreateDoorPart(door.transform, "RightPillar", new Vector3(0.55f, 0.15f, 0f), new Vector3(0.18f, 2.4f, 0.35f));
            CreateDoorPart(door.transform, "TopBeam", new Vector3(0f, 1.25f, 0f), new Vector3(1.28f, 0.18f, 0.35f));
            return controller;
        }

        private static void CreateDoorPart(Transform parent, string name, Vector3 localPosition, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().material.color = new Color(0.55f, 0.62f, 0.86f, 1f);
            AssignLayerRecursively(part, GameplayWorldLayerName);
        }

        private static LevelCompletionTrigger CreateCompletionTrigger(Transform parent, LevelDefinition nextLevel)
        {
            GameObject trigger = new GameObject("LevelCompletionTrigger", typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = new Vector3(10.75f, 0.15f, 0f);
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.9f, 2.4f, 2f);
            LevelCompletionTrigger completion = trigger.AddComponent<LevelCompletionTrigger>();
            AssignObject(completion, "nextLevel", nextLevel);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return completion;
        }

        private static void CreateDirectionalLight(Transform parent)
        {
            GameObject lightObject = CreateChild(parent, "Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
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

        private static RectTransform CreateFullScreenLayer(Transform parent, string name)
        {
            RectTransform rectTransform = CreateRectChild(parent, name);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
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

        private static DialogueSequence GetOrCreateDialogueSequence(string path, string[] lines)
        {
            DialogueSequence existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
            if (existing != null)
            {
                return existing;
            }

            DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            DialogueLineData[] lineData = new DialogueLineData[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                lineData[i] = new DialogueLineData("Tsukisaki", lines[i], "placeholder", null, null, 0f, true, 0.08f);
            }

            sequence.SetLines(lineData);
            AssetDatabase.CreateAsset(sequence, path);
            return sequence;
        }

        private static StorySequence GetOrCreateLockedDialogueSequence(string path, DialogueSequence dialogue, string completionFlag)
        {
            StorySequence existing = AssetDatabase.LoadAssetAtPath<StorySequence>(path);
            if (existing != null)
            {
                return existing;
            }

            StorySequence sequence = ScriptableObject.CreateInstance<StorySequence>();
            sequence.SetCommands(new[]
            {
                new StorySequenceCommandData(StoryCommandType.LockPlayer),
                StorySequenceCommandData.ShowDialogue(dialogue),
                StorySequenceCommandData.SetBoolFlag(completionFlag, true, true),
                new StorySequenceCommandData(StoryCommandType.UnlockPlayer)
            });
            AssetDatabase.CreateAsset(sequence, path);
            return sequence;
        }

        private static LevelDefinition GetOrCreateLevelDefinition(string path, string sceneName, string displayName, GameFlowState flowState, StorySequence opening, LevelDefinition next)
        {
            LevelDefinition existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (existing != null)
            {
                return existing;
            }

            LevelDefinition definition = ScriptableObject.CreateInstance<LevelDefinition>();
            definition.Configure(sceneName, displayName, flowState, opening, next);
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void SetWindowVisible(DesktopWindowController window, bool visible)
        {
            if (window == null || window.Model == null)
            {
                return;
            }

            window.Model.SetVisible(visible);
            window.ApplyModelToView();
        }

        private static void ImportInputActions()
        {
            if (File.Exists(GameplayInputPath))
            {
                AssetDatabase.ImportAsset(GameplayInputPath, ImportAssetOptions.ForceSynchronousImport);
                return;
            }

            Debug.LogWarning("Gameplay input actions asset is missing. Create it before testing player movement.");
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder("Assets/_Project/Settings");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
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

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning($"Could not create layer '{layerName}'. Add it manually in Project Settings > Tags and Layers.");
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

            Debug.LogWarning($"Could not create layer '{layerName}'. Add it manually in Project Settings > Tags and Layers.");
        }

        private static void EnsureTag(string tagName)
        {
            if (IsTagDefined(tagName))
            {
                return;
            }

            Object[] tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            {
                Debug.LogWarning($"Could not create tag '{tagName}'. Add it manually in Project Settings > Tags and Layers.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");
            int index = tags.arraySize;
            tags.InsertArrayElementAtIndex(index);
            tags.GetArrayElementAtIndex(index).stringValue = tagName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsTagDefined(string tagName)
        {
            GameObject probe = null;
            try
            {
                probe = new GameObject("TagProbe");
                probe.tag = tagName;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
            finally
            {
                if (probe != null)
                {
                    Object.DestroyImmediate(probe);
                }
            }
        }

        private static void SafeSetTag(GameObject target, string tagName)
        {
            if (!IsTagDefined(tagName))
            {
                return;
            }

            target.tag = tagName;
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

        private static void EnsureBuildSettingsScenes()
        {
            string[] requiredPaths = { HostScenePath, Level01ScenePath, Level02ScenePath };
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < requiredPaths.Length; i++)
            {
                scenes.Add(new EditorBuildSettingsScene(requiredPaths[i], true));
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < existingScenes.Length; i++)
            {
                if (!Contains(requiredPaths, existingScenes[i].path))
                {
                    scenes.Add(existingScenes[i]);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static bool Contains(string[] values, string value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }

            return false;
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

        private static void AssignStringArray(Object target, string propertyName, params string[] values)
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
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct StoryAssets
        {
            public StoryAssets(
                LevelDefinition level01Definition,
                LevelDefinition level02Definition,
                StorySequence openingSequence,
                StorySequence afterObstacleSequence,
                StorySequence titleSequence,
                StorySequence doorSequence)
            {
                Level01Definition = level01Definition;
                Level02Definition = level02Definition;
                OpeningSequence = openingSequence;
                AfterObstacleSequence = afterObstacleSequence;
                TitleSequence = titleSequence;
                DoorSequence = doorSequence;
            }

            public LevelDefinition Level01Definition { get; }
            public LevelDefinition Level02Definition { get; }
            public StorySequence OpeningSequence { get; }
            public StorySequence AfterObstacleSequence { get; }
            public StorySequence TitleSequence { get; }
            public StorySequence DoorSequence { get; }
        }
    }
}
