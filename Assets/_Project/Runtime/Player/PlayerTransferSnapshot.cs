using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [Serializable]
    public struct PlayerTransferSnapshot
    {
        [SerializeField] private bool facingRight;
        [SerializeField] private PlayerStateId logicalState;
        [SerializeField] private Vector3 normalizedVelocity;
        [SerializeField] private string storyStateId;
        [SerializeField] private AnimatorFloatParameter[] floatParameters;
        [SerializeField] private AnimatorBoolParameter[] boolParameters;
        [SerializeField] private AnimatorIntParameter[] intParameters;

        public PlayerTransferSnapshot(
            bool facingRight,
            PlayerStateId logicalState,
            Vector3 normalizedVelocity,
            string storyStateId,
            AnimatorFloatParameter[] floatParameters,
            AnimatorBoolParameter[] boolParameters,
            AnimatorIntParameter[] intParameters)
        {
            this.facingRight = facingRight;
            this.logicalState = logicalState;
            this.normalizedVelocity = normalizedVelocity;
            this.storyStateId = storyStateId ?? string.Empty;
            this.floatParameters = floatParameters ?? Array.Empty<AnimatorFloatParameter>();
            this.boolParameters = boolParameters ?? Array.Empty<AnimatorBoolParameter>();
            this.intParameters = intParameters ?? Array.Empty<AnimatorIntParameter>();
        }

        public bool FacingRight => facingRight;
        public PlayerStateId LogicalState => logicalState;
        public Vector3 NormalizedVelocity => normalizedVelocity;
        public string StoryStateId => storyStateId;
        public IReadOnlyList<AnimatorFloatParameter> FloatParameters => floatParameters ?? Array.Empty<AnimatorFloatParameter>();
        public IReadOnlyList<AnimatorBoolParameter> BoolParameters => boolParameters ?? Array.Empty<AnimatorBoolParameter>();
        public IReadOnlyList<AnimatorIntParameter> IntParameters => intParameters ?? Array.Empty<AnimatorIntParameter>();

        public static PlayerTransferSnapshot Capture(GameObject sourceRoot, string presentationStoryState = "")
        {
            if (sourceRoot == null)
            {
                return default;
            }

            PlayerFacingController facing = sourceRoot.GetComponentInChildren<PlayerFacingController>(true);
            PlayerStateMachine stateMachine = sourceRoot.GetComponentInChildren<PlayerStateMachine>(true);
            Rigidbody body = sourceRoot.GetComponentInChildren<Rigidbody>(true);
            Animator animator = sourceRoot.GetComponentInChildren<Animator>(true);

            Vector3 velocity = body != null ? body.linearVelocity : Vector3.zero;
            Vector3 normalized = velocity.sqrMagnitude > 1f ? velocity.normalized : velocity;

            CaptureAnimatorParameters(
                animator,
                out AnimatorFloatParameter[] floats,
                out AnimatorBoolParameter[] bools,
                out AnimatorIntParameter[] ints);

            return new PlayerTransferSnapshot(
                facing == null || facing.FacingRight,
                stateMachine != null ? stateMachine.CurrentStateId : PlayerStateId.Idle,
                normalized,
                presentationStoryState,
                floats,
                bools,
                ints);
        }

        public void ApplyTo(GameObject targetRoot)
        {
            if (targetRoot == null)
            {
                return;
            }

            PlayerFacingController facing = targetRoot.GetComponentInChildren<PlayerFacingController>(true);
            if (facing != null)
            {
                if (facingRight)
                {
                    facing.FaceRight();
                }
                else
                {
                    facing.FaceLeft();
                }
            }

            PlayerStateMachine stateMachine = targetRoot.GetComponentInChildren<PlayerStateMachine>(true);
            if (stateMachine != null)
            {
                stateMachine.TransitionTo(logicalState);
            }

            Animator animator = targetRoot.GetComponentInChildren<Animator>(true);
            ApplyAnimatorParameters(animator);
        }

        private static void CaptureAnimatorParameters(
            Animator animator,
            out AnimatorFloatParameter[] floats,
            out AnimatorBoolParameter[] bools,
            out AnimatorIntParameter[] ints)
        {
            floats = Array.Empty<AnimatorFloatParameter>();
            bools = Array.Empty<AnimatorBoolParameter>();
            ints = Array.Empty<AnimatorIntParameter>();

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            List<AnimatorFloatParameter> floatValues = new List<AnimatorFloatParameter>();
            List<AnimatorBoolParameter> boolValues = new List<AnimatorBoolParameter>();
            List<AnimatorIntParameter> intValues = new List<AnimatorIntParameter>();

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        floatValues.Add(new AnimatorFloatParameter(parameter.name, animator.GetFloat(parameter.nameHash)));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        boolValues.Add(new AnimatorBoolParameter(parameter.name, animator.GetBool(parameter.nameHash)));
                        break;
                    case AnimatorControllerParameterType.Int:
                        intValues.Add(new AnimatorIntParameter(parameter.name, animator.GetInteger(parameter.nameHash)));
                        break;
                }
            }

            floats = floatValues.ToArray();
            bools = boolValues.ToArray();
            ints = intValues.ToArray();
        }

        private void ApplyAnimatorParameters(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            AnimatorFloatParameter[] floats = floatParameters ?? Array.Empty<AnimatorFloatParameter>();
            for (int i = 0; i < floats.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(floats[i].Name))
                {
                    animator.SetFloat(floats[i].Name, floats[i].Value);
                }
            }

            AnimatorBoolParameter[] bools = boolParameters ?? Array.Empty<AnimatorBoolParameter>();
            for (int i = 0; i < bools.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(bools[i].Name))
                {
                    animator.SetBool(bools[i].Name, bools[i].Value);
                }
            }

            AnimatorIntParameter[] ints = intParameters ?? Array.Empty<AnimatorIntParameter>();
            for (int i = 0; i < ints.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ints[i].Name))
                {
                    animator.SetInteger(ints[i].Name, ints[i].Value);
                }
            }
        }
    }

    [Serializable]
    public struct AnimatorFloatParameter
    {
        [SerializeField] private string name;
        [SerializeField] private float value;

        public AnimatorFloatParameter(string name, float value)
        {
            this.name = name ?? string.Empty;
            this.value = value;
        }

        public string Name => name;
        public float Value => value;
    }

    [Serializable]
    public struct AnimatorBoolParameter
    {
        [SerializeField] private string name;
        [SerializeField] private bool value;

        public AnimatorBoolParameter(string name, bool value)
        {
            this.name = name ?? string.Empty;
            this.value = value;
        }

        public string Name => name;
        public bool Value => value;
    }

    [Serializable]
    public struct AnimatorIntParameter
    {
        [SerializeField] private string name;
        [SerializeField] private int value;

        public AnimatorIntParameter(string name, int value)
        {
            this.name = name ?? string.Empty;
            this.value = value;
        }

        public string Name => name;
        public int Value => value;
    }
}
