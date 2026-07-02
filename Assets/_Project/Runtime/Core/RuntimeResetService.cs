using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class RuntimeResetService : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] initialResetTargets = Array.Empty<MonoBehaviour>();

        private readonly List<ResetEntry> resetEntries = new List<ResetEntry>();
        private readonly List<ResetEntry> resetExecutionBuffer = new List<ResetEntry>();
        private int nextRegistrationIndex;

        public int RegisteredCount => resetEntries.Count;

        private void Awake()
        {
            RegisterInitialTargets();
        }

        public bool Register(IRuntimeResettable resettable)
        {
            if (resettable == null || Contains(resettable))
            {
                return false;
            }

            resetEntries.Add(new ResetEntry(resettable, nextRegistrationIndex++));
            return true;
        }

        public bool Unregister(IRuntimeResettable resettable)
        {
            if (resettable == null)
            {
                return false;
            }

            for (int i = resetEntries.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(resetEntries[i].Resettable, resettable))
                {
                    resetEntries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void ExecuteReset()
        {
            resetExecutionBuffer.Clear();
            for (int i = 0; i < resetEntries.Count; i++)
            {
                IRuntimeResettable resettable = resetEntries[i].Resettable;
                if (IsStillValid(resettable))
                {
                    resetExecutionBuffer.Add(resetEntries[i]);
                }
            }

            resetExecutionBuffer.Sort(CompareResetEntries);
            for (int i = 0; i < resetExecutionBuffer.Count; i++)
            {
                resetExecutionBuffer[i].Resettable.RuntimeReset();
            }
        }

        private void RegisterInitialTargets()
        {
            for (int i = 0; i < initialResetTargets.Length; i++)
            {
                if (initialResetTargets[i] is IRuntimeResettable resettable)
                {
                    Register(resettable);
                }
            }
        }

        private bool Contains(IRuntimeResettable resettable)
        {
            for (int i = 0; i < resetEntries.Count; i++)
            {
                if (ReferenceEquals(resetEntries[i].Resettable, resettable))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareResetEntries(ResetEntry left, ResetEntry right)
        {
            int orderComparison = left.Resettable.ResetOrder.CompareTo(right.Resettable.ResetOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int typeComparison = string.CompareOrdinal(left.Resettable.GetType().FullName, right.Resettable.GetType().FullName);
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            int pathComparison = string.CompareOrdinal(GetHierarchyPath(left.Resettable), GetHierarchyPath(right.Resettable));
            if (pathComparison != 0)
            {
                return pathComparison;
            }

            return left.RegistrationIndex.CompareTo(right.RegistrationIndex);
        }

        private static string GetHierarchyPath(IRuntimeResettable resettable)
        {
            Component component = resettable as Component;
            if (component == null)
            {
                return string.Empty;
            }

            return BuildPath(component.transform);
        }

        private static string BuildPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            if (transform.parent == null)
            {
                return transform.name;
            }

            return BuildPath(transform.parent) + "/" + transform.name;
        }

        private static bool IsStillValid(IRuntimeResettable resettable)
        {
            if (resettable == null)
            {
                return false;
            }

            UnityEngine.Object unityObject = resettable as UnityEngine.Object;
            return ReferenceEquals(unityObject, null) || unityObject != null;
        }

        private readonly struct ResetEntry
        {
            public ResetEntry(IRuntimeResettable resettable, int registrationIndex)
            {
                Resettable = resettable;
                RegistrationIndex = registrationIndex;
            }

            public IRuntimeResettable Resettable { get; }
            public int RegistrationIndex { get; }
        }
    }
}
