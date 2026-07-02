using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class WindowPuzzleTargetTests
    {
        [Test]
        public void MultipleEffectSourcesKeepTargetErasedUntilAllRelease()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                WindowPuzzleTarget target = ConfigureTarget(targetObject);
                Renderer renderer = targetObject.GetComponent<Renderer>();
                Collider collider = targetObject.GetComponent<Collider>();

                target.ApplyEffect("rule-a");
                target.ApplyEffect("rule-b");
                target.ReleaseEffect("rule-a");

                Assert.IsFalse(renderer.enabled);
                Assert.IsFalse(collider.enabled);
                Assert.AreEqual(1, target.ActiveSourceCount);

                target.ReleaseEffect("rule-b");

                Assert.IsTrue(renderer.enabled);
                Assert.IsTrue(collider.enabled);
                Assert.AreEqual(0, target.ActiveSourceCount);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void RuntimeResetClearsSourcesAndRestoresInitialState()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                WindowPuzzleTarget target = ConfigureTarget(targetObject);
                Renderer renderer = targetObject.GetComponent<Renderer>();
                Collider collider = targetObject.GetComponent<Collider>();

                target.ApplyEffect("rule-a");

                Assert.IsFalse(renderer.enabled);
                Assert.IsFalse(collider.enabled);

                target.RuntimeReset();

                Assert.IsTrue(renderer.enabled);
                Assert.IsTrue(collider.enabled);
                Assert.AreEqual(0, target.ActiveSourceCount);
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        private static WindowPuzzleTarget ConfigureTarget(GameObject targetObject)
        {
            WindowPuzzleTarget target = targetObject.AddComponent<WindowPuzzleTarget>();
            target.SetAffectedObjects(
                new[] { targetObject.GetComponent<Renderer>() },
                new[] { targetObject.GetComponent<Collider>() });
            return target;
        }
    }
}
