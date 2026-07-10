using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopHostStartupBootAnimation : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private CanvasGroup bootCanvasGroup;
        [SerializeField] private TextMeshProUGUI bootLogText;
        [SerializeField] private Image bootBackground;
        [SerializeField] private Image scanlineImage;
        [SerializeField] private CanvasGroup desktopWindowLayerGroup;
        [SerializeField, TextArea(1, 3)] private string[] bootLines =
        {
            "OWARI BIOS v0.02",
            "Memory check: 65536 KB OK",
            "Mounting /desktop",
            "Loading 終わりなき扉.exe",
            "Window subsystem online",
            "Boot complete."
        };
        [SerializeField, Min(0f)] private float initialDelay = 0.35f;
        [SerializeField, Min(0f)] private float characterDelay = 0.018f;
        [SerializeField, Min(0f)] private float lineDelay = 0.08f;
        [SerializeField, Min(0f)] private float holdAfterLog = 0.45f;
        [SerializeField, Min(0f)] private float desktopFadeDuration = 0.9f;
        [SerializeField] private bool hideDesktopWindowsDuringBoot = true;
        [SerializeField] private bool allowDevelopmentSkip = true;

        private readonly StringBuilder textBuilder = new StringBuilder(512);
        private Coroutine activeRoutine;
        private bool skipRequested;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            RestoreDesktopWindows();
        }

        public void Play()
        {
            if (activeRoutine != null)
            {
                return;
            }

            activeRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            skipRequested = false;
            textBuilder.Length = 0;
            SetDesktopWindowsAlpha(hideDesktopWindowsDuringBoot ? 0f : 1f);
            ShowImmediate();

            if (bootLogText != null)
            {
                bootLogText.text = string.Empty;
            }

            yield return WaitOrSkip(initialDelay);

            for (int lineIndex = 0; lineIndex < bootLines.Length && !skipRequested; lineIndex++)
            {
                string line = bootLines[lineIndex] ?? string.Empty;
                for (int characterIndex = 0; characterIndex < line.Length && !skipRequested; characterIndex++)
                {
                    textBuilder.Append(line[characterIndex]);
                    ApplyBootText();
                    UpdateScanlinePulse();
                    yield return WaitOrSkip(characterDelay);
                }

                textBuilder.AppendLine();
                ApplyBootText();
                yield return WaitOrSkip(lineDelay);
            }

            if (skipRequested)
            {
                textBuilder.Length = 0;
                for (int i = 0; i < bootLines.Length; i++)
                {
                    textBuilder.AppendLine(bootLines[i] ?? string.Empty);
                }

                ApplyBootText();
            }

            yield return WaitOrSkip(holdAfterLog);
            yield return RevealDesktop();
            HideImmediate();
            RestoreDesktopWindows();
            activeRoutine = null;
        }

        private IEnumerator RevealDesktop()
        {
            if (desktopFadeDuration <= 0f || bootCanvasGroup == null)
            {
                SetDesktopWindowsAlpha(1f);
                if (bootCanvasGroup != null)
                {
                    bootCanvasGroup.alpha = 0f;
                }

                yield break;
            }

            float elapsed = 0f;
            while (elapsed < desktopFadeDuration && !skipRequested)
            {
                UpdateSkipRequest();
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / desktopFadeDuration);
                bootCanvasGroup.alpha = 1f - t;
                SetDesktopWindowsAlpha(Mathf.Lerp(hideDesktopWindowsDuringBoot ? 0f : 1f, 1f, t));
                UpdateScanlinePulse();
                yield return null;
            }

            bootCanvasGroup.alpha = 0f;
            SetDesktopWindowsAlpha(1f);
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
                UpdateScanlinePulse();
                yield return null;
            }
        }

        private void ApplyBootText()
        {
            if (bootLogText != null)
            {
                bootLogText.text = textBuilder.ToString();
            }
        }

        private void UpdateScanlinePulse()
        {
            if (scanlineImage == null)
            {
                return;
            }

            Color color = scanlineImage.color;
            color.a = 0.05f + Mathf.PingPong(Time.unscaledTime * 0.35f, 0.05f);
            scanlineImage.color = color;
        }

        private void UpdateSkipRequest()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!allowDevelopmentSkip)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
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
            ResolveReferences();
            if (bootCanvasGroup == null)
            {
                return;
            }

            bootCanvasGroup.alpha = 1f;
            bootCanvasGroup.interactable = false;
            bootCanvasGroup.blocksRaycasts = true;

            if (bootBackground != null)
            {
                Color color = bootBackground.color;
                color.a = 1f;
                bootBackground.color = color;
            }
        }

        private void HideImmediate()
        {
            ResolveReferences();
            if (bootCanvasGroup == null)
            {
                return;
            }

            bootCanvasGroup.alpha = 0f;
            bootCanvasGroup.interactable = false;
            bootCanvasGroup.blocksRaycasts = false;
        }

        private void SetDesktopWindowsAlpha(float alpha)
        {
            if (desktopWindowLayerGroup == null)
            {
                return;
            }

            desktopWindowLayerGroup.alpha = Mathf.Clamp01(alpha);
            desktopWindowLayerGroup.interactable = alpha > 0.99f;
            desktopWindowLayerGroup.blocksRaycasts = alpha > 0.99f;
        }

        private void RestoreDesktopWindows()
        {
            SetDesktopWindowsAlpha(1f);
        }

        private void ResolveReferences()
        {
            if (bootCanvasGroup == null)
            {
                bootCanvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
