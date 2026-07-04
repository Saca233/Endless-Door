using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OwariNakiTobira.Editor
{
    public static class PlayerCharacterAnimatorSetupMenu
    {
        private const string ScenePath = "Assets/_Project/Scenes/DesktopHostPrototype.unity";
        private const string CharacterModelPath = "Assets/_Project/character/character.fbx";
        private const string RunClipPath = "Assets/_Project/character/Run.fbx";
        private const string JumpClipPath = "Assets/_Project/character/Jump.fbx";
        private const string FallClipPath = "Assets/_Project/character/Fall.fbx";
        private const string LandClipPath = "Assets/_Project/character/Land.fbx";
        private const string AnimatorControllerPath = "Assets/_Project/character/Player_AC.controller";

        [MenuItem("Tools/OwariNakiTobira/Setup Prototype Player Character Animator")]
        public static void SetupPrototypePlayerCharacterAnimatorMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            bool success = SetupPrototypePlayerCharacterAnimator();
            EditorUtility.DisplayDialog(
                success ? "Character Animator Connected" : "Character Animator Setup Failed",
                success ? "PrototypePlayer now uses the character model and Player_AC." : "Setup failed. See Console for details.",
                "OK");
        }

        public static bool SetupPrototypePlayerCharacterAnimator()
        {
            List<string> changes = new List<string>();
            if (!ValidateRequiredAssets())
            {
                return false;
            }

            ConfigureClipImporter(CharacterModelPath, "Idle", true, changes);
            ConfigureClipImporter(RunClipPath, "Run", true, changes);
            ConfigureClipImporter(JumpClipPath, "Jump", false, changes);
            ConfigureClipImporter(FallClipPath, "Fall", true, changes);
            ConfigureClipImporter(LandClipPath, "Land", false, changes);

            AnimationClip idle = LoadClip(CharacterModelPath, "Idle");
            AnimationClip run = LoadClip(RunClipPath, "Run");
            AnimationClip jump = LoadClip(JumpClipPath, "Jump");
            AnimationClip fall = LoadClip(FallClipPath, "Fall");
            AnimationClip land = LoadClip(LandClipPath, "Land");
            if (idle == null || run == null || jump == null || fall == null || land == null)
            {
                Debug.LogError("[Setup Character Animator] One or more animation clips could not be loaded after import.");
                return false;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                Debug.LogError("[Setup Character Animator] Could not load Animator Controller: " + AnimatorControllerPath);
                return false;
            }

            ConfigureAnimatorController(controller, idle, run, jump, fall, land, changes);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!ConnectCharacterModelInScene(scene, controller, changes))
            {
                return false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(changes.Count == 0
                ? "[Setup Character Animator] No changes were needed."
                : "[Setup Character Animator] Applied:\n- " + string.Join("\n- ", changes));
            return true;
        }

        private static bool ValidateRequiredAssets()
        {
            bool valid = true;
            valid &= ValidateAsset(CharacterModelPath);
            valid &= ValidateAsset(RunClipPath);
            valid &= ValidateAsset(JumpClipPath);
            valid &= ValidateAsset(FallClipPath);
            valid &= ValidateAsset(LandClipPath);
            valid &= ValidateAsset(AnimatorControllerPath);
            valid &= ValidateAsset(ScenePath);
            return valid;
        }

        private static bool ValidateAsset(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }

            Debug.LogError("[Setup Character Animator] Missing asset: " + path);
            return false;
        }

        private static void ConfigureClipImporter(string path, string clipName, bool loopTime, List<string> changes)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[Setup Character Animator] Asset is not a model importer: " + path);
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("[Setup Character Animator] No animation clips found in: " + path);
                return;
            }

            bool changed = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (i == 0 && clips[i].name != clipName)
                {
                    clips[i].name = clipName;
                    changed = true;
                }

                if (clips[i].name == clipName && clips[i].loopTime != loopTime)
                {
                    clips[i].loopTime = loopTime;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            changes.Add(Path.GetFileName(path) + " clip = " + clipName + ", loopTime = " + loopTime);
        }

        private static AnimationClip LoadClip(string path, string preferredName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            AnimationClip fallback = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip)
                {
                    fallback ??= clip;
                    if (clip.name == preferredName)
                    {
                        return clip;
                    }
                }
            }

            return fallback;
        }

        private static void ConfigureAnimatorController(
            AnimatorController controller,
            AnimationClip idle,
            AnimationClip run,
            AnimationClip jump,
            AnimationClip fall,
            AnimationClip land,
            List<string> changes)
        {
            EnsureParameter(controller, "HorizontalSpeed", AnimatorControllerParameterType.Float, changes);
            EnsureParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float, changes);
            EnsureParameter(controller, "Grounded", AnimatorControllerParameterType.Bool, changes);
            EnsureParameter(controller, "State", AnimatorControllerParameterType.Int, changes);

            if (controller.layers == null || controller.layers.Length == 0)
            {
                Debug.LogError("[Setup Character Animator] Player_AC has no Animator layers.");
                return;
            }

            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorStateMachine stateMachine = layer.stateMachine;
            ClearStateMachine(stateMachine);

            AnimatorState idleState = AddState(stateMachine, "Idle", idle, new Vector3(260f, 80f, 0f));
            AnimatorState runState = AddState(stateMachine, "Run", run, new Vector3(520f, 80f, 0f));
            AnimatorState jumpState = AddState(stateMachine, "Jump", jump, new Vector3(260f, 220f, 0f));
            AnimatorState fallState = AddState(stateMachine, "Fall", fall, new Vector3(520f, 220f, 0f));
            AnimatorState landState = AddState(stateMachine, "Land", land, new Vector3(780f, 220f, 0f));
            AnimatorState disabledState = AddState(stateMachine, "Disabled", idle, new Vector3(780f, 80f, 0f));
            stateMachine.defaultState = idleState;

            AddStateTransition(stateMachine, idleState, PlayerStateId.Idle);
            AddStateTransition(stateMachine, runState, PlayerStateId.Run);
            AddStateTransition(stateMachine, jumpState, PlayerStateId.Jump);
            AddStateTransition(stateMachine, fallState, PlayerStateId.Fall);
            AddStateTransition(stateMachine, landState, PlayerStateId.Land);
            AddStateTransition(stateMachine, disabledState, PlayerStateId.Disabled);

            EditorUtility.SetDirty(controller);
            changes.Add("Updated existing Player_AC Animator Controller states and State-driven transitions");
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, List<string> changes)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, type);
            changes.Add("Added Animator parameter " + name);
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                stateMachine.RemoveState(states[i].state);
            }

            AnimatorStateTransition[] anyTransitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < anyTransitions.Length; i++)
            {
                stateMachine.RemoveAnyStateTransition(anyTransitions[i]);
            }

            AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;
            for (int i = 0; i < entryTransitions.Length; i++)
            {
                stateMachine.RemoveEntryTransition(entryTransitions[i]);
            }
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position)
        {
            AnimatorState state = stateMachine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddStateTransition(AnimatorStateMachine stateMachine, AnimatorState state, PlayerStateId stateId)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, (int)stateId, "State");
        }

        private static bool ConnectCharacterModelInScene(Scene scene, RuntimeAnimatorController controller, List<string> changes)
        {
            Transform player = FindSceneTransform(scene, "PrototypePlayer");
            if (player == null)
            {
                Debug.LogError("[Setup Character Animator] Missing PrototypePlayer in DesktopHostPrototype.");
                return false;
            }

            Transform visualRoot = FindInChildrenByName(player, "VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError("[Setup Character Animator] Missing PrototypePlayer/VisualRoot.");
                return false;
            }

            Transform capsuleVisual = FindInChildrenByName(visualRoot, "CapsuleVisual");
            if (capsuleVisual != null && capsuleVisual.gameObject.activeSelf)
            {
                capsuleVisual.gameObject.SetActive(false);
                EditorUtility.SetDirty(capsuleVisual.gameObject);
                changes.Add("Disabled CapsuleVisual");
            }

            GameObject characterModel = GetOrCreateCharacterModel(visualRoot, changes);
            if (characterModel == null)
            {
                return false;
            }

            Animator animator = characterModel.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = characterModel.AddComponent<Animator>();
                changes.Add("Added Animator to CharacterModel");
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                changes.Add("Assigned Player_AC to character Animator");
            }

            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(CharacterModelPath);
            if (animator.avatar == null && avatar != null)
            {
                animator.avatar = avatar;
                changes.Add("Assigned character Avatar to Animator");
            }

            if (animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
                changes.Add("Disabled root motion on character Animator");
            }

            PlayerAnimatorBridge bridge = player.GetComponent<PlayerAnimatorBridge>();
            if (bridge == null)
            {
                Debug.LogError("[Setup Character Animator] PrototypePlayer is missing PlayerAnimatorBridge.");
                return false;
            }

            SerializedObject bridgeObject = new SerializedObject(bridge);
            SerializedProperty animatorProperty = bridgeObject.FindProperty("animator");
            if (animatorProperty != null && animatorProperty.objectReferenceValue != animator)
            {
                animatorProperty.objectReferenceValue = animator;
                bridgeObject.ApplyModifiedPropertiesWithoutUndo();
                changes.Add("Assigned character Animator to PlayerAnimatorBridge");
            }

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(bridge);
            return true;
        }

        private static GameObject GetOrCreateCharacterModel(Transform visualRoot, List<string> changes)
        {
            Transform existing = FindInChildrenByName(visualRoot, "CharacterModel");
            if (existing != null)
            {
                ConfigureCharacterModelTransform(existing);
                return existing.gameObject;
            }

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            if (modelPrefab == null)
            {
                Debug.LogError("[Setup Character Animator] Could not load character prefab from: " + CharacterModelPath);
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, visualRoot);
            if (instance == null)
            {
                Debug.LogError("[Setup Character Animator] Could not instantiate character model prefab.");
                return null;
            }

            instance.name = "CharacterModel";
            ConfigureCharacterModelTransform(instance.transform);
            changes.Add("Instantiated character model under VisualRoot");
            return instance;
        }

        private static void ConfigureCharacterModelTransform(Transform model)
        {
            model.localPosition = new Vector3(0f, -1f, 0f);
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;
            EditorUtility.SetDirty(model);
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
    }
}
