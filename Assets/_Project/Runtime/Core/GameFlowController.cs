using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameFlowState initialState = GameFlowState.Boot;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private bool persistAcrossSceneLoads = true;

        public event Action<GameFlowState, GameFlowState> StateChanged;

        public GameFlowState CurrentState { get; private set; }
        public int LoopCount { get; private set; }

        private void Awake()
        {
            CurrentState = initialState;
            if (persistAcrossSceneLoads)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }

        public bool TryTransitionTo(GameFlowState nextState)
        {
            if (!GameFlowTransitionRules.IsValid(CurrentState, nextState))
            {
                return false;
            }

            SetState(nextState);
            return true;
        }

        public bool CanTransitionTo(GameFlowState nextState)
        {
            return GameFlowTransitionRules.IsValid(CurrentState, nextState);
        }

        public void ResetToLevel01()
        {
            runtimeResetService?.ExecuteReset();
            LoopCount++;
            SetState(GameFlowState.Level01);
        }

        public void EnterStaticEnding()
        {
            SetState(GameFlowState.StaticEnding);
        }

        public void SetRuntimeResetService(RuntimeResetService service)
        {
            runtimeResetService = service;
        }

        private void SetState(GameFlowState nextState)
        {
            GameFlowState previousState = CurrentState;
            CurrentState = nextState;
            if (previousState != nextState)
            {
                StateChanged?.Invoke(previousState, nextState);
            }
        }
    }
}
