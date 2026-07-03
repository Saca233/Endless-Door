using UnityEngine;
using UnityEngine.InputSystem;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PlayerRepresentationTransfer : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private GameObject windowRepresentation;
        [SerializeField] private GameObject desktopRepresentation;
        [SerializeField] private Transform windowSpawnPoint;
        [SerializeField] private Transform desktopSpawnPoint;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private bool startInWindowRepresentation = true;
        [SerializeField] private string transferStoryStateId;
        [SerializeField] private int resetOrder = -60;

        private PlayerTransferSnapshot lastSnapshot;
        private bool hasSnapshot;

        public int ResetOrder => resetOrder;
        public PlayerTransferSnapshot LastSnapshot => lastSnapshot;
        public bool HasSnapshot => hasSnapshot;
        public bool IsDesktopRepresentationActive { get; private set; }
        public GameObject ActiveRepresentation => IsDesktopRepresentationActive ? desktopRepresentation : windowRepresentation;

        private void Awake()
        {
            ApplyInitialMode();
        }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        public void SetRepresentations(GameObject windowPlayer, GameObject desktopPlayer)
        {
            windowRepresentation = windowPlayer;
            desktopRepresentation = desktopPlayer;
        }

        public void SetSpawnPoints(Transform windowSpawn, Transform desktopSpawn)
        {
            windowSpawnPoint = windowSpawn;
            desktopSpawnPoint = desktopSpawn;
        }

        public PlayerTransferSnapshot CaptureWindowSnapshot()
        {
            lastSnapshot = PlayerTransferSnapshot.Capture(windowRepresentation, transferStoryStateId);
            hasSnapshot = windowRepresentation != null;
            return lastSnapshot;
        }

        public bool TransferToDesktop()
        {
            if (desktopRepresentation == null)
            {
                return false;
            }

            CaptureWindowSnapshot();

            if (desktopSpawnPoint != null)
            {
                desktopRepresentation.transform.SetPositionAndRotation(desktopSpawnPoint.position, desktopSpawnPoint.rotation);
            }

            SetRepresentationActive(windowRepresentation, false, false);
            SetRepresentationActive(desktopRepresentation, true, true);
            lastSnapshot.ApplyTo(desktopRepresentation);
            IsDesktopRepresentationActive = true;
            return true;
        }

        public void ResetToWindowRepresentation()
        {
            if (windowRepresentation != null && windowSpawnPoint != null)
            {
                windowRepresentation.transform.SetPositionAndRotation(windowSpawnPoint.position, windowSpawnPoint.rotation);
            }

            SetRepresentationActive(desktopRepresentation, false, false);
            SetRepresentationActive(windowRepresentation, true, true);
            IsDesktopRepresentationActive = false;
            hasSnapshot = false;
            lastSnapshot = default;
        }

        public int CountActiveInputReceivers()
        {
            return CountActiveInputReceivers(windowRepresentation) + CountActiveInputReceivers(desktopRepresentation);
        }

        public void RuntimeReset()
        {
            ResetToWindowRepresentation();
        }

        private void ApplyInitialMode()
        {
            if (startInWindowRepresentation)
            {
                ResetToWindowRepresentation();
            }
            else
            {
                SetRepresentationActive(windowRepresentation, false, false);
                SetRepresentationActive(desktopRepresentation, true, true);
                IsDesktopRepresentationActive = true;
            }
        }

        private static int CountActiveInputReceivers(GameObject root)
        {
            if (root == null || !root.activeInHierarchy)
            {
                return 0;
            }

            PlayerInputReader[] readers = root.GetComponentsInChildren<PlayerInputReader>(true);
            for (int i = 0; i < readers.Length; i++)
            {
                if (readers[i] != null && readers[i].enabled && readers[i].gameObject.activeInHierarchy)
                {
                    return 1;
                }
            }

            PlayerInput[] playerInputs = root.GetComponentsInChildren<PlayerInput>(true);
            for (int i = 0; i < playerInputs.Length; i++)
            {
                if (playerInputs[i] != null && playerInputs[i].enabled && playerInputs[i].gameObject.activeInHierarchy)
                {
                    return 1;
                }
            }

            return 0;
        }

        private static void SetRepresentationActive(GameObject root, bool active, bool inputEnabled)
        {
            if (root == null)
            {
                return;
            }

            root.SetActive(active);

            PlayerInputReader[] readers = root.GetComponentsInChildren<PlayerInputReader>(true);
            for (int i = 0; i < readers.Length; i++)
            {
                if (readers[i] == null)
                {
                    continue;
                }

                if (inputEnabled)
                {
                    readers[i].enabled = true;
                    readers[i].EnableGameplay();
                }
                else
                {
                    readers[i].DisableGameplay();
                    readers[i].enabled = false;
                }
            }

            PlayerInput[] playerInputs = root.GetComponentsInChildren<PlayerInput>(true);
            for (int i = 0; i < playerInputs.Length; i++)
            {
                if (playerInputs[i] != null)
                {
                    playerInputs[i].enabled = inputEnabled;
                }
            }

            Rigidbody body = root.GetComponentInChildren<Rigidbody>(true);
            if (body != null && !inputEnabled)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}
