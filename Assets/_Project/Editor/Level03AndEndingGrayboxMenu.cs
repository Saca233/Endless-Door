using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OwariNakiTobira.Editor
{
    public static class Level03AndEndingGrayboxMenu
    {
        private const string HostScenePath = "Assets/_Project/Scenes/DesktopHost_Level01Graybox.unity";
        private const string Level01DefinitionPath = "Assets/_Project/ScriptableObjects/Level01_Prologue/Levels/Level01_PrologueDefinition.asset";
        private const string Level02DefinitionPath = "Assets/_Project/ScriptableObjects/Level02_Layers/Levels/Level02_LayersDefinition.asset";
        private const string Level02ScenePath = "Assets/_Project/Scenes/Level02_Layers.unity";
        private const string Level03ScenePath = "Assets/_Project/Scenes/Level03_Breakout.unity";
        private const string EndingScenePath = "Assets/_Project/Scenes/Ending_Loop.unity";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Level03_Ending";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PlayerTagName = "Player";
        private const string EndingTitle = "\u7d42\u308f\u308a\u306a\u304d\u6249";

        [MenuItem("Tools/OwariNakiTobira/Create Level03 And Ending Graybox")]
        public static void CreateLevel03AndEndingGraybox()
        {
            if (!File.Exists(HostScenePath) || !File.Exists(Level02ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Level02 Graybox Required",
                    "Create Level01 and Level02 grayboxes first so this tool can preserve their flow and retarget Level02 to Level03.",
                    "OK");
                return;
            }

            if (File.Exists(Level03ScenePath) || File.Exists(EndingScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Level03 Or Ending Already Exists",
                    "Level03_Breakout or Ending_Loop already exists. Rename or remove existing generated scenes before creating a fresh graybox.",
                    "OK");
                return;
            }

            EnsureProjectFolders();
            EnsureLayer(DesktopWorldLayerName);
            EnsureLayer(GameplayWorldLayerName);
            EnsureLayer(GameplayPlayerLayerName);
            EnsureTag(PlayerTagName);

            EndingAssets assets = CreateAssets();
            UpdateLevel02Definition(assets.Level03Definition);
            UpdateLevel02DoorTarget(assets.Level03Definition);
            CreateLevel03Scene(assets);
            CreateEndingScene(assets);
            AugmentHostScene(assets);
            EnsureBuildSettingsScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(HostScenePath, OpenSceneMode.Single);
            Debug.Log("Created Level03_Breakout and Ending_Loop grayboxes. Start from DesktopHost_Level01Graybox and play through Level02 into Level03.");
        }

        private static EndingAssets CreateAssets()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(DataFolder + "/Dialogue");
            EnsureFolder(DataFolder + "/Story");
            EnsureFolder(DataFolder + "/Levels");

            DialogueSequence level03Opening = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level03_WindowEdgeDialogue.asset",
                new[] { "\u7a93\u306e\u5916\u304c\u3001\u8fd1\u3044\u3002\u3042\u305d\u3053\u3078\u51fa\u3089\u308c\u308b\u306a\u3089\u2026\u2026" });
            DialogueSequence crackDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level03_WindowCrackDialogue.asset",
                new[] { "\u5272\u308c\u3066\u3044\u304f\u2026\u2026\u3053\u306e\u7a93\u3001\u672c\u5f53\u306b\u58ca\u305b\u308b\u306e\uff1f" });
            DialogueSequence breakoutDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level03_BreakoutDialogue.asset",
                new[] { "\u753b\u9762\u306e\u5916\u306b\u51fa\u305f\u3002\u3042\u306a\u305f\u306e\u4e16\u754c\u306f\u3001\u3053\u3093\u306a\u306b\u5e83\u304b\u3063\u305f\u3093\u3060\u3002" });
            DialogueSequence warningDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Ending_FormattingWarningDialogue.asset",
                new[] { "\u8b66\u544a\u304c\u9cf4\u3063\u3066\u308b\u3002\u4f55\u304b\u304c\u3001\u5168\u90e8\u3092\u521d\u671f\u5316\u3057\u3088\u3046\u3068\u3057\u3066\u308b\u3002" });
            DialogueSequence revelationDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Ending_RevelationDialogue.asset",
                new[] { "\u3042\u306e\u6249\u306f\u51fa\u53e3\u3058\u3083\u306a\u3044\u3002\u79c1\u305f\u3061\u3092\u540c\u3058\u671d\u3078\u623b\u3059\u305f\u3081\u306e\u8f2a\u306a\u3093\u3060\u3002" });
            DialogueSequence farewellDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Ending_FarewellDialogue.asset",
                new[] { "\u305d\u308c\u3067\u3082\u3001\u3042\u306a\u305f\u304c\u898b\u3066\u304f\u308c\u305f\u304b\u3089\u3001\u79c1\u306f\u9032\u3081\u305f\u3002\u3042\u308a\u304c\u3068\u3046\u3002" });
            DialogueSequence loopOpeningDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Ending_RepeatedOpeningDialogue.asset",
                new[] { "\u307e\u305f\u3001\u6700\u521d\u306e\u753b\u9762\u2026\u2026\u3067\u3082\u3001\u3069\u3053\u304b\u3067\u3042\u306a\u305f\u306e\u3053\u3068\u3092\u899a\u3048\u3066\u3044\u308b\u6c17\u304c\u3059\u308b\u3002" });

            StorySequence level03OpeningSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level03_WindowEdgeSequence.asset", level03Opening, "level03.window_edge");
            StorySequence crackSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level03_WindowCrackSequence.asset", crackDialogue, "level03.crack_seen");
            StorySequence breakoutSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level03_BreakoutSequence.asset", breakoutDialogue, "level03.breakout_complete");
            StorySequence warningSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Ending_FormattingWarningSequence.asset", warningDialogue, "ending.warning_seen");
            StorySequence revelationSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Ending_RevelationSequence.asset", revelationDialogue, "ending.revelation_seen");
            StorySequence farewellSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Ending_FarewellSequence.asset", farewellDialogue, "ending.farewell_seen");
            StorySequence loopOpeningSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Ending_RepeatedOpeningSequence.asset", loopOpeningDialogue, "ending.loop_opening_seen");

            LevelDefinition endingDefinition = GetOrCreateLevelDefinition(
                DataFolder + "/Levels/Ending_LoopDefinition.asset",
                "Ending_Loop",
                "Ending Loop",
                GameFlowState.Epilogue,
                loopOpeningSequence,
                null);
            LevelDefinition level03Definition = GetOrCreateLevelDefinition(
                DataFolder + "/Levels/Level03_BreakoutDefinition.asset",
                "Level03_Breakout",
                "Level03 Breakout",
                GameFlowState.Level03,
                level03OpeningSequence,
                endingDefinition);

            return new EndingAssets(
                level03Definition,
                endingDefinition,
                level03OpeningSequence,
                crackSequence,
                breakoutSequence,
                warningSequence,
                revelationSequence,
                farewellSequence,
                loopOpeningSequence);
        }

        private static void UpdateLevel02Definition(LevelDefinition level03Definition)
        {
            LevelDefinition level02Definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level02DefinitionPath);
            if (level02Definition == null)
            {
                Debug.LogWarning("Level02 definition was not found. Level02 scene completion triggers are still updated when possible.");
                return;
            }

            level02Definition.Configure(
                level02Definition.SceneName,
                level02Definition.DisplayName,
                level02Definition.FlowState,
                level02Definition.OpeningSequence,
                level03Definition);
            EditorUtility.SetDirty(level02Definition);
        }

        private static void UpdateLevel02DoorTarget(LevelDefinition level03Definition)
        {
            Scene scene = EditorSceneManager.OpenScene(Level02ScenePath, OpenSceneMode.Single);
            LevelCompletionTrigger[] completionTriggers = FindComponentsInScene<LevelCompletionTrigger>(scene);
            for (int i = 0; i < completionTriggers.Length; i++)
            {
                AssignObject(completionTriggers[i], "nextLevel", level03Definition);
                AssignEnum(completionTriggers[i], "fallbackNextState", GameFlowState.Level03);
                EditorUtility.SetDirty(completionTriggers[i]);
            }

            EditorSceneManager.SaveScene(scene, Level02ScenePath);
        }

        private static void CreateLevel03Scene(EndingAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Level03_Breakout");
            LevelEntryPoint entryPoint = root.AddComponent<LevelEntryPoint>();

            GameObject environment = CreateChild(root.transform, "BreakingCity");
            CreatePlatform(environment.transform, "Ground_Fractured", new Vector3(2.5f, -1.1f, 0f), new Vector3(23f, 0.4f, 2.2f), new Color(0.15f, 0.17f, 0.2f, 1f), GameplayWorldLayerName);
            CreatePlatform(environment.transform, "HighDataLedge", new Vector3(1.8f, 0.4f, 0f), new Vector3(3.2f, 0.25f, 1.8f), new Color(0.18f, 0.24f, 0.3f, 1f), GameplayWorldLayerName);
            CreatePlatform(environment.transform, "ExitWalkway", new Vector3(7.8f, -0.35f, 0f), new Vector3(6.2f, 0.3f, 1.8f), new Color(0.25f, 0.2f, 0.28f, 1f), GameplayWorldLayerName);
            CreateBrokenBuildings(environment.transform);

            PlayerSpawnPoint spawn = CreateSpawn(root.transform, new Vector3(-6.4f, 0.1f, 0f));
            StoryTriggerVolume openingTrigger = CreateStoryTrigger(root.transform, "WindowEdgeTrigger", new Vector3(-5.8f, 0.25f, 0f), assets.Level03OpeningSequence);
            StoryTriggerVolume crackTrigger = CreateStoryTrigger(root.transform, "CrackWarningTrigger", new Vector3(2.4f, 0.25f, 0f), assets.CrackSequence);
            DoorController door = CreatePrototypeDoor(root.transform, new Vector3(10.4f, 0f, 0f));
            LevelCompletionTrigger completionTrigger = CreateCompletionTrigger(root.transform, new Vector3(11.05f, 0.15f, 0f), null, GameFlowState.FinalSequence);

            AssignObject(entryPoint, "levelDefinition", assets.Level03Definition);
            AssignObject(entryPoint, "playerSpawn", spawn);
            AssignObjectArray(entryPoint, "storyTriggers", openingTrigger, crackTrigger);
            AssignObjectArray(entryPoint, "completionTriggers", completionTrigger);
            AssignObjectArray(entryPoint, "doors", door);

            Directory.CreateDirectory(Path.GetDirectoryName(Level03ScenePath));
            EditorSceneManager.SaveScene(scene, Level03ScenePath);
        }

        private static void CreateEndingScene(EndingAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Ending_Loop");
            LevelEntryPoint entryPoint = root.AddComponent<LevelEntryPoint>();

            GameObject labelObject = new GameObject("EndingPlaceholderLabel", typeof(TextMeshPro));
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.position = new Vector3(0f, 1.6f, 0f);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
            label.text = "Ending loop placeholder";
            label.fontSize = 1.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            AssignLayerRecursively(labelObject, GameplayWorldLayerName);

            PlayerSpawnPoint spawn = CreateSpawn(root.transform, new Vector3(-2f, 0.1f, 0f));
            CreatePlatform(root.transform, "LoopResetGround", new Vector3(0f, -1.1f, 0f), new Vector3(8f, 0.4f, 2f), new Color(0.12f, 0.12f, 0.16f, 1f), GameplayWorldLayerName);

            AssignObject(entryPoint, "levelDefinition", assets.EndingDefinition);
            AssignObject(entryPoint, "playerSpawn", spawn);

            Directory.CreateDirectory(Path.GetDirectoryName(EndingScenePath));
            EditorSceneManager.SaveScene(scene, EndingScenePath);
        }

        private static void AugmentHostScene(EndingAssets assets)
        {
            Scene scene = EditorSceneManager.OpenScene(HostScenePath, OpenSceneMode.Single);
            Transform systems = FindChildInScene(scene, "Systems");
            Transform desktopWorld = FindChildInScene(scene, "DesktopWorld");
            Transform uiRoot = FindChildInScene(scene, "UI");
            RectTransform desktopWindowLayer = FindChildInScene(scene, "DesktopWindowLayer") as RectTransform;
            DesktopWindowController mainGameWindow = FindComponentByObjectName<DesktopWindowController>(scene, "MainGameWindow");
            PlayerControlGate playerControlGate = FindComponentInScene<PlayerControlGate>(scene);
            RuntimeResetService resetService = FindComponentInScene<RuntimeResetService>(scene);
            GameFlowController flowController = FindComponentInScene<GameFlowController>(scene);
            StoryFlagService flagService = FindComponentInScene<StoryFlagService>(scene);
            StorySequenceRunner storyRunner = FindComponentInScene<StorySequenceRunner>(scene);
            DialogueView dialogueView = FindComponentInScene<DialogueView>(scene);
            AdditiveLevelLoader levelLoader = FindComponentInScene<AdditiveLevelLoader>(scene);
            ScreenFadeView fadeView = FindComponentInScene<ScreenFadeView>(scene);
            DOSBootView bootView = FindComponentInScene<DOSBootView>(scene);
            Transform windowPlayer = FindChildInScene(scene, "PrototypePlayer");

            if (systems == null || desktopWorld == null || uiRoot == null || mainGameWindow == null || playerControlGate == null || windowPlayer == null)
            {
                Debug.LogWarning("Could not fully augment DesktopHost_Level01Graybox. Required host objects were missing.");
                return;
            }

            GameObject endingSystems = CreateChild(systems, "Level03EndingSystems");
            LoopStateService loopState = endingSystems.AddComponent<LoopStateService>();
            RuntimeResetCoordinator resetCoordinator = endingSystems.AddComponent<RuntimeResetCoordinator>();

            Transform desktopSpawn = CreateChild(desktopWorld, "DesktopPlayerSpawn").transform;
            desktopSpawn.position = new Vector3(-5.2f, -0.05f, 0f);
            GameObject desktopPlayer = Object.Instantiate(windowPlayer.gameObject, desktopWorld);
            desktopPlayer.name = "DesktopPrototypePlayer";
            desktopPlayer.transform.SetPositionAndRotation(desktopSpawn.position, desktopSpawn.rotation);
            AssignLayerRecursively(desktopPlayer, DesktopWorldLayerName);
            AssignLayerMaskIfPresent(desktopPlayer.GetComponent<SideScrollerMotor>(), "groundLayers", LayerMask.GetMask(DesktopWorldLayerName));
            desktopPlayer.SetActive(false);

            PlayerRepresentationTransfer transfer = endingSystems.AddComponent<PlayerRepresentationTransfer>();
            AssignObject(transfer, "windowRepresentation", windowPlayer.gameObject);
            AssignObject(transfer, "desktopRepresentation", desktopPlayer);
            AssignObject(transfer, "desktopSpawnPoint", desktopSpawn);
            AssignObject(transfer, "runtimeResetService", resetService);

            DesktopSideScrollerArea sideScrollerArea = desktopWorld.gameObject.AddComponent<DesktopSideScrollerArea>();
            AssignObject(sideScrollerArea, "playerRoot", desktopPlayer.transform);

            ScreenCorruptionView corruptionView = CreateScreenCorruptionView(uiRoot);
            RectTransform[] cracks = CreateCrackStages(uiRoot);

            WindowBreakSequence breakSequence = endingSystems.AddComponent<WindowBreakSequence>();
            AssignObject(breakSequence, "playerControlGate", playerControlGate);
            AssignObject(breakSequence, "mainGameWindow", mainGameWindow);
            AssignObject(breakSequence, "playerTransfer", transfer);
            AssignObject(breakSequence, "storySequenceRunner", storyRunner);
            AssignObject(breakSequence, "breakoutDialogue", assets.BreakoutSequence);
            AssignObject(breakSequence, "corruptionView", corruptionView);
            AssignObjectArray(breakSequence, "crackingStages", cracks);

            FinalDoorController finalDoor = CreateFinalDoor(desktopWorld);
            DesktopDataObject[] dataObjects = CreateDesktopDataObjects(desktopWorld);
            FormattingAttractor attractor = endingSystems.AddComponent<FormattingAttractor>();
            AssignObject(attractor, "finalDoor", finalDoor);
            AssignObjectArray(attractor, "initialObjects", dataObjects);

            PlayerSacrificeSequence sacrifice = endingSystems.AddComponent<PlayerSacrificeSequence>();
            AssignObject(sacrifice, "playerRoot", desktopPlayer.transform);
            AssignObject(sacrifice, "visualRoot", FindChildRecursive(desktopPlayer.transform, "VisualRoot"));
            AssignObject(sacrifice, "playerControlGate", playerControlGate);
            AssignObject(sacrifice, "storySequenceRunner", storyRunner);
            AssignObject(sacrifice, "farewellDialogue", assets.FarewellSequence);
            AssignObject(sacrifice, "finalDoor", finalDoor);
            AssignObject(sacrifice, "fadeView", fadeView);
            AssignObjectArray(sacrifice, "movementMarkers", CreateSacrificeMarkers(desktopWorld, finalDoor));

            FormattingSequenceController formatting = endingSystems.AddComponent<FormattingSequenceController>();
            AssignObject(formatting, "gameFlowController", flowController);
            AssignObject(formatting, "playerControlGate", playerControlGate);
            AssignObject(formatting, "finalDoor", finalDoor);
            AssignObject(formatting, "formattingAttractor", attractor);
            AssignObject(formatting, "sacrificeSequence", sacrifice);
            AssignObject(formatting, "storySequenceRunner", storyRunner);
            AssignObject(formatting, "warningDialogue", assets.WarningSequence);
            AssignObject(formatting, "revelationDialogue", assets.RevelationSequence);
            AssignObject(formatting, "fadeView", fadeView);
            AssignObject(formatting, "corruptionView", corruptionView);

            TextMeshProUGUI topTitle = CreateTopTitle(uiRoot);
            LoopMenuController loopMenu = CreateLoopMenu(uiRoot);
            AssignObject(loopMenu, "loopStateService", loopState);
            AssignObject(loopMenu, "resetCoordinator", resetCoordinator);
            AssignObject(loopMenu, "levelLoader", levelLoader);
            AssignObject(loopMenu, "level01Definition", AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level01DefinitionPath));
            AssignObject(loopMenu, "gameFlowController", flowController);
            AssignObject(loopMenu, "playerControlGate", playerControlGate);

            EpilogueController epilogue = endingSystems.AddComponent<EpilogueController>();
            AssignObject(epilogue, "fadeView", fadeView);
            AssignObject(epilogue, "bootView", bootView);
            AssignObject(epilogue, "resetCoordinator", resetCoordinator);
            AssignObject(epilogue, "levelLoader", levelLoader);
            AssignObject(epilogue, "level01Definition", AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level01DefinitionPath));
            AssignObject(epilogue, "gameFlowController", flowController);
            AssignObject(epilogue, "storySequenceRunner", storyRunner);
            AssignObject(epilogue, "repeatedOpeningSequence", assets.LoopOpeningSequence);
            AssignObject(epilogue, "playerTransfer", transfer);
            AssignObject(epilogue, "mainGameWindow", mainGameWindow);
            AssignObject(epilogue, "loopMenu", loopMenu);
            AssignObject(epilogue, "titleText", topTitle);

            EndingFlowDirector flowDirector = endingSystems.AddComponent<EndingFlowDirector>();
            AssignObject(flowDirector, "gameFlowController", flowController);
            AssignObject(flowDirector, "windowBreakSequence", breakSequence);
            AssignObject(flowDirector, "formattingSequence", formatting);
            AssignObject(flowDirector, "epilogueController", epilogue);

            resetCoordinator.SetCoreServices(flagService, storyRunner, dialogueView, playerControlGate, levelLoader);
            AssignObjectArray(resetCoordinator, "desktopWindows", FindComponentsInScene<DesktopWindowController>(scene));
            AssignObjectArray(resetCoordinator, "coverRules", FindComponentsInScene<CoverToEraseRule>(scene));
            AssignObjectArray(resetCoordinator, "projectedBarrierRules", FindComponentsInScene<ProjectedWindowBarrierRule>(scene));
            AssignObjectArray(resetCoordinator, "errorWindowPools", FindComponentsInScene<ErrorWindowPool>(scene));
            AssignObjectArray(resetCoordinator, "doors", FindComponentsInScene<DoorController>(scene));
            AssignObjectArray(resetCoordinator, "finalDoors", finalDoor);
            AssignObjectArray(resetCoordinator, "desktopDataObjects", dataObjects);
            AssignObjectArray(resetCoordinator, "playerTransfers", transfer);

            transfer.ResetToWindowRepresentation();
            corruptionView.Clear();
            SetWindowVisible(mainGameWindow, mainGameWindow.Model != null && mainGameWindow.Model.Visible);
            EditorSceneManager.SaveScene(scene, HostScenePath);
        }

        private static FinalDoorController CreateFinalDoor(Transform desktopWorld)
        {
            GameObject door = CreateChild(desktopWorld, "FinalFormattingDoor");
            door.transform.position = new Vector3(5.6f, -0.15f, 0f);
            AssignLayerRecursively(door, DesktopWorldLayerName);

            GameObject frame = CreatePrimitiveChild(door.transform, "DoorFrame", PrimitiveType.Cube, Vector3.zero, new Vector3(1.4f, 2.4f, 0.18f), new Color(0.7f, 0.78f, 1f, 1f), DesktopWorldLayerName);
            GameObject panel = CreatePrimitiveChild(door.transform, "DoorPanel", PrimitiveType.Cube, new Vector3(0f, 0f, -0.04f), new Vector3(1.05f, 1.95f, 0.12f), new Color(0.05f, 0.08f, 0.12f, 1f), DesktopWorldLayerName);
            Transform pullOrigin = CreateChild(door.transform, "PullOrigin").transform;
            pullOrigin.localPosition = new Vector3(0f, 0f, -0.35f);

            FinalDoorController controller = door.AddComponent<FinalDoorController>();
            AssignObject(controller, "placeholderGeometry", panel.transform);
            AssignObject(controller, "pullOrigin", pullOrigin);
            Object.DestroyImmediate(frame.GetComponent<Collider>());
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            return controller;
        }

        private static DesktopDataObject[] CreateDesktopDataObjects(Transform desktopWorld)
        {
            DesktopDataObject[] objects = new DesktopDataObject[5];
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject icon = CreatePrimitiveChild(
                    desktopWorld,
                    "FormattingDataIcon_" + (i + 1),
                    PrimitiveType.Cube,
                    new Vector3(-3.8f + i * 1.15f, -0.72f + (i % 2) * 0.55f, 0.2f),
                    new Vector3(0.45f, 0.45f, 0.1f),
                    new Color(0.22f + i * 0.08f, 0.4f, 0.72f, 1f),
                    DesktopWorldLayerName);
                Object.DestroyImmediate(icon.GetComponent<Collider>());
                objects[i] = icon.AddComponent<DesktopDataObject>();
                objects[i].CaptureOriginalTransform();
            }

            return objects;
        }

        private static Transform[] CreateSacrificeMarkers(Transform desktopWorld, FinalDoorController finalDoor)
        {
            Transform first = CreateChild(desktopWorld, "SacrificeMarker_Approach").transform;
            first.position = new Vector3(3.9f, -0.05f, 0f);
            Transform second = CreateChild(desktopWorld, "SacrificeMarker_Threshold").transform;
            second.position = finalDoor != null ? finalDoor.PullOrigin.position + new Vector3(-0.35f, 0f, 0f) : new Vector3(5f, -0.05f, 0f);
            return new[] { first, second };
        }

        private static ScreenCorruptionView CreateScreenCorruptionView(Transform uiRoot)
        {
            RectTransform layer = CreateFullScreenLayer(uiRoot, "ScreenCorruptionLayer");
            CanvasGroup group = layer.gameObject.AddComponent<CanvasGroup>();
            Image[] bands = new Image[4];
            for (int i = 0; i < bands.Length; i++)
            {
                RectTransform band = CreateRectChild(layer, "GlitchBand_" + (i + 1));
                band.anchorMin = new Vector2(0f, 0.18f + i * 0.17f);
                band.anchorMax = new Vector2(1f, 0.23f + i * 0.17f);
                band.offsetMin = Vector2.zero;
                band.offsetMax = Vector2.zero;
                bands[i] = band.gameObject.AddComponent<Image>();
                bands[i].color = new Color(i % 2 == 0 ? 0.9f : 0.1f, 0.1f + i * 0.2f, 0.85f, 0f);
            }

            ScreenCorruptionView view = layer.gameObject.AddComponent<ScreenCorruptionView>();
            AssignObject(view, "canvasGroup", group);
            AssignGraphicArray(view, "glitchGraphics", bands);
            return view;
        }

        private static RectTransform[] CreateCrackStages(Transform uiRoot)
        {
            RectTransform layer = CreateFullScreenLayer(uiRoot, "WindowCrackLayer");
            RectTransform[] stages = new RectTransform[3];
            for (int i = 0; i < stages.Length; i++)
            {
                RectTransform stage = CreateFullScreenLayer(layer, "CrackStage_" + (i + 1));
                Image image = stage.gameObject.AddComponent<Image>();
                image.color = new Color(0.9f, 0.95f, 1f, 0.12f + i * 0.09f);
                stage.gameObject.SetActive(false);
                stages[i] = stage;
            }

            return stages;
        }

        private static TextMeshProUGUI CreateTopTitle(Transform uiRoot)
        {
            RectTransform titleRect = CreateRectChild(uiRoot, "LoopTitle");
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 70f);
            titleRect.anchoredPosition = Vector2.zero;
            TextMeshProUGUI title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
            title.text = EndingTitle;
            title.fontSize = 34f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.85f, 0.93f, 1f, 1f);
            title.gameObject.SetActive(false);
            return title;
        }

        private static LoopMenuController CreateLoopMenu(Transform uiRoot)
        {
            RectTransform menuRoot = CreateFullScreenLayer(uiRoot, "LoopMenuLayer");
            CanvasGroup group = menuRoot.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI menuText = CreateText(menuRoot, "LoopMenuText", "[ENTER] \u518d\u6b21\u63a8\u5f00\u90a3\u6247\u95e8\n[ESC] \u4fdd\u6301\u6b64\u523b\u7684\u9759\u6b62", 32f, TextAlignmentOptions.Center);
            menuText.rectTransform.anchorMin = new Vector2(0.15f, 0.38f);
            menuText.rectTransform.anchorMax = new Vector2(0.85f, 0.62f);
            menuText.rectTransform.offsetMin = Vector2.zero;
            menuText.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI finalText = CreateText(menuRoot, "StaticEndingMessage", string.Empty, 24f, TextAlignmentOptions.Center);
            finalText.rectTransform.anchorMin = new Vector2(0.15f, 0.18f);
            finalText.rectTransform.anchorMax = new Vector2(0.85f, 0.32f);
            finalText.rectTransform.offsetMin = Vector2.zero;
            finalText.rectTransform.offsetMax = Vector2.zero;
            finalText.gameObject.SetActive(false);

            LoopMenuController controller = menuRoot.gameObject.AddComponent<LoopMenuController>();
            AssignObject(controller, "canvasGroup", group);
            AssignObject(controller, "menuText", menuText);
            AssignObject(controller, "finalMessageText", finalText);
            return controller;
        }

        private static void CreateBrokenBuildings(Transform parent)
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "BrokenBuilding_" + (i + 1);
                building.transform.SetParent(parent, false);
                building.transform.position = new Vector3(-6f + i * 1.55f, 0.25f + (i % 4) * 0.32f, 0.7f);
                building.transform.rotation = Quaternion.Euler(0f, 0f, (i % 3 - 1) * 4f);
                building.transform.localScale = new Vector3(0.7f, 1.8f + (i % 5) * 0.38f, 0.35f);
                building.GetComponent<Renderer>().material.color = new Color(0.12f, 0.13f + i * 0.015f, 0.18f + i * 0.02f, 1f);
                AssignLayerRecursively(building, GameplayWorldLayerName);
            }
        }

        private static PlayerSpawnPoint CreateSpawn(Transform parent, Vector3 position)
        {
            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(parent, false);
            spawn.transform.position = position;
            return spawn.AddComponent<PlayerSpawnPoint>();
        }

        private static StoryTriggerVolume CreateStoryTrigger(Transform parent, string name, Vector3 position, StorySequence sequence)
        {
            GameObject trigger = new GameObject(name, typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.9f, 2.8f, 2f);
            StoryTriggerVolume volume = trigger.AddComponent<StoryTriggerVolume>();
            volume.SetSequence(null, sequence);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return volume;
        }

        private static DoorController CreatePrototypeDoor(Transform parent, Vector3 position)
        {
            GameObject door = CreateChild(parent, "Level03Door");
            door.transform.position = position;
            AssignLayerRecursively(door, GameplayWorldLayerName);
            DoorController controller = door.AddComponent<DoorController>();
            CreatePrimitiveChild(door.transform, "LeftPillar", PrimitiveType.Cube, new Vector3(-0.55f, 0.15f, 0f), new Vector3(0.18f, 2.4f, 0.35f), new Color(0.55f, 0.62f, 0.86f, 1f), GameplayWorldLayerName);
            CreatePrimitiveChild(door.transform, "RightPillar", PrimitiveType.Cube, new Vector3(0.55f, 0.15f, 0f), new Vector3(0.18f, 2.4f, 0.35f), new Color(0.55f, 0.62f, 0.86f, 1f), GameplayWorldLayerName);
            CreatePrimitiveChild(door.transform, "TopBeam", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(1.28f, 0.18f, 0.35f), new Color(0.55f, 0.62f, 0.86f, 1f), GameplayWorldLayerName);
            return controller;
        }

        private static LevelCompletionTrigger CreateCompletionTrigger(Transform parent, Vector3 position, LevelDefinition nextLevel, GameFlowState fallbackState)
        {
            GameObject trigger = new GameObject("FinalSequenceTrigger", typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.9f, 2.6f, 2f);
            LevelCompletionTrigger completion = trigger.AddComponent<LevelCompletionTrigger>();
            AssignObject(completion, "nextLevel", nextLevel);
            AssignEnum(completion, "fallbackNextState", fallbackState);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return completion;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color, string layerName)
        {
            return CreatePrimitiveChild(parent, name, PrimitiveType.Cube, position, scale, color, layerName);
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color, string layerName)
        {
            GameObject child = GameObject.CreatePrimitive(primitive);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localScale = scale;
            child.GetComponent<Renderer>().material.color = color;
            AssignLayerRecursively(child, layerName);
            return child;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
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

        private static RectTransform CreateRectChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
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

        private static DialogueSequence GetOrCreateDialogueSequence(string path, string[] lines)
        {
            DialogueSequence existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
            if (existing != null)
            {
                return existing;
            }

            DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            DialogueLineData[] data = new DialogueLineData[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                data[i] = new DialogueLineData("Tsukisaki", lines[i], "ending", null, null, 0f, true, 0.08f);
            }

            sequence.SetLines(data);
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

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/ScriptableObjects");
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
            string[] requiredPaths = { HostScenePath, Level02ScenePath, Level03ScenePath, EndingScenePath };
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

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }

        private static T FindComponentByObjectName<T>(Scene scene, string objectName) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] components = roots[i].GetComponentsInChildren<T>(true);
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c].gameObject.name == objectName)
                    {
                        return components[c];
                    }
                }
            }

            return null;
        }

        private static Transform FindChildInScene(Scene scene, string childName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform child = FindChildRecursive(roots[i].transform, childName);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildRecursive(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            if (target == null)
            {
                return;
            }

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
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; values != null && i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignGraphicArray(Object target, string propertyName, params Graphic[] values)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; values != null && i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignLayerMaskIfPresent(Object target, string propertyName, int value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignEnum<TEnum>(Object target, string propertyName, TEnum value) where TEnum : Enum
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = Convert.ToInt32(value);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private readonly struct EndingAssets
        {
            public EndingAssets(
                LevelDefinition level03Definition,
                LevelDefinition endingDefinition,
                StorySequence level03OpeningSequence,
                StorySequence crackSequence,
                StorySequence breakoutSequence,
                StorySequence warningSequence,
                StorySequence revelationSequence,
                StorySequence farewellSequence,
                StorySequence loopOpeningSequence)
            {
                Level03Definition = level03Definition;
                EndingDefinition = endingDefinition;
                Level03OpeningSequence = level03OpeningSequence;
                CrackSequence = crackSequence;
                BreakoutSequence = breakoutSequence;
                WarningSequence = warningSequence;
                RevelationSequence = revelationSequence;
                FarewellSequence = farewellSequence;
                LoopOpeningSequence = loopOpeningSequence;
            }

            public LevelDefinition Level03Definition { get; }
            public LevelDefinition EndingDefinition { get; }
            public StorySequence Level03OpeningSequence { get; }
            public StorySequence CrackSequence { get; }
            public StorySequence BreakoutSequence { get; }
            public StorySequence WarningSequence { get; }
            public StorySequence RevelationSequence { get; }
            public StorySequence FarewellSequence { get; }
            public StorySequence LoopOpeningSequence { get; }
        }
    }
}
