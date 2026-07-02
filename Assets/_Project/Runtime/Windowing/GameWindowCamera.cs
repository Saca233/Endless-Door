using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class GameWindowCamera : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private LayerMask gameplayLayers = ~0;
        [SerializeField] private Vector2Int renderResolution = new Vector2Int(1280, 720);
        [SerializeField] private float gameplayPlaneZ;

        public Camera GameplayCamera => gameplayCamera;
        public LayerMask GameplayLayers => gameplayLayers;
        public Vector2Int RenderResolution => renderResolution;

        private void Awake()
        {
            ResolveCamera();
            ApplyCameraSettings();
        }

        private void OnValidate()
        {
            renderResolution = new Vector2Int(Mathf.Max(1, renderResolution.x), Mathf.Max(1, renderResolution.y));
            ResolveCamera();
            ApplyCameraSettings();
        }

        public Vector2 WorldToViewportPoint(Vector3 worldPosition)
        {
            ResolveCamera();
            Vector3 viewport = gameplayCamera.WorldToViewportPoint(worldPosition);
            return new Vector2(viewport.x, viewport.y);
        }

        public Ray ViewportPointToRay(Vector2 viewportPoint)
        {
            ResolveCamera();
            return gameplayCamera.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0f));
        }

        public bool TryViewportToWorldOnGameplayPlane(Vector2 viewportPoint, out Vector3 worldPosition)
        {
            Ray ray = ViewportPointToRay(viewportPoint);
            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, gameplayPlaneZ));
            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        public void SetRenderResolution(Vector2Int value)
        {
            renderResolution = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
        }

        public void SetGameplayLayers(LayerMask value)
        {
            gameplayLayers = value;
            ApplyCameraSettings();
        }

        private void ResolveCamera()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = GetComponent<Camera>();
            }
        }

        private void ApplyCameraSettings()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            gameplayCamera.orthographic = true;
            gameplayCamera.cullingMask = gameplayLayers;
        }
    }
}
