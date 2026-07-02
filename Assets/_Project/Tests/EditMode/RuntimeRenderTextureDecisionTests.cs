using NUnit.Framework;
using UnityEngine;

namespace OwariNakiTobira.Tests
{
    public sealed class RuntimeRenderTextureDecisionTests
    {
        [Test]
        public void NullRenderTextureRequiresCreation()
        {
            Assert.IsTrue(RuntimeRenderTexture.ShouldRecreate(null, new Vector2Int(1280, 720)));
        }

        [Test]
        public void MatchingSizeDoesNotRequireRecreation()
        {
            RenderTexture texture = new RenderTexture(1280, 720, 24);

            Assert.IsFalse(RuntimeRenderTexture.ShouldRecreate(texture, new Vector2Int(1280, 720)));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SizeChangeRequiresRecreation()
        {
            RenderTexture texture = new RenderTexture(1280, 720, 24);

            Assert.IsTrue(RuntimeRenderTexture.ShouldRecreate(texture, new Vector2Int(1920, 1080)));

            Object.DestroyImmediate(texture);
        }
    }
}
