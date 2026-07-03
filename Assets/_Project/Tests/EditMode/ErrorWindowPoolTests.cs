using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class ErrorWindowPoolTests
    {
        [Test]
        public void PoolResetReturnsAllActiveWindows()
        {
            GameObject poolObject = new GameObject("Pool");
            ErrorWindowController first = CreateErrorWindow("First");
            ErrorWindowController second = CreateErrorWindow("Second");
            WindowLayoutDefinition firstLayout = ScriptableObject.CreateInstance<WindowLayoutDefinition>();
            WindowLayoutDefinition secondLayout = ScriptableObject.CreateInstance<WindowLayoutDefinition>();
            try
            {
                firstLayout.Configure(new Vector2(-10f, 5f), new Vector2(200f, 120f), 0, true, "a");
                secondLayout.Configure(new Vector2(10f, 5f), new Vector2(200f, 120f), 1, true, "b");
                ErrorWindowPool pool = poolObject.AddComponent<ErrorWindowPool>();
                pool.SetWindows(new[] { first, second });
                pool.SetInitialLayouts(new[] { firstLayout, secondLayout });

                pool.ActivateInitialLayouts();

                Assert.AreEqual(2, pool.ActiveCount);
                Assert.IsTrue(first.IsActive);
                Assert.IsTrue(second.IsActive);

                pool.RuntimeReset();

                Assert.AreEqual(0, pool.ActiveCount);
                Assert.IsFalse(first.IsActive);
                Assert.IsFalse(second.IsActive);
            }
            finally
            {
                Object.DestroyImmediate(poolObject);
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
                Object.DestroyImmediate(firstLayout);
                Object.DestroyImmediate(secondLayout);
            }
        }

        private static ErrorWindowController CreateErrorWindow(string name)
        {
            GameObject window = new GameObject(name, typeof(RectTransform));
            DesktopWindowModel model = window.AddComponent<DesktopWindowModel>();
            DesktopWindowView view = window.AddComponent<DesktopWindowView>();
            DesktopWindowController controller = window.AddComponent<DesktopWindowController>();
            ErrorWindowController errorWindow = window.AddComponent<ErrorWindowController>();

            AssignObject(view, "windowRoot", window.GetComponent<RectTransform>());
            AssignObject(errorWindow, "windowController", controller);
            AssignObject(errorWindow, "model", model);
            AssignObject(errorWindow, "view", view);
            return errorWindow;
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
