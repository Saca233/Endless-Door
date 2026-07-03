using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class LoopStateService : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField, Min(0)] private int initialLoopCount;
        [SerializeField] private int resetOrder = -200;

        public int ResetOrder => resetOrder;
        public int LoopCount { get; private set; }

        private void Awake()
        {
            LoopCount = Mathf.Max(0, initialLoopCount);
        }

        public int IncrementLoop()
        {
            LoopCount++;
            return LoopCount;
        }

        public void SetLoopCount(int count)
        {
            LoopCount = Mathf.Max(0, count);
        }

        public void ClearLoopCount()
        {
            LoopCount = 0;
        }

        public void RuntimeReset()
        {
            // Loop count is meta-state and intentionally survives transient resets.
        }
    }
}
