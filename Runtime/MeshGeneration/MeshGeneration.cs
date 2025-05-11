using UnityEngine;


namespace ArcaneOnyx
{
    public static class MeshGeneration
    {
        public static Mesh GenerateCylinder(int numSides, float length, float polygonSideLength)
        {
            var meshData = new MeshData();
            var polygon = new Polygon(numSides, polygonSideLength);
            
            StackPolygon(meshData, polygon, length, 0, length);

            return meshData.CreateMesh();
        }
        
        private static void StackPolygon(MeshData meshData, Polygon polygon, float length, float z1, float z2)
        {
            Vector2[] polyVs = polygon.vertices;
            float[] angularUvs = polygon.angularUvs;

            for (int i1 = 0; i1 < polygon.numSides; i1++)
            {
                int i2 = (i1 + 1) % polyVs.Length;

                float angularUv1 = angularUvs[i1];
                float angularUv2 = angularUvs[i2];

                float zUv1 = z1 / length;
                float zUv2 = z2 / length;

                int idx1 = meshData.AddVertex(new Vector3(polyVs[i1].x, polyVs[i1].y, z1), new Vector2(angularUv1, zUv1));
                int idx2 = meshData.AddVertex(new Vector3(polyVs[i2].x, polyVs[i2].y, z1), new Vector2(angularUv2, zUv1));
                int idx3 = meshData.AddVertex(new Vector3(polyVs[i1].x, polyVs[i1].y, z2), new Vector2(angularUv1, zUv2));
                meshData.AddTriangleIdxs(idx1, idx2, idx3);

                idx1 = meshData.AddVertex(new Vector3(polyVs[i2].x, polyVs[i2].y, z1), new Vector2(angularUv2, zUv1));
                idx2 = meshData.AddVertex(new Vector3(polyVs[i2].x, polyVs[i2].y, z2), new Vector2(angularUv2, zUv2));
                idx3 = meshData.AddVertex(new Vector3(polyVs[i1].x, polyVs[i1].y, z2), new Vector2(angularUv1, zUv2));
                meshData.AddTriangleIdxs(idx1, idx2, idx3);
            }
        }
        
    }
}