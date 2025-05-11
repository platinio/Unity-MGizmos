using UnityEngine;


namespace ArcaneOnyx
{
    public class DebugCollisions : MonoBehaviour
    {
        [SerializeField] private float duration;
        
        private void OnCollisionEnter(Collision other)
        {
            PhysicsDebug.DrawCollision(other, duration);
        }
    }
}
