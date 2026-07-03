using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class CoverToEraseRule : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private DesktopWindowController coveringWindow;
        [SerializeField] private GameWindowCamera gameWindowCamera;
        [SerializeField] private GameWindowView gameWindowView;
        [SerializeField] private WindowPuzzleTarget[] targets = System.Array.Empty<WindowPuzzleTarget>();
        [SerializeField, Range(0f, 1f)] private float activationCoverage = 0.55f;
        [SerializeField, Range(0f, 1f)] private float deactivationCoverage = 0.45f;
        [SerializeField] private bool ruleEnabled = true;

        private readonly Vector3[] projectionCorners = new Vector3[WorldBoundsProjector.CornerCount];
        private readonly Vector3[] windowCorners = new Vector3[4];
        private bool[] targetActive = System.Array.Empty<bool>();
        private Rect[] targetRects = System.Array.Empty<Rect>();
        private Rect[] intersectionRects = System.Array.Empty<Rect>();
        private float[] targetCoverages = System.Array.Empty<float>();
        private string effectSourceId;
        private bool hasSnapshot;
        private Rect lastCoveringWindowRect;
        private Matrix4x4 lastCameraViewProjection;
        private Rect lastGameWindowRect;
        private Canvas coveringWindowCanvas;

        public event Action<WindowPuzzleTarget, float> Activated;
        public event Action<WindowPuzzleTarget, float> Deactivated;

        public bool RuleEnabled => ruleEnabled;
        public DesktopWindowController CoveringWindow => coveringWindow;
        public int TargetCount => targets == null ? 0 : targets.Length;
        public Rect LastCoveringWindowRect => lastCoveringWindowRect;
        public int ResetOrder => 40;

        private void Awake()
        {
            EnsureEffectSourceId();
            EnsureStateArrays();
        }

        private void OnEnable()
        {
            EnsureEffectSourceId();
            EnsureStateArrays();
            SubscribeToWindow();
            EvaluateNow(true);
        }

        private void Update()
        {
            if (!ruleEnabled)
            {
                return;
            }

            if (HasProjectionInputsChanged())
            {
                EvaluateNow(false);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromWindow();
            ReleaseAllEffects();
        }

        public void SetRuleEnabled(bool enabled)
        {
            if (ruleEnabled == enabled)
            {
                return;
            }

            ruleEnabled = enabled;
            if (ruleEnabled)
            {
                EvaluateNow(true);
            }
            else
            {
                ReleaseAllEffects();
            }
        }

        public void EvaluateNow(bool force)
        {
            if (!ruleEnabled || coveringWindow == null || gameWindowCamera == null || gameWindowCamera.GameplayCamera == null || gameWindowView == null)
            {
                return;
            }

            EnsureStateArrays();
            if (!force && !HasProjectionInputsChanged())
            {
                return;
            }

            if (!TryGetCoveringWindowRect(out Rect coveringRect))
            {
                return;
            }

            lastCoveringWindowRect = coveringRect;
            for (int i = 0; i < targets.Length; i++)
            {
                WindowPuzzleTarget target = targets[i];
                Rect targetRect = Rect.zero;
                Rect intersection = Rect.zero;
                float coverage = 0f;

                if (target != null
                    && target.TryGetWorldBounds(out Bounds bounds)
                    && WorldBoundsProjector.TryProjectBoundsToDesktopRect(bounds, gameWindowCamera.GameplayCamera, gameWindowView, projectionCorners, out targetRect))
                {
                    intersection = ScreenRectUtility.GetIntersection(targetRect, coveringRect);
                    coverage = ScreenRectUtility.GetCoverageRatio(targetRect, coveringRect);
                }

                targetRects[i] = targetRect;
                intersectionRects[i] = intersection;
                targetCoverages[i] = coverage;

                bool nextActive = EvaluateHysteresis(targetActive[i], coverage, activationCoverage, deactivationCoverage);
                if (nextActive == targetActive[i])
                {
                    continue;
                }

                targetActive[i] = nextActive;
                if (target == null)
                {
                    continue;
                }

                if (nextActive)
                {
                    target.ApplyEffect(effectSourceId);
                    Activated?.Invoke(target, coverage);
                }
                else
                {
                    target.ReleaseEffect(effectSourceId);
                    Deactivated?.Invoke(target, coverage);
                }
            }

            CaptureProjectionSnapshot(coveringRect);
        }

        public bool TryGetDebugSnapshot(int index, out Rect targetRect, out Rect coveringRect, out Rect intersectionRect, out float coverage)
        {
            targetRect = Rect.zero;
            coveringRect = lastCoveringWindowRect;
            intersectionRect = Rect.zero;
            coverage = 0f;

            if (index < 0 || index >= targetRects.Length)
            {
                return false;
            }

            targetRect = targetRects[index];
            intersectionRect = intersectionRects[index];
            coverage = targetCoverages[index];
            return ScreenRectUtility.GetArea(targetRect) > 0f;
        }

        public static bool EvaluateHysteresis(bool currentlyActive, float coverage, float activationThreshold, float deactivationThreshold)
        {
            if (currentlyActive)
            {
                return coverage > deactivationThreshold;
            }

            return coverage >= activationThreshold;
        }

        public void SetTargets(WindowPuzzleTarget[] value)
        {
            ReleaseAllEffects();
            targets = value ?? System.Array.Empty<WindowPuzzleTarget>();
            targetActive = System.Array.Empty<bool>();
            targetRects = System.Array.Empty<Rect>();
            intersectionRects = System.Array.Empty<Rect>();
            targetCoverages = System.Array.Empty<float>();
            hasSnapshot = false;
            EnsureStateArrays();
            EvaluateNow(true);
        }

        public void RuntimeReset()
        {
            ReleaseAllEffects();
        }

        public void ReleaseAllEffects()
        {
            EnsureEffectSourceId();
            EnsureStateArrays();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targetActive[i])
                {
                    targets[i].ReleaseEffect(effectSourceId);
                }

                targetActive[i] = false;
                targetCoverages[i] = 0f;
                targetRects[i] = Rect.zero;
                intersectionRects[i] = Rect.zero;
            }
        }

        private bool TryGetCoveringWindowRect(out Rect rect)
        {
            rect = Rect.zero;
            if (coveringWindow.View == null || coveringWindow.View.WindowRoot == null)
            {
                return false;
            }

            if (coveringWindowCanvas == null)
            {
                coveringWindowCanvas = coveringWindow.View.WindowRoot.GetComponentInParent<Canvas>();
            }

            Camera eventCamera = coveringWindowCanvas == null || coveringWindowCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : coveringWindowCanvas.worldCamera;
            return ScreenRectUtility.TryGetRectTransformScreenRect(coveringWindow.View.WindowRoot, eventCamera, windowCorners, out rect);
        }

        private void SubscribeToWindow()
        {
            if (coveringWindow == null)
            {
                return;
            }

            coveringWindow.DragStarted += OnWindowDragChanged;
            coveringWindow.Dragging += OnWindowDragChanged;
            coveringWindow.DragEnded += OnWindowDragChanged;
        }

        private void UnsubscribeFromWindow()
        {
            if (coveringWindow == null)
            {
                return;
            }

            coveringWindow.DragStarted -= OnWindowDragChanged;
            coveringWindow.Dragging -= OnWindowDragChanged;
            coveringWindow.DragEnded -= OnWindowDragChanged;
        }

        private void OnWindowDragChanged(DesktopWindowController window)
        {
            EvaluateNow(true);
        }

        private bool HasProjectionInputsChanged()
        {
            if (!hasSnapshot)
            {
                return true;
            }

            if (!TryGetCoveringWindowRect(out Rect coveringRect))
            {
                return false;
            }

            Matrix4x4 viewProjection = GetCameraViewProjectionMatrix();
            Rect gameWindowRect = gameWindowView != null ? gameWindowView.GetVisibleScreenRect() : Rect.zero;
            return !Approximately(coveringRect, lastCoveringWindowRect)
                || !Approximately(gameWindowRect, lastGameWindowRect)
                || viewProjection != lastCameraViewProjection;
        }

        private void CaptureProjectionSnapshot(Rect coveringRect)
        {
            hasSnapshot = true;
            lastCoveringWindowRect = coveringRect;
            lastGameWindowRect = gameWindowView != null ? gameWindowView.GetVisibleScreenRect() : Rect.zero;
            lastCameraViewProjection = GetCameraViewProjectionMatrix();
        }

        private Matrix4x4 GetCameraViewProjectionMatrix()
        {
            Camera camera = gameWindowCamera != null ? gameWindowCamera.GameplayCamera : null;
            if (camera == null)
            {
                return Matrix4x4.identity;
            }

            return camera.projectionMatrix * camera.worldToCameraMatrix;
        }

        private void EnsureStateArrays()
        {
            if (targets == null)
            {
                targets = System.Array.Empty<WindowPuzzleTarget>();
            }

            if (targetActive.Length == targets.Length)
            {
                return;
            }

            targetActive = new bool[targets.Length];
            targetRects = new Rect[targets.Length];
            intersectionRects = new Rect[targets.Length];
            targetCoverages = new float[targets.Length];
        }

        private void EnsureEffectSourceId()
        {
            if (string.IsNullOrEmpty(effectSourceId))
            {
                effectSourceId = $"{nameof(CoverToEraseRule)}:{GetInstanceID()}";
            }
        }

        private static bool Approximately(Rect a, Rect b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.width, b.width)
                && Mathf.Approximately(a.height, b.height);
        }
    }
}
