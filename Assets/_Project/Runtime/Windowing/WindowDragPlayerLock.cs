using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class WindowDragPlayerLock : MonoBehaviour
    {
        [SerializeField] private DesktopWindowManager windowManager;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private string lockOwnerName = "DesktopWindowDrag";

        private PlayerControlLockToken activeLock;

        private void OnEnable()
        {
            Subscribe();
            if (windowManager != null && windowManager.AnyWindowCurrentlyDragging)
            {
                AcquireLockIfNeeded();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseLockIfNeeded();
        }

        private void OnDestroy()
        {
            ReleaseLockIfNeeded();
        }

        private void Subscribe()
        {
            if (windowManager == null)
            {
                return;
            }

            windowManager.WindowDragStarted += OnWindowDragStarted;
            windowManager.WindowDragEnded += OnWindowDragEnded;
        }

        private void Unsubscribe()
        {
            if (windowManager == null)
            {
                return;
            }

            windowManager.WindowDragStarted -= OnWindowDragStarted;
            windowManager.WindowDragEnded -= OnWindowDragEnded;
        }

        private void OnWindowDragStarted(DesktopWindowController window)
        {
            AcquireLockIfNeeded();
        }

        private void OnWindowDragEnded(DesktopWindowController window)
        {
            if (windowManager == null || !windowManager.AnyWindowCurrentlyDragging)
            {
                ReleaseLockIfNeeded();
            }
        }

        private void AcquireLockIfNeeded()
        {
            if (playerControlGate == null || activeLock.IsValid)
            {
                return;
            }

            activeLock = playerControlGate.AcquireLock(lockOwnerName);
        }

        private void ReleaseLockIfNeeded()
        {
            if (playerControlGate == null || !activeLock.IsValid)
            {
                activeLock = default;
                return;
            }

            playerControlGate.ReleaseLock(activeLock);
            activeLock = default;
        }
    }
}
