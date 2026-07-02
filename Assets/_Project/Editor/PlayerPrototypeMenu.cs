using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira.Editor
{
    public static class PlayerPrototypeMenu
    {
        private const string InputActionsPath = "Assets/_Project/Settings/GameplayInputActions.inputactions";
        private const string PrototypePrefabPath = "Assets/_Project/Prefabs/PrototypePlayer.prefab";
        private const string MovementTestScenePath = "Assets/_Project/Scenes/MovementTestScene.unity";

        [MenuItem("Tools/OwariNakiTobira/Create Player Prototype")]
        public static void CreatePlayerPrototype()
        {
            CreatePlayerPrototypePrefab(true);
        }

        [MenuItem("Tools/OwariNakiTobira/Create Movement Test Scene")]
        public static void CreateMovementTestScene()
        {
            if (File.Exists(MovementTestScenePath) && !EditorUtility.DisplayDialog("Overwrite Movement Test Scene", "MovementTestScene already exists. Overwrite it?", "Overwrite", "Cancel"))
            {
                return;
            }

            EnsureInputActionsAsset();
            if (!File.Exists(PrototypePrefabPath))
            {
                CreatePlayerPrototypePrefab(false);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateFloor();
            CreatePlatform("Platform_Low", new Vector3(-3f, 1.25f, 0f), new Vector3(2.5f, 0.35f, 2f));
            CreatePlatform("Platform_Mid", new Vector3(2.5f, 2.25f, 0f), new Vector3(2f, 0.35f, 2f));
            CreatePlatform("Platform_High", new Vector3(6f, 3.25f, 0f), new Vector3(2.5f, 0.35f, 2f));

            GameObject spawn = new GameObject("PlayerSpawnPoint");
            spawn.transform.position = new Vector3(-6f, 1.4f, 0f);
            spawn.AddComponent<PlayerSpawnPoint>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePrefabPath);
            if (prefab != null)
            {
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
                player.name = "PrototypePlayer";
            }

            Camera camera = CreateCamera();
            CreateLight();
            Selection.activeGameObject = camera.gameObject;

            Directory.CreateDirectory(Path.GetDirectoryName(MovementTestScenePath));
            EditorSceneManager.SaveScene(scene, MovementTestScenePath);
            AssetDatabase.Refresh();
        }

        private static void CreatePlayerPrototypePrefab(bool askBeforeOverwrite)
        {
            if (askBeforeOverwrite && File.Exists(PrototypePrefabPath) && !EditorUtility.DisplayDialog("Overwrite Player Prototype", "PrototypePlayer prefab already exists. Overwrite it?", "Overwrite", "Cancel"))
            {
                return;
            }

            EnsureInputActionsAsset();
            Directory.CreateDirectory(Path.GetDirectoryName(PrototypePrefabPath));

            GameObject root = new GameObject("PrototypePlayer");
            root.transform.position = Vector3.zero;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.45f;
            capsule.center = Vector3.zero;

            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Gameplay";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            PlayerInputReader inputReader = root.AddComponent<PlayerInputReader>();
            PlayerControlGate controlGate = root.AddComponent<PlayerControlGate>();
            SideScrollerMotor motor = root.AddComponent<SideScrollerMotor>();
            PlayerFacingController facing = root.AddComponent<PlayerFacingController>();
            PlayerAnimatorBridge animatorBridge = root.AddComponent<PlayerAnimatorBridge>();
            PlayerStateMachine stateMachine = root.AddComponent<PlayerStateMachine>();

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleVisual.name = "CapsuleVisual";
            capsuleVisual.transform.SetParent(visualRoot.transform, false);
            Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(root.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -1.05f, 0f);

            AssignObject(inputReader, "inputActions", actions);
            AssignObject(motor, "groundCheck", groundCheck.transform);
            AssignObject(facing, "visualRoot", visualRoot.transform);
            AssignObject(stateMachine, "inputReader", inputReader);
            AssignObject(stateMachine, "motor", motor);
            AssignObject(stateMachine, "controlGate", controlGate);
            AssignObject(stateMachine, "facingController", facing);
            AssignObject(stateMachine, "animatorBridge", animatorBridge);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrototypePrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
        }

        private static void EnsureInputActionsAsset()
        {
            if (File.Exists(InputActionsPath))
            {
                AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(InputActionsPath));
            File.WriteAllText(InputActionsPath, InputActionsJson);
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static GameObject CreateFloor()
        {
            return CreatePlatform("Floor", new Vector3(0f, -1.25f, 0f), new Vector3(18f, 0.5f, 2f));
        }

        private static GameObject CreatePlatform(string name, Vector3 position, Vector3 scale)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.position = position;
            platform.transform.localScale = scale;
            return platform;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Side View Camera");
            cameraObject.transform.position = new Vector3(0f, 2.5f, -12f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
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

        private const string InputActionsJson = @"{
    ""name"": ""GameplayInputActions"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""8a7a0b2f-2b5a-4dd8-bcb9-7c03db12b46f"",
            ""actions"": [
                { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""b5f06b27-7370-4c4f-bf8f-d43e560dc3c9"", ""expectedControlType"": ""Vector2"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": true },
                { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""9421d75a-fc1a-4623-b706-b1998dc7da9d"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                { ""name"": ""Interact"", ""type"": ""Button"", ""id"": ""c9efc4ab-0b64-4d8b-877a-e48fbc246f1f"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": false },
                { ""name"": ""Pause"", ""type"": ""Button"", ""id"": ""428cc2a2-fd3a-48e6-a41e-0358bfd97ae1"", ""expectedControlType"": ""Button"", ""processors"": """", ""interactions"": """", ""initialStateCheck"": false }
            ],
            ""bindings"": [
                { ""name"": ""AD"", ""id"": ""181578d4-adf7-47df-bc95-c27c68cf7884"", ""path"": ""2DVector"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": true, ""isPartOfComposite"": false },
                { ""name"": ""left"", ""id"": ""3d4f6ed2-bcef-4ace-99e5-305c2c29d1c6"", ""path"": ""<Keyboard>/a"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                { ""name"": ""right"", ""id"": ""62e1ee13-20db-4860-abf6-bcdd164f5d7b"", ""path"": ""<Keyboard>/d"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                { ""name"": ""Arrows"", ""id"": ""9df7cb51-8316-48b3-b141-c2c7ed1cfeb1"", ""path"": ""2DVector"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": true, ""isPartOfComposite"": false },
                { ""name"": ""left"", ""id"": ""044ce364-cf33-4875-8558-b91f39f571cd"", ""path"": ""<Keyboard>/leftArrow"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                { ""name"": ""right"", ""id"": ""b65d04e6-cf67-4c35-8010-a3d5d1666373"", ""path"": ""<Keyboard>/rightArrow"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                { ""name"": """", ""id"": ""b26e8984-8025-4fcf-b3ef-dbc9a172d6e3"", ""path"": ""<Keyboard>/space"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Jump"", ""isComposite"": false, ""isPartOfComposite"": false },
                { ""name"": """", ""id"": ""23973f15-e543-4c76-b067-8ad7ee0f85c8"", ""path"": ""<Keyboard>/e"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Interact"", ""isComposite"": false, ""isPartOfComposite"": false },
                { ""name"": """", ""id"": ""52e1bc69-340a-45e4-b751-26cc394f8ce8"", ""path"": ""<Keyboard>/escape"", ""interactions"": """", ""processors"": """", ""groups"": ""Keyboard&Mouse"", ""action"": ""Pause"", ""isComposite"": false, ""isPartOfComposite"": false }
            ]
        }
    ],
    ""controlSchemes"": [ { ""name"": ""Keyboard&Mouse"", ""bindingGroup"": ""Keyboard&Mouse"", ""devices"": [ { ""devicePath"": ""<Keyboard>"", ""isOptional"": false, ""isOR"": false } ] } ]
}";
    }
}
