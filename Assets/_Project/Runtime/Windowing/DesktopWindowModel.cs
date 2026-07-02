using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopWindowModel : MonoBehaviour
    {
        [SerializeField] private DesktopWindowId windowId = DesktopWindowId.NewId();
        [SerializeField] private string title = "Window";
        [SerializeField] private Vector2 currentPosition;
        [SerializeField] private Vector2 currentSize = new Vector2(640f, 360f);
        [SerializeField] private Vector2 minimumSize = new Vector2(220f, 140f);
        [SerializeField] private bool draggingAllowed = true;
        [SerializeField] private bool resizingAllowed;
        [SerializeField] private bool focused;
        [SerializeField] private bool visible = true;

        public DesktopWindowId WindowId => windowId;
        public string Title => title;
        public Vector2 CurrentPosition => currentPosition;
        public Vector2 CurrentSize => currentSize;
        public Vector2 MinimumSize => minimumSize;
        public bool DraggingAllowed => draggingAllowed;
        public bool ResizingAllowed => resizingAllowed;
        public bool Focused => focused;
        public bool Visible => visible;

        private void Reset()
        {
            RegenerateId();
        }

        private void OnValidate()
        {
            if (!windowId.IsValid)
            {
                RegenerateId();
            }

            currentSize = Vector2.Max(currentSize, minimumSize);
            minimumSize = new Vector2(Mathf.Max(1f, minimumSize.x), Mathf.Max(1f, minimumSize.y));
        }

        public void RegenerateId()
        {
            windowId = DesktopWindowId.NewId();
        }

        public void SetTitle(string value)
        {
            title = string.IsNullOrWhiteSpace(value) ? "Window" : value;
        }

        public void SetPosition(Vector2 value)
        {
            currentPosition = value;
        }

        public void SetSize(Vector2 value)
        {
            currentSize = Vector2.Max(value, minimumSize);
        }

        public void SetMinimumSize(Vector2 value)
        {
            minimumSize = new Vector2(Mathf.Max(1f, value.x), Mathf.Max(1f, value.y));
            currentSize = Vector2.Max(currentSize, minimumSize);
        }

        public void SetDraggingAllowed(bool value)
        {
            draggingAllowed = value;
        }

        public void SetResizingAllowed(bool value)
        {
            resizingAllowed = value;
        }

        public void SetFocused(bool value)
        {
            focused = value;
        }

        public void SetVisible(bool value)
        {
            visible = value;
        }
    }
}
