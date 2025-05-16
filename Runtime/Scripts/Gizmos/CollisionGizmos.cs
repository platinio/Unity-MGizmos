using UnityEngine;


namespace ArcaneOnyx
{
    public class CollisionGizmos : MonoBehaviour
    {
        [SerializeField] private bool showOnCollisionEnter;
        [SerializeField] private bool showOnCollisionStay;
        [SerializeField] private bool showOnCollisionExit;
        [SerializeField] private float duration;
        
        private void OnCollisionEnter(Collision other)
        {
            if (!showOnCollisionEnter) return;
            
            var dc = MPhysics.RenderCollision(other).SetDuration(duration);
            MGizmos.AddMeshDrawCall(dc);
        }

        private void OnCollisionStay(Collision other)
        {
            if (!showOnCollisionStay) return;
            
            var dc = MPhysics.RenderCollision(other).SetDuration(duration);
            MGizmos.AddMeshDrawCall(dc);
        }

        private void OnCollisionExit(Collision other)
        {
            if (!showOnCollisionExit) return;
            
            var dc = MPhysics.RenderCollision(other).SetDuration(duration);
            MGizmos.AddMeshDrawCall(dc);
        }
    }
}
