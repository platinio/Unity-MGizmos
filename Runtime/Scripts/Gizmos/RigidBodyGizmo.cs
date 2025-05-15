using UnityEngine;

namespace ArcaneOnyx
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
            float v = MPhysics.Duration;
            
            MPhysics.Duration = 0;
            MPhysics.DebugRigidBody(rb, velocityScaler);
            MPhysics.Duration = v;
        }
    }
}