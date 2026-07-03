using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class ErrorWindowPool : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private ErrorWindowController windowPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private ErrorWindowController[] preloadedWindows = Array.Empty<ErrorWindowController>();
        [SerializeField] private WindowLayoutDefinition[] initialLayouts = Array.Empty<WindowLayoutDefinition>();
        [SerializeField] private ProjectedWindowBarrierRule[] barrierRules = Array.Empty<ProjectedWindowBarrierRule>();
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private bool activateOnEnable;
        [SerializeField] private int resetOrder = 35;

        private readonly List<ErrorWindowController> allWindows = new List<ErrorWindowController>();
        private readonly List<ErrorWindowController> activeWindows = new List<ErrorWindowController>();

        public int ResetOrder => resetOrder;
        public int ActiveCount => activeWindows.Count;
        public int PooledCount => allWindows.Count;

        private void Awake()
        {
            SeedPreloadedWindows();
        }

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
            if (activateOnEnable)
            {
                ActivateInitialLayouts();
            }
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
            ReturnAll();
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

        public void SetWindows(ErrorWindowController[] windows)
        {
            ReturnAll();
            preloadedWindows = windows ?? Array.Empty<ErrorWindowController>();
            allWindows.Clear();
            SeedPreloadedWindows();
        }

        public void SetBarrierRules(ProjectedWindowBarrierRule[] rules)
        {
            barrierRules = rules ?? Array.Empty<ProjectedWindowBarrierRule>();
        }

        public void SetInitialLayouts(WindowLayoutDefinition[] layouts)
        {
            initialLayouts = layouts ?? Array.Empty<WindowLayoutDefinition>();
        }

        public void ActivateInitialLayouts()
        {
            ActivateLayouts(initialLayouts);
        }

        public void ActivateLayouts(WindowLayoutDefinition[] layouts)
        {
            ReturnAll();
            if (layouts == null)
            {
                return;
            }

            Array.Sort(layouts, CompareLayouts);
            for (int i = 0; i < layouts.Length; i++)
            {
                ErrorWindowController window = GetAvailableWindow();
                if (window == null)
                {
                    continue;
                }

                window.Activate(layouts[i]);
                activeWindows.Add(window);

                ProjectedWindowBarrierRule rule = GetRuleForLayout(layouts[i], i);
                if (rule != null)
                {
                    rule.SetErrorWindow(window);
                    rule.SetRuleEnabled(true);
                    rule.EvaluateNow(true);
                }
            }
        }

        public void ReturnAll()
        {
            for (int i = 0; i < barrierRules.Length; i++)
            {
                if (barrierRules[i] != null)
                {
                    barrierRules[i].SetErrorWindow(null);
                    barrierRules[i].RuntimeReset();
                }
            }

            for (int i = 0; i < allWindows.Count; i++)
            {
                if (allWindows[i] != null)
                {
                    allWindows[i].Deactivate();
                }
            }

            activeWindows.Clear();
        }

        public void RuntimeReset()
        {
            ReturnAll();
        }

        private void SeedPreloadedWindows()
        {
            for (int i = 0; i < preloadedWindows.Length; i++)
            {
                if (preloadedWindows[i] != null && !allWindows.Contains(preloadedWindows[i]))
                {
                    allWindows.Add(preloadedWindows[i]);
                    preloadedWindows[i].Deactivate();
                }
            }
        }

        private ErrorWindowController GetAvailableWindow()
        {
            for (int i = 0; i < allWindows.Count; i++)
            {
                if (allWindows[i] != null && !activeWindows.Contains(allWindows[i]))
                {
                    return allWindows[i];
                }
            }

            if (windowPrefab == null)
            {
                return null;
            }

            Transform parent = poolRoot != null ? poolRoot : transform;
            ErrorWindowController created = Instantiate(windowPrefab, parent);
            created.Deactivate();
            allWindows.Add(created);
            return created;
        }

        private ProjectedWindowBarrierRule GetRuleForLayout(WindowLayoutDefinition layout, int index)
        {
            if (layout != null && layout.AssociatedPuzzleRule != null)
            {
                return layout.AssociatedPuzzleRule;
            }

            if (index >= 0 && index < barrierRules.Length)
            {
                return barrierRules[index];
            }

            return null;
        }

        private static int CompareLayouts(WindowLayoutDefinition left, WindowLayoutDefinition right)
        {
            int leftOrder = left != null ? left.InitialFocusOrder : 0;
            int rightOrder = right != null ? right.InitialFocusOrder : 0;
            return leftOrder.CompareTo(rightOrder);
        }
    }
}
