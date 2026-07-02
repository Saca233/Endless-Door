using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopWindowView : MonoBehaviour
    {
        [SerializeField] private RectTransform windowRoot;
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Graphic[] borderVisuals = System.Array.Empty<Graphic>();

        public RectTransform WindowRoot => windowRoot;
        public RectTransform TitleBar => titleBar;
        public RectTransform ContentRoot => contentRoot;
        public TextMeshProUGUI TitleText => titleText;
        public Button CloseButton => closeButton;
        public Graphic[] BorderVisuals => borderVisuals;

        private void Reset()
        {
            windowRoot = transform as RectTransform;
        }

        public void ApplyTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void ApplyFocus(bool focused)
        {
            if (borderVisuals == null)
            {
                return;
            }

            Color color = focused ? new Color(0.3f, 0.7f, 1f, 1f) : new Color(0.25f, 0.25f, 0.28f, 1f);
            for (int i = 0; i < borderVisuals.Length; i++)
            {
                if (borderVisuals[i] != null)
                {
                    borderVisuals[i].color = color;
                }
            }
        }

        public void ApplyVisible(bool visible)
        {
            if (windowRoot != null)
            {
                windowRoot.gameObject.SetActive(visible);
            }
        }
    }
}
