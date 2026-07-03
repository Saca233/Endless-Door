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
    public static class Level02GrayboxMenu
    {
        private const string HostScenePath = "Assets/_Project/Scenes/DesktopHost_Level01Graybox.unity";
        private const string Level01ScenePath = "Assets/_Project/Scenes/Level01_Prologue.unity";
        private const string Level02ScenePath = "Assets/_Project/Scenes/Level02_Layers.unity";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Level02_Layers";
        private const string Level01DefinitionPath = "Assets/_Project/ScriptableObjects/Level01_Prologue/Levels/Level01_PrologueDefinition.asset";
        private const string DesktopWorldLayerName = "DesktopWorld";
        private const string GameplayWorldLayerName = "GameplayWorld";
        private const string GameplayPlayerLayerName = "GameplayPlayer";
        private const string PuzzleObjectLayerName = "PuzzleObject";
        private const string StackedCityWorldLayerName = "StackedCityWorld";
        private const string PlayerTagName = "Player";
        private const string LayerTitle = "\u7d42\u308f\u308a\u306a\u304d\u6249 - Layer 02";

        [MenuItem("Tools/OwariNakiTobira/Create Level02 Graybox")]
        public static void CreateLevel02Graybox()
        {
            if (!File.Exists(HostScenePath) || !File.Exists(Level01ScenePath))
            {
                EditorUtility.DisplayDialog(
                    "Level01 Graybox Required",
                    "Create Level01 Graybox first so this tool can preserve the persistent DesktopHost and retarget the first door.",
                    "OK");
                return;
            }

            if (File.Exists(Level02ScenePath))
            {
                EditorUtility.DisplayDialog("Level02 Graybox Already Exists", "Level02_Layers already exists. Rename or remove it before generating a fresh graybox.", "OK");
                return;
            }

            EnsureProjectFolders();
            EnsureProjectLayers();
            EnsureTag(PlayerTagName);

            Level02Assets assets = CreateAssets();
            UpdateLevel01Definition(assets.Level02Definition);
            UpdateLevel01DoorTarget(assets.Level02Definition);
            CreateLevel02Scene(assets);
            AugmentHostScene(assets);
            EnsureBuildSettingsScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(HostScenePath, OpenSceneMode.Single);
            Debug.Log("Created Level02_Layers graybox. Start from DesktopHost_Level01Graybox and pass through the first door.");
        }

        private static Level02Assets CreateAssets()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(DataFolder + "/Dialogue");
            EnsureFolder(DataFolder + "/Story");
            EnsureFolder(DataFolder + "/Levels");
            EnsureFolder(DataFolder + "/WindowLayouts");

            DialogueSequence copyDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level02_CopyDiscoveryDialogue.asset",
                new[] { "\u307e\u305f\u540c\u3058\u8857\u2026\u2026\uff1f\u3055\u3063\u304d\u306e\u90e8\u5c4b\u3068\u3001\u5168\u90e8\u304c\u5c11\u3057\u305a\u3064\u305a\u308c\u3066\u308b\u3002" });
            DialogueSequence endlessDoorDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level02_EndlessDoorsDialogue.asset",
                new[] { "\u6249\u306e\u5148\u306b\u6249\u304c\u3042\u308b\u3002\u7d42\u308f\u3089\u306a\u3044\u3088\u3046\u306b\u3001\u8ab0\u304b\u304c\u91cd\u306d\u3066\u3044\u308b\u3093\u3060\u3002" });
            DialogueSequence losingHopeDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level02_LosingHopeDialogue.asset",
                new[] { "\u3082\u3057\u51fa\u53e3\u306a\u3093\u3066\u6700\u521d\u304b\u3089\u7121\u304b\u3063\u305f\u3089\u2026\u2026\u79c1\u306f\u3001\u4f55\u5ea6\u3053\u3053\u3067\u76ee\u3092\u899a\u307e\u3059\u306e\uff1f" });
            DialogueSequence pointerDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level02_PointerStillMovesDialogue.asset",
                new[] { "\u3067\u3082\u3001\u3042\u306a\u305f\u306e\u30dd\u30a4\u30f3\u30bf\u306f\u307e\u3060\u52d5\u3044\u3066\u308b\u3002\u307e\u3060\u3001\u898b\u6368\u3066\u3089\u308c\u3066\u306a\u3044\u3002" });
            DialogueSequence nextDoorDialogue = GetOrCreateDialogueSequence(
                DataFolder + "/Dialogue/Level02_NextDoorDialogue.asset",
                new[] { "\u6b21\u306e\u6249\u3078\u884c\u3053\u3046\u3002\u6016\u304f\u3066\u3082\u3001\u3053\u3053\u3067\u6b62\u307e\u308b\u3088\u308a\u306f\u307e\u3057\u3060\u3088\u306d\u3002" });

            StorySequence copySequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level02_CopyDiscoverySequence.asset", copyDialogue, "level02.copy_discovered");
            StorySequence endlessDoorSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level02_EndlessDoorsSequence.asset", endlessDoorDialogue, "level02.endless_doors_seen");
            StorySequence losingHopeSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level02_LosingHopeSequence.asset", losingHopeDialogue, "level02.losing_hope");
            StorySequence pointerSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level02_PointerStillMovesSequence.asset", pointerDialogue, "level02.pointer_moves");
            StorySequence nextDoorSequence = GetOrCreateLockedDialogueSequence(DataFolder + "/Story/Level02_NextDoorSequence.asset", nextDoorDialogue, "level02.next_door_seen");

            LevelDefinition level02Definition = GetOrCreateLevelDefinition(
                DataFolder + "/Levels/Level02_LayersDefinition.asset",
                "Level02_Layers",
                "Level02 Layers",
                GameFlowState.Level02,
                copySequence,
                null);

            WindowLayoutDefinition[] layouts =
            {
                GetOrCreateWindowLayout(DataFolder + "/WindowLayouts/ErrorWindow_01.asset", new Vector2(-465f, 190f), new Vector2(330f, 180f), 0, true, "level02.error.01"),
                GetOrCreateWindowLayout(DataFolder + "/WindowLayouts/ErrorWindow_02.asset", new Vector2(250f, 235f), new Vector2(360f, 210f), 1, true, "level02.error.02"),
                GetOrCreateWindowLayout(DataFolder + "/WindowLayouts/ErrorWindow_03.asset", new Vector2(20f, -280f), new Vector2(420f, 170f), 2, true, "level02.error.03")
            };

            return new Level02Assets(
                level02Definition,
                layouts,
                copySequence,
                endlessDoorSequence,
                losingHopeSequence,
                pointerSequence,
                nextDoorSequence);
        }

        private static void UpdateLevel01Definition(LevelDefinition level02Definition)
        {
            LevelDefinition level01Definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Level01DefinitionPath);
            if (level01Definition == null)
            {
                Debug.LogWarning("Level01 definition was not found. The Level01 scene trigger is still updated when possible.");
                return;
            }

            level01Definition.Configure(
                level01Definition.SceneName,
                level01Definition.DisplayName,
                level01Definition.FlowState,
                level01Definition.OpeningSequence,
                level02Definition);
            EditorUtility.SetDirty(level01Definition);
        }

        private static void UpdateLevel01DoorTarget(LevelDefinition level02Definition)
        {
            Scene scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            LevelCompletionTrigger[] completionTriggers = FindComponentsInScene<LevelCompletionTrigger>(scene);
            for (int i = 0; i < completionTriggers.Length; i++)
            {
                AssignObject(completionTriggers[i], "nextLevel", level02Definition);
                AssignEnum(completionTriggers[i], "fallbackNextState", GameFlowState.Level02);
                EditorUtility.SetDirty(completionTriggers[i]);
            }

            EditorSceneManager.SaveScene(scene, Level01ScenePath);
        }

        private static void CreateLevel02Scene(Level02Assets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Level02_Layers");
            LevelEntryPoint entryPoint = root.AddComponent<LevelEntryPoint>();

            GameObject environment = CreateChild(root.transform, "CorruptedCity");
            CreatePlatform(environment.transform, "Ground_Corrupted", new Vector3(2.8f, -1.1f, 0f), new Vector3(24f, 0.4f, 2.2f), new Color(0.23f, 0.24f, 0.3f, 1f));
            CreatePlatform(environment.transform, "OffsetPlatform_A", new Vector3(-0.6f, 0.15f, 0f), new Vector3(2.2f, 0.28f, 1.8f), new Color(0.28f, 0.19f, 0.34f, 1f));
            CreatePlatform(environment.transform, "OffsetPlatform_B", new Vector3(3.7f, 0.75f, 0f), new Vector3(2.7f, 0.28f, 1.8f), new Color(0.16f, 0.3f, 0.32f, 1f));
            CreatePlatform(environment.transform, "FinalApproach", new Vector3(9.4f, -0.45f, 0f), new Vector3(4.8f, 0.3f, 1.8f), new Color(0.34f, 0.22f, 0.28f, 1f));
            CreateCorruptedBuildings(environment.transform);
            CreateGlitchBlocks(environment.transform);

            PlayerSpawnPoint spawn = CreateSpawn(root.transform, new Vector3(-6.2f, 0.1f, 0f));
            StoryTriggerVolume copyTrigger = CreateStoryTrigger(root.transform, "CopyDiscoveryTrigger", new Vector3(-5.55f, 0.25f, 0f), assets.CopySequence);
            StoryTriggerVolume endlessTrigger = CreateStoryTrigger(root.transform, "EndlessDoorsTrigger", new Vector3(-1.8f, 0.25f, 0f), assets.EndlessDoorSequence);
            StoryTriggerVolume hopeTrigger = CreateStoryTrigger(root.transform, "LosingHopeTrigger", new Vector3(2.8f, 0.25f, 0f), assets.LosingHopeSequence);
            StoryTriggerVolume pointerTrigger = CreateStoryTrigger(root.transform, "PointerStillMovesTrigger", new Vector3(5.8f, 0.25f, 0f), assets.PointerSequence);
            StoryTriggerVolume nextDoorTrigger = CreateStoryTrigger(root.transform, "NextDoorApproachTrigger", new Vector3(8.9f, 0.25f, 0f), assets.NextDoorSequence);
            DoorController door = CreatePrototypeDoor(root.transform, new Vector3(10.6f, 0f, 0f));
            LevelCompletionTrigger completionTrigger = CreateCompletionTrigger(root.transform, new Vector3(11.15f, 0.15f, 0f));

            AssignObject(entryPoint, "levelDefinition", assets.Level02Definition);
            AssignObject(entryPoint, "playerSpawn", spawn);
            AssignObjectArray(entryPoint, "storyTriggers", copyTrigger, endlessTrigger, hopeTrigger, pointerTrigger, nextDoorTrigger);
            AssignObjectArray(entryPoint, "completionTriggers", completionTrigger);
            AssignObjectArray(entryPoint, "doors", door);

            Directory.CreateDirectory(Path.GetDirectoryName(Level02ScenePath));
            EditorSceneManager.SaveScene(scene, Level02ScenePath);
        }

        private static void AugmentHostScene(Level02Assets assets)
        {
            Scene scene = EditorSceneManager.OpenScene(HostScenePath, OpenSceneMode.Single);
            DesktopWindowManager windowManager = FindComponentInScene<DesktopWindowManager>(scene);
            AdditiveLevelLoader levelLoader = FindComponentInScene<AdditiveLevelLoader>(scene);
            RuntimeResetService resetService = FindComponentInScene<RuntimeResetService>(scene);
            Transform systems = FindChildInScene(scene, "Systems");
            Transform gameplayWorld = FindChildInScene(scene, "GameplayWorld");
            RectTransform desktopWindowLayer = FindChildInScene(scene, "DesktopWindowLayer") as RectTransform;
            GameWindowView mainGameWindowView = FindComponentByObjectName<GameWindowView>(scene, "MainGameWindow");
            GameWindowCamera mainGameWindowCamera = FindComponentByObjectName<GameWindowCamera>(scene, "GameplayCamera");

            if (windowManager == null || systems == null || gameplayWorld == null || desktopWindowLayer == null || mainGameWindowView == null || mainGameWindowCamera == null)
            {
                Debug.LogWarning("Could not augment DesktopHost_Level01Graybox. Required Level01 host objects were missing.");
                return;
            }

            GameWindowCamera stackedCamera = CreateStackedCityWorld(gameplayWorld, mainGameWindowCamera);
            DesktopWindowController stackedWindow = CreateStackedCityWindow(desktopWindowLayer, windowManager, stackedCamera);
            RuntimeRenderTexture stackedTexture = stackedWindow.GetComponent<RuntimeRenderTexture>();
            GameWindowView stackedWindowView = stackedWindow.GetComponent<GameWindowView>();

            ErrorWindowController[] errorWindows = new ErrorWindowController[assets.ErrorLayouts.Length];
            ProjectedWindowBarrierRule[] rules = new ProjectedWindowBarrierRule[assets.ErrorLayouts.Length];
            Transform barrierRoot = CreateChild(gameplayWorld, "Level02ProjectedErrorBarriers").transform;

            for (int i = 0; i < assets.ErrorLayouts.Length; i++)
            {
                errorWindows[i] = CreateErrorWindow(desktopWindowLayer, windowManager, "ErrorWindow_" + (i + 1));
                rules[i] = CreateBarrierRule(barrierRoot, "ProjectedErrorBarrier_" + (i + 1), errorWindows[i], mainGameWindowView, mainGameWindowCamera.GameplayCamera, assets.ErrorLayouts[i].AssociatedPuzzleRuleId);
            }

            ErrorWindowPool pool = CreateChild(systems, "Level02ErrorWindowPool").AddComponent<ErrorWindowPool>();
            AssignObject(pool, "runtimeResetService", resetService);
            AssignObjectArray(pool, "preloadedWindows", errorWindows);
            AssignObjectArray(pool, "initialLayouts", assets.ErrorLayouts);
            AssignObjectArray(pool, "barrierRules", rules);

            StackedCityWindowView stackedView = CreateChild(systems, "StackedCityWindowView").AddComponent<StackedCityWindowView>();
            AssignObject(stackedView, "windowController", stackedWindow);
            AssignObject(stackedView, "runtimeRenderTexture", stackedTexture);
            AssignObject(stackedView, "cityCamera", stackedCamera);
            AssignObject(stackedView, "cityWindowView", stackedWindowView);
            AssignObject(stackedView, "levelLoader", levelLoader);
            AssignObject(stackedView, "activeLevel", assets.Level02Definition);
            AssignObject(stackedView, "errorWindowPool", pool);
            AssignObject(stackedView, "runtimeResetService", resetService);
            AssignObjectArray(stackedView, "errorWindowLayouts", assets.ErrorLayouts);

            SetWindowVisible(stackedWindow, false);
            for (int i = 0; i < errorWindows.Length; i++)
            {
                errorWindows[i].Deactivate();
                rules[i].SetRuleEnabled(false);
            }

            EditorSceneManager.SaveScene(scene, HostScenePath);
        }

        private static GameWindowCamera CreateStackedCityWorld(Transform gameplayWorld, GameWindowCamera mainGameWindowCamera)
        {
            GameObject layerRoot = CreateChild(gameplayWorld, "StackedCityWorld");
            AssignLayerRecursively(layerRoot, StackedCityWorldLayerName);

            Camera mainCamera = mainGameWindowCamera.GameplayCamera;
            GameObject cameraObject = CreateChild(layerRoot.transform, "StackedCityCamera");
            cameraObject.transform.position = mainCamera != null ? mainCamera.transform.position + new Vector3(0.45f, 0.2f, 0f) : new Vector3(3.4f, 2.8f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = mainCamera != null ? mainCamera.orthographicSize : 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.02f, 0.045f, 1f);
            camera.depth = -20f;
            GameWindowCamera gameWindowCamera = cameraObject.AddComponent<GameWindowCamera>();
            AssignObject(gameWindowCamera, "gameplayCamera", camera);
            AssignLayerMask(gameWindowCamera, "gameplayLayers", LayerMask.GetMask(StackedCityWorldLayerName));
            AssignVector2Int(gameWindowCamera, "renderResolution", new Vector2Int(960, 540));

            CreatePlatform(layerRoot.transform, "Layer02EchoGround", new Vector3(2.7f, -1.1f, 0.45f), new Vector3(22f, 0.32f, 1.2f), new Color(0.2f, 0.11f, 0.24f, 1f), StackedCityWorldLayerName);
            for (int i = 0; i < 8; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "StackedEchoBuilding_" + (i + 1);
                building.transform.SetParent(layerRoot.transform, false);
                building.transform.position = new Vector3(-5.4f + i * 1.65f, 0.45f + (i % 3) * 0.34f, 0.55f);
                building.transform.localScale = new Vector3(0.75f, 1.8f + (i % 4) * 0.5f, 0.35f);
                building.GetComponent<Renderer>().material.color = new Color(0.22f + i * 0.015f, 0.08f, 0.27f, 1f);
                AssignLayerRecursively(building, StackedCityWorldLayerName);
            }

            return gameWindowCamera;
        }

        private static DesktopWindowController CreateStackedCityWindow(RectTransform parent, DesktopWindowManager manager, GameWindowCamera gameWindowCamera)
        {
            DesktopWindowController window = CreateWindowInstance(parent, manager, "StackedCityWindow", LayerTitle, new Vector2(120f, 130f), new Vector2(760f, 420f), false);
            RectTransform contentRoot = window.View.ContentRoot;
            GameObject rawObject = new GameObject("StackedCityRenderTexture", typeof(RectTransform), typeof(RawImage));
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

            RuntimeRenderTexture runtimeTexture = window.gameObject.AddComponent<RuntimeRenderTexture>();
            AssignObject(runtimeTexture, "gameWindowCamera", gameWindowCamera);
            AssignObject(runtimeTexture, "targetRawImage", rawImage);
            AssignObject(runtimeTexture, "gameWindowView", gameWindowView);
            return window;
        }

        private static ErrorWindowController CreateErrorWindow(RectTransform parent, DesktopWindowManager manager, string name)
        {
            DesktopWindowController window = CreateWindowInstance(parent, manager, name, "Error 404", Vector2.zero, new Vector2(330f, 180f), true);
            TextMeshProUGUI content = CreateText(window.View.ContentRoot, "ErrorContent", "ERROR 404\nMissing corridor data.", 18f, TextAlignmentOptions.Center);
            RectTransform rect = content.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -12f);
            ErrorWindowController errorWindow = window.gameObject.AddComponent<ErrorWindowController>();
            AssignObject(errorWindow, "windowController", window);
            AssignObject(errorWindow, "model", window.Model);
            AssignObject(errorWindow, "view", window.View);
            AssignObject(errorWindow, "contentText", content);
            return errorWindow;
        }

        private static ProjectedWindowBarrierRule CreateBarrierRule(Transform parent, string name, ErrorWindowController errorWindow, GameWindowView mainWindowView, Camera gameplayCamera, string ruleId)
        {
            GameObject barrier = CreateChild(parent, name);
            AssignLayerRecursively(barrier, PuzzleObjectLayerName);
            BoxCollider collider = barrier.AddComponent<BoxCollider>();
            collider.enabled = false;

            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "BarrierPreview";
            preview.transform.SetParent(barrier.transform, false);
            preview.GetComponent<Renderer>().material.color = new Color(0.9f, 0.1f, 0.2f, 0.65f);
            Object.DestroyImmediate(preview.GetComponent<Collider>());
            preview.SetActive(false);
            AssignLayerRecursively(preview, PuzzleObjectLayerName);

            ProjectedWindowBarrierRule rule = barrier.AddComponent<ProjectedWindowBarrierRule>();
            AssignString(rule, "ruleId", ruleId);
            AssignObject(rule, "errorWindow", errorWindow);
            AssignObject(rule, "mainGameWindowView", mainWindowView);
            AssignObject(rule, "gameplayCamera", gameplayCamera);
            AssignObject(rule, "barrierCollider", collider);
            AssignObject(rule, "barrierPreview", preview.transform);
            AssignBool(rule, "ruleEnabled", false);
            return rule;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color, string layerName = GameplayWorldLayerName)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.transform.localScale = scale;
            platform.GetComponent<Renderer>().material.color = color;
            AssignLayerRecursively(platform, layerName);
            return platform;
        }

        private static void CreateCorruptedBuildings(Transform parent)
        {
            GameObject buildings = CreateChild(parent, "CorruptedPlaceholderBuildings");
            AssignLayerRecursively(buildings, GameplayWorldLayerName);
            for (int i = 0; i < 10; i++)
            {
                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = "CorruptedBuilding_" + (i + 1);
                building.transform.SetParent(buildings.transform, false);
                building.transform.position = new Vector3(-5.7f + i * 1.6f, 0.35f + (i % 4) * 0.25f, 0.8f);
                building.transform.localScale = new Vector3(0.8f, 1.9f + (i % 5) * 0.45f, 0.55f);
                building.transform.rotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 2.5f : -3.5f);
                building.GetComponent<Renderer>().material.color = new Color(0.14f + i * 0.012f, 0.11f, 0.2f + i * 0.018f, 1f);
                AssignLayerRecursively(building, GameplayWorldLayerName);
            }
        }

        private static void CreateGlitchBlocks(Transform parent)
        {
            GameObject glitches = CreateChild(parent, "ReplaceableGlitchBlocks");
            for (int i = 0; i < 7; i++)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "FloatingErrorBlock_" + (i + 1);
                block.transform.SetParent(glitches.transform, false);
                block.transform.position = new Vector3(-4.2f + i * 2.1f, 2.1f + (i % 2) * 0.7f, 0f);
                block.transform.localScale = new Vector3(0.45f + (i % 3) * 0.18f, 0.16f, 1.15f);
                block.GetComponent<Renderer>().material.color = new Color(0.85f, 0.08f, 0.18f, 1f);
                AssignLayerRecursively(block, GameplayWorldLayerName);
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

        private static StoryTriggerVolume CreateStoryTrigger(Transform parent, string name, Vector3 position, StorySequence sequence)
        {
            GameObject trigger = new GameObject(name, typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.8f, 2.8f, 2f);
            StoryTriggerVolume volume = trigger.AddComponent<StoryTriggerVolume>();
            volume.SetSequence(null, sequence);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return volume;
        }

        private static DoorController CreatePrototypeDoor(Transform parent, Vector3 position)
        {
            GameObject door = CreateChild(parent, "Layer02Door");
            door.transform.position = position;
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
            part.GetComponent<Renderer>().material.color = new Color(0.68f, 0.28f, 0.86f, 1f);
            AssignLayerRecursively(part, GameplayWorldLayerName);
        }

        private static LevelCompletionTrigger CreateCompletionTrigger(Transform parent, Vector3 position)
        {
            GameObject trigger = new GameObject("Level02CompletionTrigger", typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            BoxCollider collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.9f, 2.4f, 2f);
            LevelCompletionTrigger completion = trigger.AddComponent<LevelCompletionTrigger>();
            AssignEnum(completion, "fallbackNextState", GameFlowState.Level03);
            AssignLayerRecursively(trigger, GameplayWorldLayerName);
            return completion;
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
            background.color = new Color(0.08f, 0.06f, 0.09f, 0.97f);

            RectTransform titleBar = CreateRectChild(root, "TitleBar");
            titleBar.anchorMin = new Vector2(0f, 1f);
            titleBar.anchorMax = new Vector2(1f, 1f);
            titleBar.pivot = new Vector2(0.5f, 1f);
            titleBar.sizeDelta = new Vector2(0f, 34f);
            titleBar.anchoredPosition = Vector2.zero;
            Image titleBarImage = titleBar.gameObject.AddComponent<Image>();
            titleBarImage.color = new Color(0.22f, 0.08f, 0.12f, 1f);

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
                data[i] = new DialogueLineData("Tsukisaki", lines[i], "level02", null, null, 0f, true, 0.08f);
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

        private static LevelDefinition GetOrCreateLevelDefinition(string path, string sceneName, string displayName, GameFlowState flowState, StorySequence openingSequence, LevelDefinition nextLevel)
        {
            LevelDefinition existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (existing != null)
            {
                return existing;
            }

            LevelDefinition definition = ScriptableObject.CreateInstance<LevelDefinition>();
            definition.Configure(sceneName, displayName, flowState, openingSequence, nextLevel);
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static WindowLayoutDefinition GetOrCreateWindowLayout(string path, Vector2 position, Vector2 size, int focusOrder, bool draggable, string puzzleRuleId)
        {
            WindowLayoutDefinition existing = AssetDatabase.LoadAssetAtPath<WindowLayoutDefinition>(path);
            if (existing != null)
            {
                return existing;
            }

            WindowLayoutDefinition layout = ScriptableObject.CreateInstance<WindowLayoutDefinition>();
            layout.Configure(position, size, focusOrder, draggable, puzzleRuleId);
            AssetDatabase.CreateAsset(layout, path);
            return layout;
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

        private static void EnsureProjectLayers()
        {
            EnsureLayer(DesktopWorldLayerName);
            EnsureLayer(GameplayWorldLayerName);
            EnsureLayer(GameplayPlayerLayerName);
            EnsureLayer(PuzzleObjectLayerName);
            EnsureLayer(StackedCityWorldLayerName);
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

        private static void AssignEnum<TEnum>(Object target, string propertyName, TEnum value) where TEnum : Enum
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = Convert.ToInt32(value);
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

        private static void AssignString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value ?? string.Empty;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private readonly struct Level02Assets
        {
            public Level02Assets(
                LevelDefinition level02Definition,
                WindowLayoutDefinition[] errorLayouts,
                StorySequence copySequence,
                StorySequence endlessDoorSequence,
                StorySequence losingHopeSequence,
                StorySequence pointerSequence,
                StorySequence nextDoorSequence)
            {
                Level02Definition = level02Definition;
                ErrorLayouts = errorLayouts;
                CopySequence = copySequence;
                EndlessDoorSequence = endlessDoorSequence;
                LosingHopeSequence = losingHopeSequence;
                PointerSequence = pointerSequence;
                NextDoorSequence = nextDoorSequence;
            }

            public LevelDefinition Level02Definition { get; }
            public WindowLayoutDefinition[] ErrorLayouts { get; }
            public StorySequence CopySequence { get; }
            public StorySequence EndlessDoorSequence { get; }
            public StorySequence LosingHopeSequence { get; }
            public StorySequence PointerSequence { get; }
            public StorySequence NextDoorSequence { get; }
        }
    }
}
