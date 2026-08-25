using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    /// <summary>
    /// The one asset MGizmos loads from Resources: how gizmos are shaded, not what they are shaped like.
    /// </summary>
    /// <remarks>
    /// It used to carry every primitive mesh. Those are generated in
    /// <see cref="MGizmoProceduralMeshes"/> now, which leaves nothing here pointing outside the module -
    /// the references it held were unversioned file IDs into Unity's own built-in resource library.
    /// </remarks>
    [CreateAssetMenu(menuName = "Debug Mesh Renderer Config")]
    public class MGizmosRendererConfig : ScriptableSingleton<MGizmosRendererConfig>
    {
        [Header("Config")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Color defaultColor;

        [Header("Text")]
        [SerializeField, Tooltip("Font used by MGizmos.RenderText. Leave empty to fall back to a runtime " +
            "dynamic Arial. A dynamic (not static) font is required.")]
        private Font textFont;

        public Material DefaultMaterial => defaultMaterial;
        public Color DefaultColor => defaultColor;
        public Font TextFont => textFont;
    }
}