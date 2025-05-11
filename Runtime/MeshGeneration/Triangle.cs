using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx
{
    public class Triangle 
    {
        private Vector3 vertex1;
        private Vector3 vertex2;
        private Vector3 vertex3;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            vertex1 = v1;
            vertex2 = v2;
            vertex3 = v3;
        }
    }
}

