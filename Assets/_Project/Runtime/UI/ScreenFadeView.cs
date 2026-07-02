using System.Collections;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class ScreenFadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool blockRaycastsWhenVisible = true;

        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0f;

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetAlpha(float alpha)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = Mathf.Clamp01(alpha);
            bool visible = canvasGroup.alpha > 0.001f;
            canvasGroup.blocksRaycasts = blockRaycastsWhenVisible && visible;
            canvasGroup.interactable = false;
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            ResolveReferences();
            if (canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float clampedTarget = Mathf.Clamp01(targetAlpha);
            if (duration <= 0f)
            {
                SetAlpha(clampedTarget);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, clampedTarget, elapsed / duration));
                yield return null;
            }

            SetAlpha(clampedTarget);
        }

        public IEnumerator FadeIn(float duration)
        {
            return FadeTo(0f, duration);
        }

        public IEnumerator FadeOut(float duration)
        {
            return FadeTo(1f, duration);
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
