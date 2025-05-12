using Platinio;
using UnityEngine;

namespace ArcaneOnyx
{
    public class CircleRender : MonoBehaviour
    {
        
        [SerializeField] private int sides;
        [SerializeField] private float radius;
        [SerializeField] private float lineWidth;
        [SerializeField] private Material material;
        [SerializeField] private Color color;
        
        private void OnDrawGizmos()
        {
            var dc = DebugMeshRenderer.Instance.RenderCircle(transform.position, sides, radius);
            dc.SetMaterial(material);
            dc.SetColor(color);
        }
    }
}