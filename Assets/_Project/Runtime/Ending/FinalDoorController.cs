using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class FinalDoorController : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform placeholderGeometry;
        [SerializeField] private Transform pullOrigin;
        [SerializeField] private bool startsOpen;
        [SerializeField] private string openTrigger = "Open";
        [SerializeField] private string closeTrigger = "Close";
        [SerializeField] private string openBool = "Open";
        [SerializeField] private Vector3 closedScale = Vector3.one;
        [SerializeField] private Vector3 openScale = new Vector3(1f, 1f, 0.18f);
        [SerializeField] private int resetOrder = 30;
        [SerializeField] private UnityEvent opened = new UnityEvent();
        [SerializeField] private UnityEvent closed = new UnityEvent();

        public int ResetOrder => resetOrder;
        public bool IsOpen { get; private set; }
        public Transform PullOrigin => pullOrigin != null ? pullOrigin : transform;
        public UnityEvent Opened => opened;
        public UnityEvent Closed => closed;

        private void Awake()
        {
            IsOpen = startsOpen;
            ApplyPresentation(false);
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            ApplyPresentation(true);
            opened.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            ApplyPresentation(true);
            closed.Invoke();
        }

        public void RuntimeReset()
        {
            IsOpen = startsOpen;
            ApplyPresentation(false);
        }

        private void ApplyPresentation(bool sendAnimatorTriggers)
        {
            if (animator != null)
            {
                if (!string.IsNullOrWhiteSpace(openBool))
                {
                    animator.SetBool(openBool, IsOpen);
                }

                if (sendAnimatorTriggers)
                {
                    string triggerName = IsOpen ? openTrigger : closeTrigger;
                    if (!string.IsNullOrWhiteSpace(triggerName))
                    {
                        animator.SetTrigger(triggerName);
                    }
                }
            }

            if (placeholderGeometry != null)
            {
                placeholderGeometry.localScale = IsOpen ? openScale : closedScale;
            }
        }
    }
}
