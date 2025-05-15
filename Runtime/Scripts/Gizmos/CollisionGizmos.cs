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
            MPhysics.DrawCollision(other).SetDuration(duration);
        }

        private void OnCollisionStay(Collision other)
        {
            if (!showOnCollisionStay) return;
            MPhysics.DrawCollision(other).SetDuration(duration);
        }

        private void OnCollisionExit(Collision other)
        {
            if (!showOnCollisionExit) return;
            MPhysics.DrawCollision(other).SetDuration(duration);
        }
    }
}
