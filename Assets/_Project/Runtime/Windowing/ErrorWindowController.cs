using System;
using TMPro;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DesktopWindowController))]
    public sealed class ErrorWindowController : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private DesktopWindowController windowController;
        [SerializeField] private DesktopWindowModel model;
        [SerializeField] private DesktopWindowView view;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private string title = "Error 404";
        [SerializeField, TextArea(2, 5)] private string content = "ERROR 404\nMissing sector.";
        [SerializeField] private int resetOrder = 30;

        public event Action<ErrorWindowController> Activated;
        public event Action<ErrorWindowController> Deactivated;

        public int ResetOrder => resetOrder;
        public bool IsActive { get; private set; }
        public DesktopWindowController WindowController => windowController;
        public RectTransform WindowRoot => view != null ? view.WindowRoot : null;

        private void Awake()
        {
            ResolveReferences();
            ApplyContent();
        }

        public void Activate(WindowLayoutDefinition layout)
        {
            ResolveReferences();
            if (model == null || windowController == null)
            {
                return;
            }

            Vector2 position = layout != null ? layout.SpawnPosition : model.CurrentPosition;
            Vector2 size = layout != null ? layout.Size : model.CurrentSize;
            bool draggable = layout == null || layout.DraggingEnabled;
            model.SetTitle(title);
            model.SetPosition(position);
            model.SetSize(size);
            model.SetDraggingAllowed(draggable);
            model.SetVisible(true);
            ApplyContent();
            windowController.ApplyModelToView();
            windowController.Manager?.FocusWindow(windowController);

            if (!IsActive)
            {
                IsActive = true;
                Activated?.Invoke(this);
            }
        }

        public void Deactivate()
        {
            ResolveReferences();
            if (model != null)
            {
                model.SetVisible(false);
            }

            windowController?.ApplyModelToView();
            if (IsActive)
            {
                IsActive = false;
                Deactivated?.Invoke(this);
            }
        }

        public void SetContent(string newTitle, string newContent)
        {
            title = string.IsNullOrWhiteSpace(newTitle) ? "Error 404" : newTitle;
            content = newContent ?? string.Empty;
            ApplyContent();
            windowController?.ApplyModelToView();
        }

        public void RuntimeReset()
        {
            Deactivate();
        }

        private void ApplyContent()
        {
            if (model != null)
            {
                model.SetTitle(title);
            }

            if (contentText != null)
            {
                contentText.text = content;
            }
        }

        private void ResolveReferences()
        {
            if (windowController == null)
            {
                windowController = GetComponent<DesktopWindowController>();
            }

            if (model == null)
            {
                model = GetComponent<DesktopWindowModel>();
            }

            if (view == null)
            {
                view = GetComponent<DesktopWindowView>();
            }
        }
    }
}
