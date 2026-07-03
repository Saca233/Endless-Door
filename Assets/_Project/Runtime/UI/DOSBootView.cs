using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DOSBootView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI bootText;
        [SerializeField, TextArea(1, 3)] private string[] bootLines =
        {
            "OWARI SYSTEM BIOS v0.1",
            "Checking memory...",
            "Mounting /desktop",
            "Loading endless-door kernel",
            "Boot complete."
        };
        [SerializeField, Min(0f)] private float initialDelay = 0.25f;
        [SerializeField, Min(0f)] private float lineDelay = 0.15f;
        [SerializeField, Min(0f)] private float characterDelay = 0.012f;
        [SerializeField, Min(0f)] private float finalDelay = 0.45f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField] private bool allowDevelopmentSkip = true;

        private bool skipRequested;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        public IEnumerator Play()
        {
            ResolveReferences();
            skipRequested = false;
            ShowImmediate();

            if (bootText != null)
            {
                bootText.text = string.Empty;
            }

            yield return WaitOrSkip(initialDelay);

            for (int i = 0; i < bootLines.Length && !skipRequested; i++)
            {
                string line = bootLines[i] ?? string.Empty;
                for (int c = 0; c < line.Length && !skipRequested; c++)
                {
                    if (bootText != null)
                    {
                        bootText.text += line[c];
                    }

                    yield return WaitOrSkip(characterDelay);
                }

                if (bootText != null)
                {
                    bootText.text += "\n";
                }

                yield return WaitOrSkip(lineDelay);
            }

            if (skipRequested && bootText != null)
            {
                bootText.text = string.Join("\n", bootLines) + "\n";
            }

            yield return WaitOrSkip(finalDelay);
            yield return FadeOut();
            HideImmediate();
        }

        private IEnumerator WaitOrSkip(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < seconds && !skipRequested)
            {
                UpdateSkipRequest();
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float start = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration && !skipRequested)
            {
                UpdateSkipRequest();
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        private void UpdateSkipRequest()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!allowDevelopmentSkip)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                skipRequested = true;
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                skipRequested = true;
            }
#endif
        }

        private void ShowImmediate()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        private void HideImmediate()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
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
