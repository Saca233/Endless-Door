using UnityEngine;

namespace OwariNakiTobira
{
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class ParallaxBackgroundController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private ParallaxLayer[] layers = System.Array.Empty<ParallaxLayer>();
        [SerializeField] private bool resetLayersOnEnable = true;

        private Vector3 initialCameraPosition;

        private void OnEnable()
        {
            CacheInitialState();
        }

        private void LateUpdate()
        {
            if (cameraRig == null || layers == null)
            {
                return;
            }

            Vector3 cameraDelta = cameraRig.position - initialCameraPosition;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    layers[i].ApplyCameraDelta(cameraDelta);
                }
            }
        }

        public void SetCameraRig(Transform value)
        {
            cameraRig = value;
            CacheInitialState();
        }

        public void SetLayers(ParallaxLayer[] value)
        {
            layers = value ?? System.Array.Empty<ParallaxLayer>();
            CacheInitialState();
        }

        public void ResetParallax()
        {
            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    layers[i].ResetLayer();
                }
            }

            if (cameraRig != null)
            {
                initialCameraPosition = cameraRig.position;
            }
        }

        private void CacheInitialState()
        {
            if (cameraRig != null)
            {
                initialCameraPosition = cameraRig.position;
            }

            if (layers == null)
            {
                layers = System.Array.Empty<ParallaxLayer>();
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    layers[i].CacheInitialPosition();
                    if (resetLayersOnEnable)
                    {
                        layers[i].ResetLayer();
                    }
                }
            }
        }
    }
}
