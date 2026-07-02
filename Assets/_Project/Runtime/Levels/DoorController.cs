using UnityEngine;
using UnityEngine.Events;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DoorController : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private DoorState initialState = DoorState.Closed;
        [SerializeField] private bool completeOpeningImmediately = true;
        [SerializeField] private StorySequenceRunner sequenceRunner;
        [SerializeField] private StorySequence sequenceOnTrigger;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private int resetOrder = 30;
        [SerializeField] private UnityEvent locked = new UnityEvent();
        [SerializeField] private UnityEvent opening = new UnityEvent();
        [SerializeField] private UnityEvent opened = new UnityEvent();
        [SerializeField] private UnityEvent closed = new UnityEvent();

        public int ResetOrder => resetOrder;
        public DoorState State { get; private set; }
        public bool IsLocked => State == DoorState.Locked;
        public UnityEvent Locked => locked;
        public UnityEvent Opening => opening;
        public UnityEvent Opened => opened;
        public UnityEvent Closed => closed;

        private void Awake()
        {
            State = initialState;
        }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        public bool TriggerDoor()
        {
            if (sequenceRunner != null && sequenceOnTrigger != null)
            {
                sequenceRunner.StartSequence(sequenceOnTrigger);
            }

            return Open();
        }

        public bool Open()
        {
            if (State == DoorState.Locked)
            {
                locked.Invoke();
                return false;
            }

            if (State == DoorState.Open || State == DoorState.Opening)
            {
                return true;
            }

            State = DoorState.Opening;
            opening.Invoke();
            if (completeOpeningImmediately)
            {
                CompleteOpening();
            }

            return true;
        }

        public void CompleteOpening()
        {
            if (State != DoorState.Opening)
            {
                return;
            }

            State = DoorState.Open;
            opened.Invoke();
        }

        public void Close()
        {
            if (State == DoorState.Closed)
            {
                return;
            }

            State = DoorState.Closed;
            closed.Invoke();
        }

        public void Lock()
        {
            State = DoorState.Locked;
        }

        public void Unlock()
        {
            if (State == DoorState.Locked)
            {
                State = DoorState.Closed;
            }
        }

        public void RuntimeReset()
        {
            State = initialState;
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

        public void SetSequence(StorySequenceRunner runner, StorySequence sequence)
        {
            sequenceRunner = runner;
            sequenceOnTrigger = sequence;
        }
    }
}
