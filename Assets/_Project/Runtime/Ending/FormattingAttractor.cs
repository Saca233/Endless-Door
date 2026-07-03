using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class FormattingAttractor : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private FinalDoorController finalDoor;
        [SerializeField] private DesktopDataObject[] initialObjects;
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private bool beginPullOnEnable;
        [SerializeField] private int resetOrder = 35;

        private readonly List<DesktopDataObject> registeredObjects = new List<DesktopDataObject>();
        private readonly List<DesktopDataObject> activeObjects = new List<DesktopDataObject>();

        public int ResetOrder => resetOrder;
        public bool IsPaused { get; private set; }
        public bool IsPulling => activeObjects.Count > 0;
        public int RegisteredCount => registeredObjects.Count;

        private void Awake()
        {
            SeedInitialObjects();
        }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
            if (beginPullOnEnable)
            {
                BeginPull();
            }
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        private void Update()
        {
            if (IsPaused || activeObjects.Count == 0)
            {
                return;
            }

            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                DesktopDataObject dataObject = activeObjects[i];
                if (dataObject == null || !dataObject.StepPull(Time.deltaTime))
                {
                    activeObjects.RemoveAt(i);
                }
            }
        }

        public void SetFinalDoor(FinalDoorController door)
        {
            finalDoor = door;
        }

        public bool Register(DesktopDataObject dataObject)
        {
            if (dataObject == null || registeredObjects.Contains(dataObject))
            {
                return false;
            }

            registeredObjects.Add(dataObject);
            return true;
        }

        public bool Unregister(DesktopDataObject dataObject)
        {
            activeObjects.Remove(dataObject);
            return registeredObjects.Remove(dataObject);
        }

        public void BeginPull()
        {
            activeObjects.Clear();
            Transform target = finalDoor != null ? finalDoor.PullOrigin : transform;
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                DesktopDataObject dataObject = registeredObjects[i];
                if (dataObject == null)
                {
                    continue;
                }

                dataObject.BeginPull(target);
                activeObjects.Add(dataObject);
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        public void RuntimeReset()
        {
            IsPaused = false;
            activeObjects.Clear();
            for (int i = 0; i < registeredObjects.Count; i++)
            {
                registeredObjects[i]?.RuntimeReset();
            }
        }

        private void SeedInitialObjects()
        {
            if (initialObjects == null)
            {
                return;
            }

            for (int i = 0; i < initialObjects.Length; i++)
            {
                Register(initialObjects[i]);
            }
        }
    }
}
