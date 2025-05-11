using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx
{
    public class MeshData
    {
        public Vector3[] vertices;
        public int[] triangleIdxs;
        public Vector2[] uvs;

        private int lastVertexIdx = -1;

        public MeshData()
        {
            Reset();
        }

        public void Reset()
        {
            vertices = new Vector3[0];
            triangleIdxs = new int[0];
            uvs = new Vector2[0];
            lastVertexIdx = -1;
        }

        public int AddVertex(Vector3 v, Vector2 uv)
        {
            System.Array.Resize(ref vertices, vertices.Length + 1);
            System.Array.Resize(ref uvs, uvs.Length + 1);

            vertices[lastVertexIdx + 1] = v;
            uvs[lastVertexIdx + 1] = uv;

            lastVertexIdx++;
            return lastVertexIdx;
        }

        public void AddTriangleIdxs(int i1, int i2, int i3)
        {
            int oldLength = triangleIdxs.Length;
            System.Array.Resize(ref triangleIdxs, oldLength + 3);

            triangleIdxs[oldLength] = i1;
            triangleIdxs[oldLength + 1] = i2;
            triangleIdxs[oldLength + 2] = i3;
        }

        public Triangle[] Triangles
        {
            get
            {
                int triangleCount = triangleIdxs.Length / 3;
                Triangle[] triangleArray = new Triangle[triangleCount];
                for (int i = 0; i < triangleCount; i++)
                {
                    int idx = i * 3;
                    triangleArray[i] = new Triangle(vertices[triangleIdxs[idx]], vertices[triangleIdxs[idx + 1]], vertices[triangleIdxs[idx + 2]]);
                }
                return triangleArray;
            }
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangleIdxs,
                uv = uvs
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}

