using UnityEngine;

namespace ArcaneOnyx
{
    public class ArrowSample : MonoBehaviour
    {
        [SerializeField] private Transform from;
        [SerializeField] private Transform to;
        [SerializeField] private float arrowHeadSize;
        [SerializeField] private Material material;
    
        private void OnDrawGizmos()
        {
            if (from == null || to == null) return;
            
            var dc = MGizmos.RenderArrow(from.position, to.position, 0.05f, arrowHeadSize);
            dc.SetMaterial(material);
            MGizmos.AddMeshDrawCall(dc);
        }
    }
}