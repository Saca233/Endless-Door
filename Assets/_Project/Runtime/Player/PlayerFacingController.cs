using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PlayerFacingController : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool startsFacingRight = true;
        [SerializeField] private float facingDeadZone = 0.05f;

        private bool facingRight;

        public bool FacingRight => facingRight;
        public int FacingSign => facingRight ? 1 : -1;

        private void Awake()
        {
            facingRight = startsFacingRight;
            ApplyFacing();
        }

        public void UpdateFacing(float horizontalInput)
        {
            if (horizontalInput > facingDeadZone)
            {
                FaceRight();
            }
            else if (horizontalInput < -facingDeadZone)
            {
                FaceLeft();
            }
        }

        public void FaceRight()
        {
            if (facingRight)
            {
                return;
            }

            facingRight = true;
            ApplyFacing();
        }

        public void FaceLeft()
        {
            if (!facingRight)
            {
                return;
            }

            facingRight = false;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.Euler(0f, facingRight ? 0f : 180f, 0f);
        }
    }
}
