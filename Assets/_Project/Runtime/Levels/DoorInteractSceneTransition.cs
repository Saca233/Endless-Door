using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class DoorInteractSceneTransition : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "stage2";
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private string requiredPlayerName = "PrototypePlayer";
        [SerializeField] private bool keepPersistentDesktopHost = true;
        [SerializeField] private string stageRootName = "Stage2_Basic";
        [SerializeField] private Vector3 fallbackStageSpawnPosition = new Vector3(-6f, 0.05f, 0f);
        [SerializeField] private string[] persistentHostLevelGroupsToHide =
        {
            "GameplayWorld/Environment",
            "GameplayWorld/Platforms",
            "GameplayWorld/Obstacles"
        };
        [SerializeField] private string[] loadedSceneStandaloneGroupsToHide =
        {
            "Systems",
            "DesktopWorld",
            "GameplayCameraRig",
            "UI",
            "GameplayWorld/Player"
        };

        private PlayerInputReader playerInputReader;
        private Transform persistentPlayer;
        private Transform persistentHostRoot;
        private bool waitingForSceneLoad;
        private bool playerInside;
        private bool used;

        private void OnDestroy()
        {
            if (waitingForSceneLoad)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void Update()
        {
            if (!playerInside || used || string.IsNullOrWhiteSpace(targetSceneName))
            {
                return;
            }

            if (WasInteractPressed())
            {
                used = triggerOnce;
                BeginTransition();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInside = true;
            playerInputReader = other.GetComponentInParent<PlayerInputReader>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInside = false;
            playerInputReader = null;
        }

        private bool WasInteractPressed()
        {
            if (playerInputReader != null && playerInputReader.ConsumeInteractPressed())
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
        }

        private void BeginTransition()
        {
            if (!keepPersistentDesktopHost)
            {
                SceneManager.LoadScene(targetSceneName, loadSceneMode);
                return;
            }

            CachePersistentHostReferences();
            HidePersistentHostLevelContent();
            waitingForSceneLoad = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.LoadScene(targetSceneName, loadSceneMode);
        }

        private void CachePersistentHostReferences()
        {
            persistentPlayer = null;
            persistentHostRoot = null;

            if (playerInputReader != null)
            {
                PlayerStateMachine stateMachine = playerInputReader.GetComponentInParent<PlayerStateMachine>();
                SideScrollerMotor motor = playerInputReader.GetComponentInParent<SideScrollerMotor>();
                if (stateMachine != null)
                {
                    persistentPlayer = stateMachine.transform;
                }
                else if (motor != null)
                {
                    persistentPlayer = motor.transform;
                }
                else
                {
                    persistentPlayer = playerInputReader.transform;
                }
            }

            if (persistentPlayer != null)
            {
                persistentHostRoot = persistentPlayer.root;
            }
        }

        private void HidePersistentHostLevelContent()
        {
            if (persistentHostRoot == null || persistentHostLevelGroupsToHide == null)
            {
                return;
            }

            for (int i = 0; i < persistentHostLevelGroupsToHide.Length; i++)
            {
                Transform target = FindChildPath(persistentHostRoot, persistentHostLevelGroupsToHide[i]);
                if (target != null)
                {
                    target.gameObject.SetActive(false);
                }
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!waitingForSceneLoad)
            {
                return;
            }

            waitingForSceneLoad = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            Transform stageRoot = FindRootTransform(scene, stageRootName);
            if (stageRoot != null)
            {
                HideLoadedStandaloneGroups(stageRoot);
                Transform spawn = FindChildPath(stageRoot, "GameplayWorld/Stage2Spawn");
                MovePersistentPlayer(spawn != null ? spawn.position : fallbackStageSpawnPosition);
                RestorePersistentPlayerControl();
            }
            else
            {
                MovePersistentPlayer(fallbackStageSpawnPosition);
                RestorePersistentPlayerControl();
            }
        }

        private void HideLoadedStandaloneGroups(Transform stageRoot)
        {
            if (loadedSceneStandaloneGroupsToHide == null)
            {
                return;
            }

            for (int i = 0; i < loadedSceneStandaloneGroupsToHide.Length; i++)
            {
                Transform target = FindChildPath(stageRoot, loadedSceneStandaloneGroupsToHide[i]);
                if (target != null)
                {
                    target.gameObject.SetActive(false);
                }
            }
        }

        private void MovePersistentPlayer(Vector3 position)
        {
            if (persistentPlayer == null)
            {
                return;
            }

            Rigidbody body = persistentPlayer.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
            }
            else
            {
                persistentPlayer.position = position;
            }

            SideScrollerMotor motor = persistentPlayer.GetComponent<SideScrollerMotor>();
            motor?.ClearQueuedJump();
        }

        private void RestorePersistentPlayerControl()
        {
            PlayerControlGate controlGate = null;
            if (persistentHostRoot != null)
            {
                Transform controlGateTransform = FindChildPath(persistentHostRoot, "Systems/PlayerControlGate");
                if (controlGateTransform != null)
                {
                    controlGate = controlGateTransform.GetComponent<PlayerControlGate>();
                }
            }

            if (controlGate == null && persistentPlayer != null)
            {
                controlGate = persistentPlayer.GetComponent<PlayerControlGate>();
            }

            controlGate?.ClearAllLocks();

            PlayerInputReader inputReader = playerInputReader;
            if (inputReader == null && persistentPlayer != null)
            {
                inputReader = persistentPlayer.GetComponent<PlayerInputReader>();
            }

            inputReader?.ClearInput();
            inputReader?.EnableGameplay();

            PlayerInput input = persistentPlayer != null ? persistentPlayer.GetComponent<PlayerInput>() : null;
            if (input != null)
            {
                input.enabled = true;
                input.ActivateInput();
            }

            PlayerStateMachine stateMachine = persistentPlayer != null ? persistentPlayer.GetComponent<PlayerStateMachine>() : null;
            stateMachine?.TransitionTo(PlayerStateId.Idle);
        }

        private bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            Transform root = other.transform.root;
            if (!string.IsNullOrWhiteSpace(requiredPlayerName) && root.name == requiredPlayerName)
            {
                return true;
            }

            return other.GetComponentInParent<PlayerInputReader>() != null
                || other.GetComponentInParent<PlayerStateMachine>() != null;
        }

        private static Transform FindRootTransform(Scene scene, string rootName)
        {
            if (string.IsNullOrWhiteSpace(rootName))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == rootName)
                {
                    return roots[i].transform;
                }
            }

            return null;
        }

        private static Transform FindChildPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            Transform current = root;
            int segmentStart = 0;
            for (int i = 0; i <= path.Length; i++)
            {
                if (i < path.Length && path[i] != '/')
                {
                    continue;
                }

                int segmentLength = i - segmentStart;
                if (segmentLength > 0)
                {
                    current = FindDirectChild(current, path, segmentStart, segmentLength);
                    if (current == null)
                    {
                        return null;
                    }
                }

                segmentStart = i + 1;
            }

            return current;
        }

        private static Transform FindDirectChild(Transform parent, string path, int segmentStart, int segmentLength)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Length == segmentLength
                    && string.Compare(child.name, 0, path, segmentStart, segmentLength, System.StringComparison.Ordinal) == 0)
                {
                    return child;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.35f);
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(0.25f, 0.75f, 1f, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
