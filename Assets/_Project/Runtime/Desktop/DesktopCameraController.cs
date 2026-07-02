using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopCameraController : MonoBehaviour
    {
        [SerializeField] private Camera desktopCamera;
        [SerializeField] private LayerMask desktopLayers = ~0;
        [SerializeField] private bool orthographic = true;
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);

        public Camera DesktopCamera => desktopCamera;
        public LayerMask DesktopLayers => desktopLayers;

        private void Awake()
        {
            ResolveCamera();
            ApplyCameraSettings();
        }

        private void OnValidate()
        {
            ResolveCamera();
            ApplyCameraSettings();
        }

        public void SetDesktopLayers(LayerMask value)
        {
            desktopLayers = value;
            ApplyCameraSettings();
        }

        private void ResolveCamera()
        {
            if (desktopCamera == null)
            {
                desktopCamera = GetComponent<Camera>();
            }
        }

        private void ApplyCameraSettings()
        {
            if (desktopCamera == null)
            {
                return;
            }

            desktopCamera.orthographic = orthographic;
            desktopCamera.cullingMask = desktopLayers;
            desktopCamera.clearFlags = CameraClearFlags.SolidColor;
            desktopCamera.backgroundColor = backgroundColor;
        }
    }
}
