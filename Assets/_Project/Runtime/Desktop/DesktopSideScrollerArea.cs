using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class DesktopSideScrollerArea : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Vector3 boundsCenter = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 boundsSize = new Vector3(12f, 5f, 0f);
        [SerializeField] private float gameplayPlaneZ;
        [SerializeField] private bool clampInLateUpdate = true;

        public Bounds MovementBounds => new Bounds(boundsCenter, new Vector3(Mathf.Max(0f, boundsSize.x), Mathf.Max(0f, boundsSize.y), 0f));
        public float GameplayPlaneZ => gameplayPlaneZ;

        private void LateUpdate()
        {
            if (clampInLateUpdate)
            {
                ConstrainPlayerNow();
            }
        }

        public void SetPlayerRoot(Transform root)
        {
            playerRoot = root;
        }

        public void SetBounds(Vector3 center, Vector3 size, float planeZ)
        {
            boundsCenter = center;
            boundsSize = size;
            gameplayPlaneZ = planeZ;
        }

        public void ConstrainPlayerNow()
        {
            if (playerRoot == null)
            {
                return;
            }

            playerRoot.position = ClampToBounds(playerRoot.position, MovementBounds, gameplayPlaneZ);
        }

        public static Vector3 ClampToBounds(Vector3 position, Bounds bounds, float planeZ)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            position.z = planeZ;
            return position;
        }
    }
}
