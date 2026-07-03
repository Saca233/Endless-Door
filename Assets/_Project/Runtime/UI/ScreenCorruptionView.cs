using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class ScreenCorruptionView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Graphic[] glitchGraphics;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField, Range(0f, 1f)] private float intensity;
        [SerializeField] private bool animateWhenVisible = true;
        [SerializeField, Min(0f)] private float jitterPixels = 14f;

        private readonly Vector2[] initialAnchoredPositions = new Vector2[16];
        private RectTransform[] graphicRects;
        private Coroutine timedRoutine;

        public float Intensity => intensity;

        private void Awake()
        {
            ResolveReferences();
            CacheGraphicRects();
            ApplyIntensity();
        }

        private void Update()
        {
            if (!animateWhenVisible || intensity <= 0.001f || graphicRects == null)
            {
                return;
            }

            for (int i = 0; i < graphicRects.Length && i < initialAnchoredPositions.Length; i++)
            {
                if (graphicRects[i] == null)
                {
                    continue;
                }

                float offset = Mathf.Sin(Time.unscaledTime * (19f + i * 3f)) * jitterPixels * intensity;
                graphicRects[i].anchoredPosition = initialAnchoredPositions[i] + new Vector2(offset, 0f);
            }
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            ResolveReferences();
            ApplyIntensity();
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        public void Clear()
        {
            if (timedRoutine != null)
            {
                StopCoroutine(timedRoutine);
                timedRoutine = null;
            }

            SetIntensity(0f);
            RestoreGraphicPositions();
        }

        public void GlitchFor(float duration, float targetIntensity)
        {
            if (timedRoutine != null)
            {
                StopCoroutine(timedRoutine);
            }

            timedRoutine = StartCoroutine(GlitchRoutine(duration, targetIntensity));
        }

        private IEnumerator GlitchRoutine(float duration, float targetIntensity)
        {
            SetIntensity(targetIntensity);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Clear();
        }

        private void ApplyIntensity()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = intensity;
                canvasGroup.blocksRaycasts = intensity > 0.001f;
                canvasGroup.interactable = false;
            }

            if (glitchGraphics == null)
            {
                return;
            }

            for (int i = 0; i < glitchGraphics.Length; i++)
            {
                if (glitchGraphics[i] == null)
                {
                    continue;
                }

                Color color = glitchGraphics[i].color;
                color.a = intensity;
                glitchGraphics[i].color = color;
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void CacheGraphicRects()
        {
            if (glitchGraphics == null)
            {
                graphicRects = null;
                return;
            }

            int count = Mathf.Min(glitchGraphics.Length, initialAnchoredPositions.Length);
            graphicRects = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                graphicRects[i] = glitchGraphics[i] != null ? glitchGraphics[i].rectTransform : null;
                initialAnchoredPositions[i] = graphicRects[i] != null ? graphicRects[i].anchoredPosition : Vector2.zero;
            }
        }

        private void RestoreGraphicPositions()
        {
            if (graphicRects == null)
            {
                return;
            }

            for (int i = 0; i < graphicRects.Length && i < initialAnchoredPositions.Length; i++)
            {
                if (graphicRects[i] != null)
                {
                    graphicRects[i].anchoredPosition = initialAnchoredPositions[i];
                }
            }
        }
    }
}
