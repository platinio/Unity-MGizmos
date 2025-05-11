using UnityEngine;

namespace ArcaneOnyx
{
    public class Polygon
    {
        public int numSides;
        public Vector2[] vertices;
        public float[] angularUvs;

        public Polygon(int numSides, float sideLength)
        {
            this.numSides = numSides;
            SetVertices(sideLength);
        }

        void SetVertices(float sideLength)
        {
            float angle = 2 * Mathf.PI / numSides;
            vertices = new Vector2[numSides];
            angularUvs = new float[numSides];

            for (int i = 0; i < numSides; i++)
            {
                float x = sideLength * Mathf.Cos(i * angle);
                float y = sideLength * Mathf.Sin(i * angle);
                vertices[i] = new Vector2(x, y);
                angularUvs[i] = (float)i / numSides;
            }
        }
    }
}