using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    public class RaycastExample : MonoBehaviour
    {
        [SerializeField] private Transform fromRaycast;
        [SerializeField] private Transform toRaycast;
        
        private void OnDrawGizmos()
        {
            if (fromRaycast == null || toRaycast == null) return;
            
            Ray ray = new Ray(fromRaycast.position, (toRaycast.position - fromRaycast.position).normalized);
            MPhysics.Raycast(ray);
        }
    }

}

