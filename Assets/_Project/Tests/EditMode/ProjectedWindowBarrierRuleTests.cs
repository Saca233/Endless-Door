using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class ProjectedWindowBarrierRuleTests
    {
        [Test]
        public void BarrierEnablesWhenWindowOverlapsMainGameWindow()
        {
            GameObject cameraObject = new GameObject("Camera");
            GameObject barrierObject = new GameObject("Barrier");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.aspect = 2f;
                BoxCollider collider = barrierObject.AddComponent<BoxCollider>();
                collider.enabled = false;
                ProjectedWindowBarrierRule rule = barrierObject.AddComponent<ProjectedWindowBarrierRule>();
                rule.SetReferences(null, null, camera, collider);

                bool active = rule.ApplyBarrierForScreenRects(
                    new Rect(50f, 25f, 75f, 50f),
                    new Rect(0f, 0f, 200f, 100f));

                Assert.IsTrue(active);
                Assert.IsTrue(collider.enabled);
                Assert.Greater(collider.size.x, 0f);
                Assert.Greater(collider.size.y, 0f);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(barrierObject);
            }
        }

        [Test]
        public void BarrierDisablesWhenWindowDoesNotOverlapMainGameWindow()
        {
            GameObject cameraObject = new GameObject("Camera");
            GameObject barrierObject = new GameObject("Barrier");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                BoxCollider collider = barrierObject.AddComponent<BoxCollider>();
                collider.enabled = true;
                ProjectedWindowBarrierRule rule = barrierObject.AddComponent<ProjectedWindowBarrierRule>();
                rule.SetReferences(null, null, camera, collider);

                bool active = rule.ApplyBarrierForScreenRects(
                    new Rect(250f, 25f, 75f, 50f),
                    new Rect(0f, 0f, 200f, 100f));

                Assert.IsFalse(active);
                Assert.IsFalse(collider.enabled);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(barrierObject);
            }
        }
    }
}
