using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class DesktopWindowManagerTests
    {
        [Test]
        public void RegistersAndUnregistersWindows()
        {
            GameObject root = new GameObject("WindowManagerTestRoot", typeof(RectTransform));
            DesktopWindowManager manager = root.AddComponent<DesktopWindowManager>();
            DesktopWindowController first = CreateWindow(root.transform, "First");
            DesktopWindowController second = CreateWindow(root.transform, "Second");

            Assert.IsTrue(manager.RegisterWindow(first));
            Assert.IsTrue(manager.RegisterWindow(second));
            Assert.IsFalse(manager.RegisterWindow(first));
            Assert.AreEqual(2, manager.RegisteredWindowCount);

            Assert.IsTrue(manager.UnregisterWindow(first));
            Assert.AreEqual(1, manager.RegisteredWindowCount);
            Assert.IsFalse(manager.UnregisterWindow(first));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void FocusMovesWindowToFrontAndUpdatesFocusedWindow()
        {
            GameObject root = new GameObject("WindowFocusTestRoot", typeof(RectTransform));
            DesktopWindowManager manager = root.AddComponent<DesktopWindowManager>();
            DesktopWindowController first = CreateWindow(root.transform, "First");
            DesktopWindowController second = CreateWindow(root.transform, "Second");

            manager.RegisterWindow(first);
            manager.RegisterWindow(second);
            manager.FocusWindow(first);

            Assert.AreSame(first, manager.FocusedWindow);
            Assert.AreSame(first, manager.RegisteredWindows[manager.RegisteredWindowCount - 1]);
            Assert.Greater(first.transform.GetSiblingIndex(), second.transform.GetSiblingIndex());
            Assert.IsTrue(first.Model.Focused);
            Assert.IsFalse(second.Model.Focused);

            Object.DestroyImmediate(root);
        }

        private static DesktopWindowController CreateWindow(Transform parent, string name)
        {
            GameObject window = new GameObject(name, typeof(RectTransform));
            window.transform.SetParent(parent, false);
            DesktopWindowModel model = window.AddComponent<DesktopWindowModel>();
            model.SetTitle(name);
            DesktopWindowView view = window.AddComponent<DesktopWindowView>();
            DesktopWindowController controller = window.AddComponent<DesktopWindowController>();
            controller.ApplyModelToView();
            return controller;
        }
    }
}
