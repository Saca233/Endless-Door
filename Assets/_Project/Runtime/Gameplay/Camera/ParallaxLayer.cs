using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform layerRoot;
        [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0.5f);
        [SerializeField] private bool applyYParallax;

        private Vector3 initialPosition;
        private bool hasInitialPosition;

        public Transform LayerRoot => layerRoot;
        public Vector2 ParallaxFactor => parallaxFactor;
        public bool ApplyYParallax => applyYParallax;

        private void Reset()
        {
            layerRoot = transform;
        }

        private void Awake()
        {
            CacheInitialPosition();
        }

        public void CacheInitialPosition()
        {
            if (layerRoot == null)
            {
                layerRoot = transform;
            }

            initialPosition = layerRoot.position;
            hasInitialPosition = true;
        }

        public void ApplyCameraDelta(Vector3 cameraDelta)
        {
            if (layerRoot == null)
            {
                return;
            }

            if (!hasInitialPosition)
            {
                CacheInitialPosition();
            }

            layerRoot.position = CalculateParallaxPosition(initialPosition, cameraDelta, parallaxFactor, applyYParallax);
        }

        public void ResetLayer()
        {
            if (layerRoot != null && hasInitialPosition)
            {
                layerRoot.position = initialPosition;
            }
        }

        public static Vector3 CalculateParallaxPosition(Vector3 initialPosition, Vector3 cameraDelta, Vector2 factor, bool applyY)
        {
            float x = initialPosition.x + cameraDelta.x * (1f - factor.x);
            float y = applyY ? initialPosition.y + cameraDelta.y * (1f - factor.y) : initialPosition.y;
            return new Vector3(x, y, initialPosition.z);
        }
    }
}
