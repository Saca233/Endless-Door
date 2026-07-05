using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class UtilityWindowViewportMapper : MonoBehaviour
    {
        [SerializeField] private GameWindowView mainGameWindowView;
        [SerializeField] private RawImage utilityRawImage;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Rect defaultUvRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private bool restoreDefaultWhenNoOverlap = true;
        [SerializeField] private bool debugOverlayEnabled;
        [SerializeField] private CoverToEraseRule debugCoverRule;

        private readonly Vector3[] utilityCorners = new Vector3[4];
        private Rect lastMainVisibleRect;
        private Rect lastUtilityScreenRect;
        private Rect lastIntersectionRect;
        private Rect lastUvRect;
        private bool lastHadOverlap;

        public RawImage UtilityRawImage => utilityRawImage;
        public Rect LastMainVisibleRect => lastMainVisibleRect;
        public Rect LastUtilityScreenRect => lastUtilityScreenRect;
        public Rect LastIntersectionRect => lastIntersectionRect;
        public Rect LastUvRect => lastUvRect;
        public bool LastHadOverlap => lastHadOverlap;

        private void Awake()
        {
            ResolveCanvas();
        }

        private void OnEnable()
        {
            UpdateMapping();
        }

        private void LateUpdate()
        {
            UpdateMapping();
        }

        public void UpdateMapping()
        {
            if (mainGameWindowView == null || utilityRawImage == null)
            {
                return;
            }

            ResolveCanvas();
            lastMainVisibleRect = mainGameWindowView.GetVisibleScreenRect();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!ScreenRectUtility.TryGetRectTransformScreenRect(utilityRawImage.rectTransform, eventCamera, utilityCorners, out lastUtilityScreenRect))
            {
                return;
            }

            lastHadOverlap = TryCalculateMappedUv(lastMainVisibleRect, lastUtilityScreenRect, out lastUvRect, out lastIntersectionRect);
            if (lastHadOverlap || restoreDefaultWhenNoOverlap)
            {
                utilityRawImage.uvRect = lastHadOverlap ? lastUvRect : defaultUvRect;
            }
        }

        public static bool TryCalculateMappedUv(Rect mainVisibleRect, Rect utilityScreenRect, out Rect uvRect)
        {
            return TryCalculateMappedUv(mainVisibleRect, utilityScreenRect, out uvRect, out _);
        }

        public static bool TryCalculateMappedUv(Rect mainVisibleRect, Rect utilityScreenRect, out Rect uvRect, out Rect intersectionRect)
        {
            uvRect = Rect.zero;
            intersectionRect = ScreenRectUtility.GetIntersection(mainVisibleRect, utilityScreenRect);
            if (ScreenRectUtility.GetArea(intersectionRect) <= 0f)
            {
                return false;
            }

            return WindowIntersectionUtility.TryNormalizeRect(intersectionRect, mainVisibleRect, out uvRect);
        }

        private void OnGUI()
        {
            if (!debugOverlayEnabled)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 170f), GUI.skin.box);
            GUILayout.Label($"MainGameWindow: {FormatRect(lastMainVisibleRect)}");
            GUILayout.Label($"UtilityWindow: {FormatRect(lastUtilityScreenRect)}");
            GUILayout.Label($"Intersection: {FormatRect(lastIntersectionRect)}");
            GUILayout.Label($"Utility UV: {FormatRect(lastUvRect)}");
            GUILayout.Label($"Overlapping: {lastHadOverlap}");
            if (debugCoverRule != null && debugCoverRule.TryGetDebugSnapshot(0, out Rect target, out Rect covering, out Rect intersection, out float coverage))
            {
                GUILayout.Label($"Target: {FormatRect(target)}");
                GUILayout.Label($"Cover: {FormatRect(covering)}  Hit: {FormatRect(intersection)}  {coverage:P0}");
            }

            GUILayout.EndArea();
        }

        private void ResolveCanvas()
        {
            if (canvas == null && utilityRawImage != null)
            {
                canvas = utilityRawImage.GetComponentInParent<Canvas>();
            }
        }

        private static string FormatRect(Rect rect)
        {
            return $"x:{rect.x:0.0} y:{rect.y:0.0} w:{rect.width:0.0} h:{rect.height:0.0}";
        }
    }
}
