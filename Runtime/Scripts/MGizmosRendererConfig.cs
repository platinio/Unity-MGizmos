using ArcaneOnyx;
using UnityEngine;

namespace ArcaneOnyx
{
    [CreateAssetMenu(menuName = "Debug Mesh Renderer Config")]
    public class MGizmosRendererConfig : ScriptableSingleton<MGizmosRendererConfig>
    {
        [Header("Config")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Color defaultColor;
        
        [Header("Meshes")]
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh cylinderMesh;
        [SerializeField] private Mesh planeMesh;
        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Mesh arrowHead;

        public Material DefaultMaterial => defaultMaterial;
        public Color DefaultColor => defaultColor;
        
        public Mesh SphereMesh => sphereMesh;
        public Mesh CapsuleMesh => capsuleMesh;
        public Mesh CubeMesh => cubeMesh;
        public Mesh CylinderMesh => cylinderMesh;
        public Mesh PlaneMesh => planeMesh;
        public Mesh QuadMesh => quadMesh;
        public Mesh ArrowHead => arrowHead;
    }
}