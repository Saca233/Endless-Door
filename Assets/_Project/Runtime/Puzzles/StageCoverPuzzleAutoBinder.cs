using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class StageCoverPuzzleAutoBinder : MonoBehaviour
    {
        [SerializeField] private Transform stageRoot;
        [SerializeField] private string utilityWindowName = "UtilityWindow";
        [SerializeField] private string coverRuleName = "CoverToEraseRule";
        [SerializeField] private bool bindOnStart = true;
        [SerializeField] private bool createRuleIfMissing = true;
        [SerializeField] private bool includeInactiveTargets;

        private readonly List<WindowPuzzleTarget> stageTargets = new List<WindowPuzzleTarget>();
        private readonly List<CoverToEraseRule> coverRules = new List<CoverToEraseRule>();
        private readonly List<DesktopWindowController> windows = new List<DesktopWindowController>();
        private readonly List<GameWindowCamera> gameWindowCameras = new List<GameWindowCamera>();
        private readonly List<GameWindowView> gameWindowViews = new List<GameWindowView>();

        private CoverToEraseRule boundRule;

        private Transform StageRoot => stageRoot != null ? stageRoot : transform;

        private void Start()
        {
            if (bindOnStart)
            {
                BindNow();
            }
        }

        private void OnDisable()
        {
            if (boundRule != null)
            {
                boundRule.SetTargets(System.Array.Empty<WindowPuzzleTarget>());
                boundRule = null;
            }
        }

        public void BindNow()
        {
            CollectStageTargets();
            CoverToEraseRule rule = ResolveCoverRule();
            DesktopWindowController utilityWindow = ResolveUtilityWindow();
            GameWindowCamera gameWindowCamera = ResolveGameWindowCamera();
            GameWindowView gameWindowView = ResolveGameWindowView();

            if (rule == null || utilityWindow == null || gameWindowCamera == null || gameWindowView == null)
            {
                Debug.LogWarning("[StageCoverPuzzleAutoBinder] Could not bind stage cover puzzle. Check UtilityWindow, CoverToEraseRule, GameWindowCamera, and GameWindowView.");
                return;
            }

            RectTransform coveringRect = ResolveCoveringRect(utilityWindow);
            rule.ConfigureReferences(utilityWindow, coveringRect, gameWindowCamera, gameWindowView);
            rule.SetTargets(stageTargets.ToArray());
            rule.SetRuleEnabled(true);
            rule.EvaluateNow(true);
            boundRule = rule;
        }

        private void CollectStageTargets()
        {
            stageTargets.Clear();
            Transform root = StageRoot;
            WindowPuzzleTarget[] targets = root.GetComponentsInChildren<WindowPuzzleTarget>(includeInactiveTargets);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    stageTargets.Add(targets[i]);
                }
            }
        }

        private CoverToEraseRule ResolveCoverRule()
        {
            coverRules.Clear();
            CollectLoadedComponents(coverRules);

            CoverToEraseRule fallback = null;
            for (int i = 0; i < coverRules.Count; i++)
            {
                CoverToEraseRule rule = coverRules[i];
                if (rule == null || IsInStageRoot(rule.transform))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = rule;
                }
                if (rule.name == coverRuleName)
                {
                    return rule;
                }
            }

            if (fallback != null || !createRuleIfMissing)
            {
                return fallback;
            }

            GameObject ruleObject = new GameObject(coverRuleName);
            ruleObject.transform.SetParent(StageRoot, false);
            return ruleObject.AddComponent<CoverToEraseRule>();
        }

        private DesktopWindowController ResolveUtilityWindow()
        {
            windows.Clear();
            CollectLoadedComponents(windows);

            DesktopWindowController fallback = null;
            for (int i = 0; i < windows.Count; i++)
            {
                DesktopWindowController window = windows[i];
                if (window == null || IsInStageRoot(window.transform))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = window;
                }
                if (window.name == utilityWindowName)
                {
                    return window;
                }
            }

            return fallback;
        }

        private GameWindowCamera ResolveGameWindowCamera()
        {
            gameWindowCameras.Clear();
            CollectLoadedComponents(gameWindowCameras);
            for (int i = 0; i < gameWindowCameras.Count; i++)
            {
                GameWindowCamera camera = gameWindowCameras[i];
                if (camera != null && !IsInStageRoot(camera.transform))
                {
                    return camera;
                }
            }

            return null;
        }

        private GameWindowView ResolveGameWindowView()
        {
            gameWindowViews.Clear();
            CollectLoadedComponents(gameWindowViews);
            for (int i = 0; i < gameWindowViews.Count; i++)
            {
                GameWindowView view = gameWindowViews[i];
                if (view != null && !IsInStageRoot(view.transform))
                {
                    return view;
                }
            }

            return null;
        }

        private RectTransform ResolveCoveringRect(DesktopWindowController utilityWindow)
        {
            RawImage rawImage = utilityWindow.GetComponentInChildren<RawImage>(true);
            if (rawImage != null)
            {
                return rawImage.rectTransform;
            }

            return utilityWindow.View != null && utilityWindow.View.ContentRoot != null
                ? utilityWindow.View.ContentRoot
                : utilityWindow.transform as RectTransform;
        }

        private bool IsInStageRoot(Transform target)
        {
            Transform root = StageRoot;
            return target != null && root != null && (target == root || target.IsChildOf(root));
        }

        private static void CollectLoadedComponents<T>(List<T> results) where T : Component
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    T[] components = roots[rootIndex].GetComponentsInChildren<T>(true);
                    for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                    {
                        results.Add(components[componentIndex]);
                    }
                }
            }

            Scene persistentScene = GetDontDestroyOnLoadScene();
            if (!persistentScene.IsValid() || !persistentScene.isLoaded)
            {
                return;
            }

            GameObject[] persistentRoots = persistentScene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < persistentRoots.Length; rootIndex++)
            {
                T[] components = persistentRoots[rootIndex].GetComponentsInChildren<T>(true);
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    results.Add(components[componentIndex]);
                }
            }
        }

        private static Scene GetDontDestroyOnLoadScene()
        {
            GameObject marker = new GameObject("DontDestroyOnLoadSceneLookup");
            DontDestroyOnLoad(marker);
            Scene scene = marker.scene;
            Destroy(marker);
            return scene;
        }
    }
}
