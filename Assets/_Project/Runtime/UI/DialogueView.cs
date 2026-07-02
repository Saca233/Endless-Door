using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DialogueView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private bool useTypewriter = true;
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

        private string currentFullText = string.Empty;
        private float visibleCharacterCount;
        private bool advanceRequested;

        public bool IsVisible { get; private set; }
        public bool LineFullyRevealed { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            Hide();
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            UpdateTypewriter();
            if (AdvanceInputPressed())
            {
                RequestAdvance();
            }
        }

        public void Show()
        {
            ResolveReferences();
            IsVisible = true;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            IsVisible = false;
            advanceRequested = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void DisplayLine(DialogueLineData line)
        {
            Show();
            currentFullText = line != null ? line.Text ?? string.Empty : string.Empty;
            visibleCharacterCount = useTypewriter ? 0f : currentFullText.Length;
            LineFullyRevealed = !useTypewriter || currentFullText.Length == 0;
            advanceRequested = false;

            if (speakerNameText != null)
            {
                speakerNameText.text = line != null ? line.Speaker : string.Empty;
            }

            if (dialogueText != null)
            {
                dialogueText.text = currentFullText;
                dialogueText.maxVisibleCharacters = LineFullyRevealed ? int.MaxValue : 0;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = line != null ? line.Portrait : null;
                portraitImage.enabled = portraitImage.sprite != null;
            }
        }

        public void CompleteLine()
        {
            visibleCharacterCount = currentFullText.Length;
            LineFullyRevealed = true;
            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        public bool ConsumeAdvanceRequest()
        {
            if (!advanceRequested)
            {
                return false;
            }

            advanceRequested = false;
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsVisible)
            {
                RequestAdvance();
            }
        }

        private void RequestAdvance()
        {
            advanceRequested = true;
        }

        private void UpdateTypewriter()
        {
            if (LineFullyRevealed || !useTypewriter)
            {
                return;
            }

            visibleCharacterCount += charactersPerSecond * Time.unscaledDeltaTime;
            if (visibleCharacterCount >= currentFullText.Length)
            {
                CompleteLine();
                return;
            }

            if (dialogueText != null)
            {
                dialogueText.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacterCount);
            }
        }

        private bool AdvanceInputPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
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
