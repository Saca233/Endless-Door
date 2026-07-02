using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class StoryFlagService : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private int resetOrder = -10;

        private readonly Dictionary<string, bool> boolFlags = new Dictionary<string, bool>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> intValues = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> temporaryBoolFlags = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> temporaryIntValues = new HashSet<string>(StringComparer.Ordinal);

        public int ResetOrder => resetOrder;
        public int BoolFlagCount => boolFlags.Count;
        public int IntValueCount => intValues.Count;

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
        }

        public void SetRuntimeResetService(RuntimeResetService service)
        {
            if (runtimeResetService == service)
            {
                return;
            }

            runtimeResetService?.Unregister(this);
            runtimeResetService = service;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
            }
        }

        public void SetBool(string stableId, bool value, bool temporary = false)
        {
            if (!IsValidId(stableId))
            {
                return;
            }

            boolFlags[stableId] = value;
            TrackTemporary(stableId, temporary, temporaryBoolFlags);
        }

        public bool GetBool(string stableId, bool defaultValue = false)
        {
            return TryGetBool(stableId, out bool value) ? value : defaultValue;
        }

        public bool TryGetBool(string stableId, out bool value)
        {
            value = false;
            return IsValidId(stableId) && boolFlags.TryGetValue(stableId, out value);
        }

        public bool CheckBool(string stableId, bool expectedValue)
        {
            return TryGetBool(stableId, out bool value) && value == expectedValue;
        }

        public void SetInt(string stableId, int value, bool temporary = false)
        {
            if (!IsValidId(stableId))
            {
                return;
            }

            intValues[stableId] = value;
            TrackTemporary(stableId, temporary, temporaryIntValues);
        }

        public int GetInt(string stableId, int defaultValue = 0)
        {
            return TryGetInt(stableId, out int value) ? value : defaultValue;
        }

        public bool TryGetInt(string stableId, out int value)
        {
            value = 0;
            return IsValidId(stableId) && intValues.TryGetValue(stableId, out value);
        }

        public bool CheckInt(string stableId, int expectedValue)
        {
            return TryGetInt(stableId, out int value) && value == expectedValue;
        }

        public void ClearFlag(string stableId)
        {
            if (!IsValidId(stableId))
            {
                return;
            }

            boolFlags.Remove(stableId);
            intValues.Remove(stableId);
            temporaryBoolFlags.Remove(stableId);
            temporaryIntValues.Remove(stableId);
        }

        public void ResetTemporaryFlags()
        {
            foreach (string flagId in temporaryBoolFlags)
            {
                boolFlags.Remove(flagId);
            }

            foreach (string flagId in temporaryIntValues)
            {
                intValues.Remove(flagId);
            }

            temporaryBoolFlags.Clear();
            temporaryIntValues.Clear();
        }

        public void RuntimeReset()
        {
            ResetTemporaryFlags();
        }

        public static bool IsValidId(string stableId)
        {
            return !string.IsNullOrWhiteSpace(stableId);
        }

        private static void TrackTemporary(string stableId, bool temporary, HashSet<string> temporaryIds)
        {
            if (temporary)
            {
                temporaryIds.Add(stableId);
            }
            else
            {
                temporaryIds.Remove(stableId);
            }
        }
    }
}
