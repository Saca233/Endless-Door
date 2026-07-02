using System;
using System.Collections.Generic;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopWindowManager : MonoBehaviour
    {
        [SerializeField] private RectTransform desktopBounds;

        private readonly List<DesktopWindowController> windows = new List<DesktopWindowController>();
        private readonly HashSet<DesktopWindowController> draggedWindows = new HashSet<DesktopWindowController>();

        public event Action<DesktopWindowController> WindowDragStarted;
        public event Action<DesktopWindowController> WindowDragging;
        public event Action<DesktopWindowController> WindowDragEnded;

        public RectTransform DesktopBounds => desktopBounds;
        public DesktopWindowController FocusedWindow { get; private set; }
        public int RegisteredWindowCount => windows.Count;
        public bool AnyWindowCurrentlyDragging => draggedWindows.Count > 0;
        public IReadOnlyList<DesktopWindowController> RegisteredWindows => windows;

        public void SetDesktopBounds(RectTransform value)
        {
            desktopBounds = value;
        }

        public bool RegisterWindow(DesktopWindowController window)
        {
            if (window == null || windows.Contains(window))
            {
                return false;
            }

            windows.Add(window);
            window.SetManager(this);
            if (FocusedWindow == null)
            {
                FocusWindow(window);
            }
            return true;
        }

        public bool UnregisterWindow(DesktopWindowController window)
        {
            if (window == null)
            {
                return false;
            }

            bool removed = windows.Remove(window);
            if (draggedWindows.Remove(window))
            {
                WindowDragEnded?.Invoke(window);
            }

            if (FocusedWindow == window)
            {
                FocusedWindow = null;
                if (windows.Count > 0)
                {
                    FocusWindow(windows[windows.Count - 1]);
                }
            }

            return removed;
        }

        public void FocusWindow(DesktopWindowController window)
        {
            if (window == null || !windows.Contains(window))
            {
                return;
            }

            for (int i = 0; i < windows.Count; i++)
            {
                windows[i].SetFocused(windows[i] == window);
            }

            FocusedWindow = window;
            window.transform.SetAsLastSibling();
            windows.Remove(window);
            windows.Add(window);
        }

        public void NotifyDragStarted(DesktopWindowController window)
        {
            if (window == null)
            {
                return;
            }

            FocusWindow(window);
            if (draggedWindows.Add(window))
            {
                WindowDragStarted?.Invoke(window);
            }
        }

        public void NotifyDragging(DesktopWindowController window)
        {
            if (window != null && draggedWindows.Contains(window))
            {
                WindowDragging?.Invoke(window);
            }
        }

        public void NotifyDragEnded(DesktopWindowController window)
        {
            if (window != null && draggedWindows.Remove(window))
            {
                WindowDragEnded?.Invoke(window);
            }
        }
    }
}
