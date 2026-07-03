using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class LevelCompletionTrigger : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private LevelDefinition nextLevel;
        [SerializeField] private AdditiveLevelLoader levelLoader;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private GameFlowState fallbackNextState = GameFlowState.Level02;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool fireOnce = true;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private int resetOrder = 15;

        private bool fired;

        public event Action<LevelCompletionTrigger> Completed;

        public int ResetOrder => resetOrder;
        public bool Fired => fired;

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

            TryComplete();
        }

        public bool TryComplete()
        {
            if (fireOnce && fired)
            {
                return false;
            }

            fired = true;
            Completed?.Invoke(this);

            if (nextLevel != null && levelLoader != null)
            {
                levelLoader.LoadLevel(nextLevel);
                return true;
            }

            gameFlowController?.TryTransitionTo(fallbackNextState);
            return true;
        }

        public void SetRuntimeServices(AdditiveLevelLoader loader, GameFlowController flowController, RuntimeResetService resetService)
        {
            runtimeResetService?.Unregister(this);
            levelLoader = loader;
            gameFlowController = flowController;
            runtimeResetService = resetService;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
            }
        }

        public void RuntimeReset()
        {
            fired = false;
        }
    }
}
