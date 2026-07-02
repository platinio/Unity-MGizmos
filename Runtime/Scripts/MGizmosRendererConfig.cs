using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
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

        [Header("Text")]
        [SerializeField, Tooltip("Font used by MGizmos.RenderText. Leave empty to fall back to a runtime " +
            "dynamic Arial. A dynamic (not static) font is required.")]
        private Font textFont;

        public Material DefaultMaterial => defaultMaterial;
        public Color DefaultColor => defaultColor;
        
        public Mesh SphereMesh => sphereMesh;
        public Mesh CapsuleMesh => capsuleMesh;
        public Mesh CubeMesh => cubeMesh;
        public Mesh CylinderMesh => cylinderMesh;
        public Mesh PlaneMesh => planeMesh;
        public Mesh QuadMesh => quadMesh;
        public Mesh ArrowHead => arrowHead;
        public Font TextFont => textFont;
    }
}