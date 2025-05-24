using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    public class RigidBodyGizmo : MonoBehaviour
    {
        [SerializeField] private float velocityScaler;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            var dc = MPhysics.RenderRigidBody(rb, velocityScaler);
            MGizmos.AddMeshDrawCall(dc);
        }
    }
}