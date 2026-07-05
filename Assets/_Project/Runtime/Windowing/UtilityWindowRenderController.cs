using UnityEngine;
using UnityEngine.UI;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class UtilityWindowRenderController : MonoBehaviour
    {
        [SerializeField] private Camera sourceGameplayCamera;
        [SerializeField] private Camera utilityViewCamera;
        [SerializeField] private RawImage targetRawImage;
        [SerializeField] private Vector2Int renderResolution = new Vector2Int(512, 288);
        [SerializeField] private LayerMask utilityCullingMask = ~0;
        [SerializeField] private int depthBufferBits = 24;
        [SerializeField] private RenderTextureFormat colorFormat = RenderTextureFormat.ARGB32;
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

        private RenderTexture renderTexture;

        public Camera SourceGameplayCamera => sourceGameplayCamera;
        public Camera UtilityViewCamera => utilityViewCamera;
        public RawImage TargetRawImage => targetRawImage;
        public RenderTexture RenderTexture => renderTexture;
        public Vector2Int RenderResolution => renderResolution;
        public LayerMask UtilityCullingMask => utilityCullingMask;

        private void OnEnable()
        {
            EnsureRenderTexture();
            SynchronizeCamera();
        }

        private void LateUpdate()
        {
            EnsureRenderTexture();
            SynchronizeCamera();
        }

        private void OnDisable()
        {
            ReleaseRenderTexture();
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public void SetRenderResolution(Vector2Int value)
        {
            renderResolution = SanitizeResolution(value);
        }

        public void SetUtilityCullingMask(LayerMask value)
        {
            utilityCullingMask = value;
            if (utilityViewCamera != null)
            {
                utilityViewCamera.cullingMask = utilityCullingMask;
            }
        }

        public void EnsureRenderTexture()
        {
            renderResolution = SanitizeResolution(renderResolution);
            if (utilityViewCamera == null || targetRawImage == null)
            {
                return;
            }

            if (!ShouldRecreate(renderTexture, renderResolution))
            {
                AssignTargets();
                return;
            }

            ReleaseRenderTexture();
            renderTexture = new RenderTexture(renderResolution.x, renderResolution.y, depthBufferBits, colorFormat)
            {
                name = "RuntimeUtilityWindowTexture",
                filterMode = filterMode,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            AssignTargets();
        }

        public void SynchronizeCamera()
        {
            if (sourceGameplayCamera == null || utilityViewCamera == null)
            {
                return;
            }

            UtilityCameraSyncSnapshot snapshot = CreateSynchronizationSnapshot(sourceGameplayCamera);
            ApplySynchronizationSnapshot(utilityViewCamera, snapshot, utilityCullingMask);
        }

        public void ReleaseRenderTexture()
        {
            if (utilityViewCamera != null && utilityViewCamera.targetTexture == renderTexture)
            {
                utilityViewCamera.targetTexture = null;
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
            Vector2Int sanitized = SanitizeResolution(requiredSize);
            return current == null || current.width != sanitized.x || current.height != sanitized.y;
        }

        public static UtilityCameraSyncSnapshot CreateSynchronizationSnapshot(Camera sourceCamera)
        {
            if (sourceCamera == null)
            {
                return UtilityCameraSyncSnapshot.Default;
            }

            return new UtilityCameraSyncSnapshot(
                sourceCamera.transform.position,
                sourceCamera.transform.rotation,
                sourceCamera.orthographic,
                sourceCamera.orthographicSize,
                sourceCamera.fieldOfView,
                sourceCamera.aspect,
                sourceCamera.nearClipPlane,
                sourceCamera.farClipPlane,
                sourceCamera.clearFlags,
                sourceCamera.backgroundColor,
                sourceCamera.allowHDR,
                sourceCamera.allowMSAA);
        }

        public static void ApplySynchronizationSnapshot(Camera targetCamera, UtilityCameraSyncSnapshot snapshot, LayerMask cullingMask)
        {
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation);
            targetCamera.orthographic = snapshot.Orthographic;
            targetCamera.orthographicSize = snapshot.OrthographicSize;
            targetCamera.fieldOfView = snapshot.FieldOfView;
            targetCamera.aspect = snapshot.Aspect;
            targetCamera.nearClipPlane = snapshot.NearClipPlane;
            targetCamera.farClipPlane = snapshot.FarClipPlane;
            targetCamera.clearFlags = snapshot.ClearFlags;
            targetCamera.backgroundColor = snapshot.BackgroundColor;
            targetCamera.allowHDR = snapshot.AllowHDR;
            targetCamera.allowMSAA = snapshot.AllowMSAA;
            targetCamera.cullingMask = cullingMask;
        }

        private static Vector2Int SanitizeResolution(Vector2Int value)
        {
            return new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
        }

        private void AssignTargets()
        {
            if (renderTexture == null || utilityViewCamera == null)
            {
                return;
            }

            utilityViewCamera.targetTexture = renderTexture;
            utilityViewCamera.cullingMask = utilityCullingMask;
            if (targetRawImage != null)
            {
                targetRawImage.texture = renderTexture;
            }
        }
    }

    public readonly struct UtilityCameraSyncSnapshot
    {
        public UtilityCameraSyncSnapshot(
            Vector3 position,
            Quaternion rotation,
            bool orthographic,
            float orthographicSize,
            float fieldOfView,
            float aspect,
            float nearClipPlane,
            float farClipPlane,
            CameraClearFlags clearFlags,
            Color backgroundColor,
            bool allowHDR,
            bool allowMSAA)
        {
            Position = position;
            Rotation = rotation;
            Orthographic = orthographic;
            OrthographicSize = orthographicSize;
            FieldOfView = fieldOfView;
            Aspect = aspect;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
            ClearFlags = clearFlags;
            BackgroundColor = backgroundColor;
            AllowHDR = allowHDR;
            AllowMSAA = allowMSAA;
        }

        public static UtilityCameraSyncSnapshot Default => new UtilityCameraSyncSnapshot(
            Vector3.zero,
            Quaternion.identity,
            true,
            5f,
            60f,
            16f / 9f,
            0.3f,
            1000f,
            CameraClearFlags.SolidColor,
            Color.black,
            false,
            false);

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Orthographic { get; }
        public float OrthographicSize { get; }
        public float FieldOfView { get; }
        public float Aspect { get; }
        public float NearClipPlane { get; }
        public float FarClipPlane { get; }
        public CameraClearFlags ClearFlags { get; }
        public Color BackgroundColor { get; }
        public bool AllowHDR { get; }
        public bool AllowMSAA { get; }
    }
}
