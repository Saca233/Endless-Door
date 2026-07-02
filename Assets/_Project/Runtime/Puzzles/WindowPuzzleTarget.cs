using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class WindowPuzzleTarget : MonoBehaviour
    {
        [SerializeField] private PuzzleTargetId targetId = PuzzleTargetId.NewId();
        [SerializeField] private Renderer[] affectedRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private Collider[] affectedColliders = System.Array.Empty<Collider>();

        private readonly HashSet<string> activeEffectSources = new HashSet<string>();
        private bool[] initialRendererStates = System.Array.Empty<bool>();
        private bool[] initialColliderStates = System.Array.Empty<bool>();
        private bool statesCached;
        private bool hasLastKnownBounds;
        private Bounds lastKnownBounds;

        public PuzzleTargetId TargetId => targetId;
        public bool IsErased => activeEffectSources.Count > 0;
        public int ActiveSourceCount => activeEffectSources.Count;

        private void Reset()
        {
            RegenerateId();
            affectedRenderers = GetComponentsInChildren<Renderer>(true);
            affectedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Awake()
        {
            CacheInitialStatesIfNeeded();
            UpdateLastKnownBounds();
        }

        private void OnValidate()
        {
            if (!targetId.IsValid)
            {
                RegenerateId();
            }
        }

        public void RegenerateId()
        {
            targetId = PuzzleTargetId.NewId();
        }

        public void SetAffectedObjects(Renderer[] renderers, Collider[] colliders)
        {
            affectedRenderers = renderers ?? System.Array.Empty<Renderer>();
            affectedColliders = colliders ?? System.Array.Empty<Collider>();
            statesCached = false;
            CacheInitialStatesIfNeeded();
            UpdateLastKnownBounds();
        }

        public bool ApplyEffect(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            CacheInitialStatesIfNeeded();
            UpdateLastKnownBounds();
            bool added = activeEffectSources.Add(sourceId);
            ApplyCurrentState();
            return added;
        }

        public bool ReleaseEffect(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return false;
            }

            bool removed = activeEffectSources.Remove(sourceId);
            ApplyCurrentState();
            return removed;
        }

        public void RuntimeReset()
        {
            activeEffectSources.Clear();
            RestoreInitialStates();
            UpdateLastKnownBounds();
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (!IsErased && UpdateLastKnownBounds())
            {
                bounds = lastKnownBounds;
                return true;
            }

            bounds = lastKnownBounds;
            return hasLastKnownBounds;
        }

        private void CacheInitialStatesIfNeeded()
        {
            if (statesCached)
            {
                return;
            }

            initialRendererStates = new bool[affectedRenderers.Length];
            for (int i = 0; i < affectedRenderers.Length; i++)
            {
                initialRendererStates[i] = affectedRenderers[i] != null && affectedRenderers[i].enabled;
            }

            initialColliderStates = new bool[affectedColliders.Length];
            for (int i = 0; i < affectedColliders.Length; i++)
            {
                initialColliderStates[i] = affectedColliders[i] != null && affectedColliders[i].enabled;
            }

            statesCached = true;
        }

        private void ApplyCurrentState()
        {
            CacheInitialStatesIfNeeded();
            if (activeEffectSources.Count > 0)
            {
                SetAffectedEnabled(false);
            }
            else
            {
                RestoreInitialStates();
            }
        }

        private void SetAffectedEnabled(bool enabled)
        {
            for (int i = 0; i < affectedRenderers.Length; i++)
            {
                if (affectedRenderers[i] != null)
                {
                    affectedRenderers[i].enabled = enabled;
                }
            }

            for (int i = 0; i < affectedColliders.Length; i++)
            {
                if (affectedColliders[i] != null)
                {
                    affectedColliders[i].enabled = enabled;
                }
            }
        }

        private void RestoreInitialStates()
        {
            CacheInitialStatesIfNeeded();
            for (int i = 0; i < affectedRenderers.Length; i++)
            {
                if (affectedRenderers[i] != null && i < initialRendererStates.Length)
                {
                    affectedRenderers[i].enabled = initialRendererStates[i];
                }
            }

            for (int i = 0; i < affectedColliders.Length; i++)
            {
                if (affectedColliders[i] != null && i < initialColliderStates.Length)
                {
                    affectedColliders[i].enabled = initialColliderStates[i];
                }
            }
        }

        private bool UpdateLastKnownBounds()
        {
            bool hasBounds = false;
            Bounds combinedBounds = default;

            for (int i = 0; i < affectedRenderers.Length; i++)
            {
                Renderer renderer = affectedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            for (int i = 0; i < affectedColliders.Length; i++)
            {
                Collider collider = affectedColliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(collider.bounds);
                }
            }

            if (hasBounds)
            {
                lastKnownBounds = combinedBounds;
                hasLastKnownBounds = true;
            }

            return hasBounds;
        }
    }
}
