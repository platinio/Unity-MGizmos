using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    /// <summary>
    /// Many coloured lines and triangles in world space, drawn as two meshes instead of one draw call each.
    /// Fill it, then draw it with <see cref="MGizmos.RenderBatch"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A per-element gizmo costs a draw call and a pooled object; a physics engine's debug picture is tens
    /// of thousands of segments a frame. A batch keeps its vertex lists between frames, so refilling it
    /// allocates nothing once it has grown to the picture's size, and it uploads only when its contents
    /// changed. Lines are one pixel wide (a lines mesh); the per-element <c>RenderLine</c> keeps its width.
    /// </para>
    /// <para>
    /// The draw call <see cref="MGizmos.RenderBatch"/> returns shows the batch as it is when drawn, so a
    /// batch that is cleared and refilled every frame needs nothing more; a timed picture that must outlive
    /// the next refill wants its own batch. Dispose the batch when its owner goes away: it owns two meshes.
    /// </para>
    /// </remarks>
    public sealed class MGizmoBatch : System.IDisposable
    {
        private readonly Part lines = new("MGizmoBatch Lines", MeshTopology.Lines);

        private readonly Part triangles = new("MGizmoBatch Triangles", MeshTopology.Triangles);

        public int LineCount => lines.ElementCount;

        public int TriangleCount => triangles.ElementCount;

        public bool IsEmpty => LineCount == 0 && TriangleCount == 0;

        internal Mesh LineMesh => lines.Mesh;

        internal Mesh TriangleMesh => triangles.Mesh;

        public void Clear()
        {
            lines.Clear();
            triangles.Clear();
        }

        public void AddLine(Vector3 from, Vector3 to, Color32 color)
        {
            lines.AddVertex(from, color);
            lines.AddVertex(to, color);
        }

        public void AddTriangle(Vector3 first, Vector3 second, Vector3 third, Color32 color)
        {
            triangles.AddVertex(first, color);
            triangles.AddVertex(second, color);
            triangles.AddVertex(third, color);
        }

        public void Dispose()
        {
            lines.Dispose();
            triangles.Dispose();
        }

        internal void UploadChanges()
        {
            lines.UploadChanges();
            triangles.UploadChanges();
        }

        //One mesh per topology: a draw call renders a single submesh, and lines and triangles cannot share one.
        //Plain arrays rather than lists: a debug picture adds hundreds of thousands of vertices a frame, and
        //no vertex is shared, so vertex i is always index i and the index buffer is a count-up kept between frames.
        private sealed class Part
        {
            private const int InitialCapacity = 1024;

            private readonly string meshName;

            private readonly MeshTopology topology;

            private Vector3[] vertices = new Vector3[InitialCapacity];

            private Color32[] colors = new Color32[InitialCapacity];

            private int[] countingIndices = System.Array.Empty<int>();

            private int vertexCount;

            private bool hasChanged;

            public Part(string meshName, MeshTopology topology)
            {
                this.meshName = meshName;
                this.topology = topology;
            }

            public Mesh Mesh { get; private set; }

            public int ElementCount => vertexCount / VerticesPerElement;

            private int VerticesPerElement => topology == MeshTopology.Lines ? 2 : 3;

            public void Clear()
            {
                hasChanged |= vertexCount > 0;
                vertexCount = 0;
            }

            public void AddVertex(Vector3 position, Color32 color)
            {
                if (vertexCount == vertices.Length)
                {
                    System.Array.Resize(ref vertices, vertices.Length * 2);
                    System.Array.Resize(ref colors, colors.Length * 2);
                }

                vertices[vertexCount] = position;
                colors[vertexCount] = color;
                vertexCount++;
                hasChanged = true;
            }

            public void UploadChanges()
            {
                if (!hasChanged)
                {
                    return;
                }

                hasChanged = false;
                Mesh ??= CreateMesh();
                Mesh.Clear();
                Mesh.SetVertices(vertices, 0, vertexCount);
                Mesh.SetColors(colors, 0, vertexCount);
                Mesh.SetIndices(CountingIndicesUpTo(vertexCount), 0, vertexCount, topology, 0);
            }

            public void Dispose()
            {
                if (Mesh == null)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(Mesh);
                }
                else
                {
                    Object.DestroyImmediate(Mesh);
                }

                Mesh = null;
            }

            private int[] CountingIndicesUpTo(int count)
            {
                if (countingIndices.Length < count)
                {
                    countingIndices = new int[Mathf.NextPowerOfTwo(count)];
                    for (var index = 0; index < countingIndices.Length; index++)
                    {
                        countingIndices[index] = index;
                    }
                }

                return countingIndices;
            }

            //Thirty-two-bit indices: a debug picture passes 65 535 vertices long before it stops being useful.
            private Mesh CreateMesh()
            {
                var mesh = new Mesh { name = meshName, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.HideAndDontSave };
                mesh.MarkDynamic();
                return mesh;
            }
        }
    }
}
