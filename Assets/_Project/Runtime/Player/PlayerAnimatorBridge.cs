using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string horizontalSpeedParameter = "HorizontalSpeed";
        [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
        [SerializeField] private string groundedParameter = "Grounded";
        [SerializeField] private string stateParameter = "State";

        private int horizontalSpeedHash;
        private int verticalSpeedHash;
        private int groundedHash;
        private int stateHash;
        private bool hasHorizontalSpeed;
        private bool hasVerticalSpeed;
        private bool hasGrounded;
        private bool hasState;

        private void Awake()
        {
            CacheParameters();
        }

        private void OnValidate()
        {
            CacheHashes();
        }

        public void Apply(float horizontalSpeed, float verticalSpeed, bool isGrounded, PlayerStateId state)
        {
            if (animator == null)
            {
                return;
            }

            if (hasHorizontalSpeed)
            {
                animator.SetFloat(horizontalSpeedHash, Mathf.Abs(horizontalSpeed));
            }

            if (hasVerticalSpeed)
            {
                animator.SetFloat(verticalSpeedHash, verticalSpeed);
            }

            if (hasGrounded)
            {
                animator.SetBool(groundedHash, isGrounded);
            }

            if (hasState)
            {
                animator.SetInteger(stateHash, (int)state);
            }
        }

        private void CacheParameters()
        {
            CacheHashes();
            hasHorizontalSpeed = false;
            hasVerticalSpeed = false;
            hasGrounded = false;
            hasState = false;

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                int nameHash = parameters[i].nameHash;
                hasHorizontalSpeed |= nameHash == horizontalSpeedHash;
                hasVerticalSpeed |= nameHash == verticalSpeedHash;
                hasGrounded |= nameHash == groundedHash;
                hasState |= nameHash == stateHash;
            }
        }

        private void CacheHashes()
        {
            horizontalSpeedHash = Animator.StringToHash(horizontalSpeedParameter);
            verticalSpeedHash = Animator.StringToHash(verticalSpeedParameter);
            groundedHash = Animator.StringToHash(groundedParameter);
            stateHash = Animator.StringToHash(stateParameter);
        }
    }
}
