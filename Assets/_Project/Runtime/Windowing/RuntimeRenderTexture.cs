using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class RuntimeRenderTexture : MonoBehaviour
    {
        [SerializeField] private GameWindowCamera gameWindowCamera;
        [SerializeField] private RawImage targetRawImage;
        [SerializeField] private GameWindowView gameWindowView;
        [SerializeField] private int depthBufferBits = 24;
        [SerializeField] private RenderTextureFormat colorFormat = RenderTextureFormat.ARGB32;
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

        private RenderTexture renderTexture;

        public RenderTexture RenderTexture => renderTexture;

        private void OnEnable()
        {
            EnsureRenderTexture();
        }

        private void LateUpdate()
        {
            EnsureRenderTexture();
        }

        private void OnDisable()
        {
            ReleaseRenderTexture();
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public void EnsureRenderTexture()
        {
            if (gameWindowCamera == null || gameWindowCamera.GameplayCamera == null)
            {
                return;
            }

            Vector2Int requiredSize = gameWindowCamera.RenderResolution;
            if (!ShouldRecreate(renderTexture, requiredSize))
            {
                AssignTargets();
                return;
            }

            ReleaseRenderTexture();
            renderTexture = new RenderTexture(requiredSize.x, requiredSize.y, depthBufferBits, colorFormat)
            {
                name = "RuntimeGameplayWindowTexture",
                filterMode = filterMode,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            AssignTargets();
        }

        public void ReleaseRenderTexture()
        {
            if (gameWindowCamera != null && gameWindowCamera.GameplayCamera != null && gameWindowCamera.GameplayCamera.targetTexture == renderTexture)
            {
                gameWindowCamera.GameplayCamera.targetTexture = null;
            }

            if (targetRawImage != null && targetRawImage.texture == renderTexture)
            {
                targetRawImage.texture = null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
        }

        public static bool ShouldRecreate(RenderTexture current, Vector2Int requiredSize)
        {
            return current == null || current.width != requiredSize.x || current.height != requiredSize.y;
        }

        private void AssignTargets()
        {
            if (renderTexture == null || gameWindowCamera == null || gameWindowCamera.GameplayCamera == null)
            {
                return;
            }

            gameWindowCamera.GameplayCamera.targetTexture = renderTexture;
            if (targetRawImage != null)
            {
                targetRawImage.texture = renderTexture;
            }

            if (gameWindowView != null)
            {
                gameWindowView.SetRenderResolution(gameWindowCamera.RenderResolution);
            }
        }
    }
}
