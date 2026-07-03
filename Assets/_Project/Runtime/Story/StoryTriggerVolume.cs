using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class StoryTriggerVolume : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private StorySequenceRunner sequenceRunner;
        [SerializeField] private StorySequence sequence;
        [SerializeField] private StoryFlagService storyFlagService;
        [SerializeField] private bool fireOncePerLoop = true;
        [SerializeField] private bool requireFlag;
        [SerializeField] private string requiredFlagId;
        [SerializeField] private bool requiredFlagValue = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private int resetOrder = 10;

        private bool firedThisLoop;

        public event Action<StoryTriggerVolume> Triggered;

        public int ResetOrder => resetOrder;
        public bool FiredThisLoop => firedThisLoop;

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(playerTag) && !other.CompareTag(playerTag))
            {
                return;
            }

            TryFire(other.gameObject);
        }

        public bool TryFire(GameObject actor)
        {
            if (fireOncePerLoop && firedThisLoop)
            {
                return false;
            }

            if (requireFlag && (storyFlagService == null || !storyFlagService.CheckBool(requiredFlagId, requiredFlagValue)))
            {
                return false;
            }

            firedThisLoop = true;
            if (sequenceRunner != null && sequence != null)
            {
                sequenceRunner.StartSequence(sequence);
            }

            Triggered?.Invoke(this);
            return true;
        }

        public void RuntimeReset()
        {
            firedThisLoop = false;
        }

        public void SetRuntimeResetService(RuntimeResetService service)
        {
            runtimeResetService?.Unregister(this);
            runtimeResetService = service;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
            }
        }

        public void SetRuntimeServices(StorySequenceRunner runner, StoryFlagService flagService, RuntimeResetService resetService)
        {
            runtimeResetService?.Unregister(this);
            sequenceRunner = runner;
            storyFlagService = flagService;
            runtimeResetService = resetService;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
            }
        }

        public void SetSequence(StorySequenceRunner runner, StorySequence storySequence)
        {
            sequenceRunner = runner;
            sequence = storySequence;
        }

        public void SetRequiredFlag(StoryFlagService flagService, string stableId, bool expectedValue)
        {
            storyFlagService = flagService;
            requiredFlagId = stableId;
            requiredFlagValue = expectedValue;
            requireFlag = true;
        }
    }
}
