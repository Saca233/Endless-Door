using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class UtilityWindowViewportMapperTests
    {
        [Test]
        public void FullOverlapMapsToFullUv()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, main, out Rect uv);

            Assert.IsTrue(mapped);
            AssertRect(uv, 0f, 0f, 1f, 1f);
        }

        [Test]
        public void PartialOverlapMapsIntersectionOnly()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(500f, 100f, 800f, 450f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            AssertRect(uv, 0.5f, 0f, 0.5f, 1f);
        }

        [Test]
        public void NoOverlapReturnsFalse()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(1000f, 100f, 200f, 120f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsFalse(mapped);
            Assert.AreEqual(Rect.zero, uv);
        }

        [Test]
        public void LeftSideMapsToLeftUv()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(100f, 160f, 200f, 180f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            Assert.AreEqual(0f, uv.xMin, 0.0001f);
            Assert.AreEqual(0.25f, uv.xMax, 0.0001f);
        }

        [Test]
        public void RightSideMapsToRightUv()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(700f, 160f, 200f, 180f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            Assert.AreEqual(0.75f, uv.xMin, 0.0001f);
            Assert.AreEqual(1f, uv.xMax, 0.0001f);
        }

        [Test]
        public void BottomSideMapsToBottomUv()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(250f, 100f, 200f, 90f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            Assert.AreEqual(0f, uv.yMin, 0.0001f);
            Assert.AreEqual(0.2f, uv.yMax, 0.0001f);
        }

        [Test]
        public void TopSideMapsToTopUv()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(250f, 460f, 200f, 90f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            Assert.AreEqual(0.8f, uv.yMin, 0.0001f);
            Assert.AreEqual(1f, uv.yMax, 0.0001f);
        }

        [Test]
        public void UvRectangleIsClampedToMainGameWindow()
        {
            Rect main = new Rect(100f, 100f, 800f, 450f);
            Rect utility = new Rect(0f, 0f, 250f, 200f);

            bool mapped = UtilityWindowViewportMapper.TryCalculateMappedUv(main, utility, out Rect uv);

            Assert.IsTrue(mapped);
            Assert.GreaterOrEqual(uv.xMin, 0f);
            Assert.GreaterOrEqual(uv.yMin, 0f);
            Assert.LessOrEqual(uv.xMax, 1f);
            Assert.LessOrEqual(uv.yMax, 1f);
        }

        [Test]
        public void CameraSynchronizationSnapshotCapturesSourceSettings()
        {
            GameObject sourceObject = new GameObject("SourceCamera");
            try
            {
                sourceObject.transform.SetPositionAndRotation(new Vector3(3f, 4f, -12f), Quaternion.Euler(5f, 10f, 0f));
                Camera source = sourceObject.AddComponent<Camera>();
                source.orthographic = true;
                source.orthographicSize = 6.5f;
                source.nearClipPlane = 0.2f;
                source.farClipPlane = 120f;
                source.clearFlags = CameraClearFlags.SolidColor;
                source.backgroundColor = Color.cyan;

                UtilityCameraSyncSnapshot snapshot = UtilityWindowRenderController.CreateSynchronizationSnapshot(source);

                Assert.AreEqual(source.transform.position, snapshot.Position);
                Assert.AreEqual(source.transform.rotation, snapshot.Rotation);
                Assert.IsTrue(snapshot.Orthographic);
                Assert.AreEqual(6.5f, snapshot.OrthographicSize, 0.0001f);
                Assert.AreEqual(0.2f, snapshot.NearClipPlane, 0.0001f);
                Assert.AreEqual(120f, snapshot.FarClipPlane, 0.0001f);
                Assert.AreEqual(CameraClearFlags.SolidColor, snapshot.ClearFlags);
                Assert.AreEqual(Color.cyan, snapshot.BackgroundColor);
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
            }
        }

        [Test]
        public void UtilityRenderTextureRecreatesOnlyWhenResolutionChanges()
        {
            RenderTexture texture = new RenderTexture(512, 288, 24);
            try
            {
                Assert.IsFalse(UtilityWindowRenderController.ShouldRecreate(texture, new Vector2Int(512, 288)));
                Assert.IsTrue(UtilityWindowRenderController.ShouldRecreate(texture, new Vector2Int(640, 360)));
            }
            finally
            {
                texture.Release();
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CoverToEraseThresholdsRemainStable()
        {
            Assert.IsTrue(CoverToEraseRule.EvaluateHysteresis(false, 0.55f, 0.55f, 0.45f));
            Assert.IsFalse(CoverToEraseRule.EvaluateHysteresis(true, 0.45f, 0.55f, 0.45f));
            Assert.IsTrue(CoverToEraseRule.EvaluateHysteresis(true, 0.5f, 0.55f, 0.45f));
            Assert.IsFalse(CoverToEraseRule.EvaluateHysteresis(false, 0.5f, 0.55f, 0.45f));
        }

        private static void AssertRect(Rect rect, float x, float y, float width, float height)
        {
            Assert.AreEqual(x, rect.x, 0.0001f);
            Assert.AreEqual(y, rect.y, 0.0001f);
            Assert.AreEqual(width, rect.width, 0.0001f);
            Assert.AreEqual(height, rect.height, 0.0001f);
        }
    }
}
