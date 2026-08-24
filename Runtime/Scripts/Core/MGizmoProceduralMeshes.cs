using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    /// <summary>
    /// Meshes MGizmos generates rather than ships: shapes that are trivial to build, that Unity has no
    /// built-in primitive for, and that <see cref="MGizmosRendererConfig"/> therefore does not carry.
    /// </summary>
    /// <remarks>
    /// Built once on first use and cached for the session. <c>HideAndDontSave</c> keeps them out of scenes
    /// and builds; a domain reload drops the static reference and the next use simply rebuilds.
    /// </remarks>
    internal static class MGizmoProceduralMeshes
    {
        private const int DiscSegments = 48;

        private static Mesh disc;

        /// <summary>
        /// A filled unit disc in the XZ plane, centred on the origin, radius 1 - scale it to size.
        /// </summary>
        /// <remarks>
        /// Double-sided (the fan is emitted with both windings) so a disc laid on the ground reads from
        /// below as well as above. The unlit gizmo shader culls back faces by default, and a range marker
        /// that vanishes when the camera dips under the floor plane looks like a bug, not a culling mode.
        /// </remarks>
        internal static Mesh Disc
        {
            get
            {
                if (disc == null) disc = BuildDisc();
                return disc;
            }
        }

        private static Mesh BuildDisc()
        {
            //centre vertex + one per segment; triangles fan out from the centre
            var vertices = new Vector3[DiscSegments + 1];
            var triangles = new int[DiscSegments * 3 * 2];

            vertices[0] = Vector3.zero;

            for (int i = 0; i < DiscSegments; i++)
            {
                float radian = (float)i / DiscSegments * 2f * Mathf.PI;
                vertices[i + 1] = new Vector3(Mathf.Cos(radian), 0f, Mathf.Sin(radian));
            }

            int t = 0;
            for (int i = 0; i < DiscSegments; i++)
            {
                int current = i + 1;
                int next = i == DiscSegments - 1 ? 1 : i + 2;

                //top face
                triangles[t++] = 0;
                triangles[t++] = next;
                triangles[t++] = current;

                //bottom face - same fan, reversed winding
                triangles[t++] = 0;
                triangles[t++] = current;
                triangles[t++] = next;
            }

            var mesh = new Mesh
            {
                name = "MGizmoDisc",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
