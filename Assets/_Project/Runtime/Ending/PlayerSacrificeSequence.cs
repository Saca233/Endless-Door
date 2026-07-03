using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PlayerSacrificeSequence : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform[] movementMarkers;
        [SerializeField] private PlayerControlGate playerControlGate;
        [SerializeField] private StorySequenceRunner storySequenceRunner;
        [SerializeField] private StorySequence farewellDialogue;
        [SerializeField] private FinalDoorController finalDoor;
        [SerializeField] private ScreenFadeView fadeView;
        [SerializeField, Min(0.01f)] private float moveSpeed = 2.6f;
        [SerializeField] private Vector3 screenFacingEuler = new Vector3(0f, 180f, 0f);
        [SerializeField, Min(0f)] private float fadeToBlackDuration = 1.2f;
        [SerializeField] private int resetOrder = -35;
        [SerializeField] private UnityEvent heavyDoorClosed = new UnityEvent();

        private Coroutine activeRoutine;
        private PlayerControlLockToken lockToken;

        public int ResetOrder => resetOrder;
        public bool IsRunning => activeRoutine != null;
        public UnityEvent HeavyDoorClosed => heavyDoorClosed;

        private void OnDisable()
        {
            Cancel();
        }

        public bool Play()
        {
            if (activeRoutine != null)
            {
                return false;
            }

            activeRoutine = StartCoroutine(RunSequence());
            return true;
        }

        public void Cancel()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            ReleaseLock();
        }

        public void RuntimeReset()
        {
            Cancel();
        }

        private IEnumerator RunSequence()
        {
            AcquireLock();

            if (movementMarkers != null)
            {
                for (int i = 0; i < movementMarkers.Length; i++)
                {
                    yield return MovePlayerTo(movementMarkers[i]);
                }
            }

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(screenFacingEuler);
            }

            if (storySequenceRunner != null && farewellDialogue != null && storySequenceRunner.StartSequence(farewellDialogue))
            {
                while (storySequenceRunner.IsRunning)
                {
                    yield return null;
                }
            }

            if (finalDoor != null)
            {
                yield return MovePlayerTo(finalDoor.PullOrigin);
                finalDoor.Close();
            }

            heavyDoorClosed.Invoke();

            if (fadeView != null)
            {
                yield return fadeView.FadeOut(fadeToBlackDuration);
            }

            ReleaseLock();
            activeRoutine = null;
        }

        private IEnumerator MovePlayerTo(Transform marker)
        {
            if (playerRoot == null || marker == null)
            {
                yield break;
            }

            while (Vector3.Distance(playerRoot.position, marker.position) > 0.02f)
            {
                playerRoot.position = Vector3.MoveTowards(playerRoot.position, marker.position, moveSpeed * Time.deltaTime);
                yield return null;
            }

            playerRoot.SetPositionAndRotation(marker.position, playerRoot.rotation);
        }

        private void AcquireLock()
        {
            if (playerControlGate == null || lockToken.IsValid)
            {
                return;
            }

            lockToken = playerControlGate.AcquireLock("PlayerSacrificeSequence");
        }

        private void ReleaseLock()
        {
            if (playerControlGate != null && lockToken.IsValid)
            {
                playerControlGate.ReleaseLock(lockToken);
            }

            lockToken = default;
        }
    }
}
