using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class ProjectedWindowBarrierRule : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private string ruleId = "error.barrier";
        [SerializeField] private ErrorWindowController errorWindow;
        [SerializeField] private GameWindowView mainGameWindowView;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private BoxCollider barrierCollider;
        [SerializeField] private Transform barrierPreview;
        [SerializeField] private float gameplayPlaneZ;
        [SerializeField, Min(0.01f)] private float barrierDepth = 1.2f;
        [SerializeField] private bool ruleEnabled = true;
        [SerializeField] private bool showPreview = true;
        [SerializeField] private int resetOrder = 45;

        private readonly Vector3[] windowCorners = new Vector3[4];
        private Canvas windowCanvas;
        private Rect lastWindowRect;
        private Rect lastMainGameRect;
        private bool hasSnapshot;

        public int ResetOrder => resetOrder;
        public string RuleId => ruleId;
        public bool BarrierActive => barrierCollider != null && barrierCollider.enabled;
        public Rect LastNormalizedOverlap { get; private set; }
        public Bounds LastWorldBounds { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToWindow();
            EvaluateNow(true);
        }

        private void LateUpdate()
        {
            if (ruleEnabled && HasInputsChanged())
            {
                EvaluateNow(false);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromWindow();
            DisableBarrier();
        }

        public void SetReferences(ErrorWindowController window, GameWindowView mainWindowView, Camera camera, BoxCollider collider)
        {
            SetErrorWindow(window);
            mainGameWindowView = mainWindowView;
            gameplayCamera = camera;
            barrierCollider = collider;
            ResolveReferences();
            EvaluateNow(true);
        }

        public void SetErrorWindow(ErrorWindowController window)
        {
            if (errorWindow == window)
            {
                return;
            }

            UnsubscribeFromWindow();
            errorWindow = window;
            windowCanvas = null;
            hasSnapshot = false;
            if (isActiveAndEnabled)
            {
                SubscribeToWindow();
                EvaluateNow(true);
            }
            else
            {
                DisableBarrier();
            }
        }

        public void SetRuleEnabled(bool enabled)
        {
            ruleEnabled = enabled;
            if (ruleEnabled)
            {
                EvaluateNow(true);
            }
            else
            {
                DisableBarrier();
            }
        }

        public void EvaluateNow(bool force)
        {
            if (!ruleEnabled)
            {
                DisableBarrier();
                return;
            }

            if (!force && !HasInputsChanged())
            {
                return;
            }

            if (!TryGetScreenRects(out Rect errorWindowRect, out Rect mainGameWindowRect)
                || !ApplyBarrierForScreenRects(errorWindowRect, mainGameWindowRect))
            {
                DisableBarrier();
                CaptureSnapshot(errorWindowRect, mainGameWindowRect);
            }
        }

        public bool ApplyBarrierForScreenRects(Rect errorWindowRect, Rect mainGameWindowRect)
        {
            if (gameplayCamera == null || barrierCollider == null)
            {
                DisableBarrier();
                return false;
            }

            bool hasBarrier = TryCalculateBarrier(
                errorWindowRect,
                mainGameWindowRect,
                new Vector2(gameplayCamera.transform.position.x, gameplayCamera.transform.position.y),
                gameplayCamera.orthographicSize,
                gameplayCamera.aspect,
                gameplayPlaneZ,
                barrierDepth,
                out Vector3 center,
                out Vector3 size,
                out Rect normalizedRect);

            if (!hasBarrier)
            {
                DisableBarrier();
                return false;
            }

            barrierCollider.transform.position = center;
            barrierCollider.transform.rotation = Quaternion.identity;
            barrierCollider.center = Vector3.zero;
            barrierCollider.size = size;
            barrierCollider.isTrigger = false;
            barrierCollider.enabled = true;

            if (barrierPreview != null)
            {
                barrierPreview.gameObject.SetActive(showPreview);
                barrierPreview.position = center;
                barrierPreview.rotation = Quaternion.identity;
                barrierPreview.localScale = size;
            }

            LastNormalizedOverlap = normalizedRect;
            LastWorldBounds = new Bounds(center, size);
            CaptureSnapshot(errorWindowRect, mainGameWindowRect);
            return true;
        }

        public static bool TryCalculateBarrier(
            Rect errorWindowScreenRect,
            Rect mainGameWindowScreenRect,
            Vector2 cameraCenter,
            float orthographicSize,
            float aspect,
            float gameplayPlaneZ,
            float depth,
            out Vector3 center,
            out Vector3 size,
            out Rect normalizedRect)
        {
            center = Vector3.zero;
            size = Vector3.zero;
            normalizedRect = Rect.zero;

            if (!WindowIntersectionUtility.TryGetNormalizedIntersection(errorWindowScreenRect, mainGameWindowScreenRect, out normalizedRect))
            {
                return false;
            }

            Rect worldRect = OrthographicViewportWorldConverter.ViewportRectToWorldRect(normalizedRect, cameraCenter, orthographicSize, aspect);
            if (worldRect.width <= 0f || worldRect.height <= 0f)
            {
                return false;
            }

            center = new Vector3(worldRect.center.x, worldRect.center.y, gameplayPlaneZ);
            size = new Vector3(worldRect.width, worldRect.height, Mathf.Max(0.01f, depth));
            return true;
        }

        public void RuntimeReset()
        {
            DisableBarrier();
            hasSnapshot = false;
        }

        private bool TryGetScreenRects(out Rect errorWindowRect, out Rect mainGameWindowRect)
        {
            errorWindowRect = Rect.zero;
            mainGameWindowRect = Rect.zero;
            if (errorWindow == null || !errorWindow.IsActive || errorWindow.WindowRoot == null || mainGameWindowView == null)
            {
                return false;
            }

            if (windowCanvas == null)
            {
                windowCanvas = errorWindow.WindowRoot.GetComponentInParent<Canvas>();
            }

            Camera eventCamera = windowCanvas == null || windowCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : windowCanvas.worldCamera;
            if (!ScreenRectUtility.TryGetRectTransformScreenRect(errorWindow.WindowRoot, eventCamera, windowCorners, out errorWindowRect))
            {
                return false;
            }

            mainGameWindowRect = mainGameWindowView.GetVisibleScreenRect();
            return mainGameWindowRect.width > 0f && mainGameWindowRect.height > 0f;
        }

        private bool HasInputsChanged()
        {
            if (!hasSnapshot)
            {
                return true;
            }

            if (!TryGetScreenRects(out Rect errorWindowRect, out Rect mainGameWindowRect))
            {
                return BarrierActive;
            }

            return !Approximately(errorWindowRect, lastWindowRect) || !Approximately(mainGameWindowRect, lastMainGameRect);
        }

        private void CaptureSnapshot(Rect errorWindowRect, Rect mainGameWindowRect)
        {
            lastWindowRect = errorWindowRect;
            lastMainGameRect = mainGameWindowRect;
            hasSnapshot = true;
        }

        private void SubscribeToWindow()
        {
            if (errorWindow == null)
            {
                return;
            }

            errorWindow.Activated += OnErrorWindowChanged;
            errorWindow.Deactivated += OnErrorWindowChanged;

            DesktopWindowController controller = errorWindow.WindowController;
            if (controller == null)
            {
                return;
            }

            controller.DragStarted += OnWindowDragChanged;
            controller.Dragging += OnWindowDragChanged;
            controller.DragEnded += OnWindowDragChanged;
        }

        private void UnsubscribeFromWindow()
        {
            if (errorWindow == null)
            {
                return;
            }

            errorWindow.Activated -= OnErrorWindowChanged;
            errorWindow.Deactivated -= OnErrorWindowChanged;

            DesktopWindowController controller = errorWindow.WindowController;
            if (controller == null)
            {
                return;
            }

            controller.DragStarted -= OnWindowDragChanged;
            controller.Dragging -= OnWindowDragChanged;
            controller.DragEnded -= OnWindowDragChanged;
        }

        private void OnErrorWindowChanged(ErrorWindowController window)
        {
            EvaluateNow(true);
        }

        private void OnWindowDragChanged(DesktopWindowController window)
        {
            EvaluateNow(true);
        }

        private void DisableBarrier()
        {
            if (barrierCollider != null)
            {
                barrierCollider.enabled = false;
            }

            if (barrierPreview != null)
            {
                barrierPreview.gameObject.SetActive(false);
            }

            LastNormalizedOverlap = Rect.zero;
            LastWorldBounds = default;
        }

        private void ResolveReferences()
        {
            if (barrierCollider == null)
            {
                barrierCollider = GetComponent<BoxCollider>();
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
