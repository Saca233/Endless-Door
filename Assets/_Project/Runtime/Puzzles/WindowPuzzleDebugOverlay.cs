using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class WindowPuzzleDebugOverlay : MonoBehaviour
    {
        [SerializeField] private CoverToEraseRule rule;
        [SerializeField] private bool overlayEnabled;
        [SerializeField] private Color targetColor = Color.cyan;
        [SerializeField] private Color coveringWindowColor = Color.yellow;
        [SerializeField] private Color intersectionColor = Color.green;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!overlayEnabled || rule == null)
            {
                return;
            }

            for (int i = 0; i < rule.TargetCount; i++)
            {
                if (!rule.TryGetDebugSnapshot(i, out Rect targetRect, out Rect coveringRect, out Rect intersectionRect, out float coverage))
                {
                    continue;
                }

                DrawOutline(ToGuiRect(targetRect), targetColor, 2f);
                DrawOutline(ToGuiRect(coveringRect), coveringWindowColor, 2f);
                DrawOutline(ToGuiRect(intersectionRect), intersectionColor, 2f);
                GUI.color = Color.white;
                GUI.Label(new Rect(12f, 12f + i * 22f, 260f, 22f), $"Target {i + 1}: {coverage:P0} covered");
            }
        }

        private static Rect ToGuiRect(Rect screenRect)
        {
            return new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        }
#endif
    }
}
