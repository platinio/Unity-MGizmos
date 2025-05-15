using ArcaneOnyx;
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
            MGizmos.RenderArrow(from.position, to.position, 0.05f, arrowHeadSize)
                .SetMaterial(material);
        }
    }
}