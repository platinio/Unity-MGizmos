using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    public static partial class MGizmos
    {
        private const float DefaultLineWidth = 0.01f;

        private const int SegmentsPerRing = 24;

        //below this the two sphere centres of a capsule are one point, and Vector3.normalized gives no axis
        private const float ShortestCapsuleAxisSquared = 1e-10f;

        private const float AxisCloseToVertical = 0.9f;

        private static readonly float[] BottomAndTop = { -1f, 1f };

        private static bool CanRender => IsEnable && Config != null;

        public static MGizmoBaseDrawCall RenderWireCube(Vector3 center, Quaternion rotation, Vector3 size) => RenderWireCube(center, rotation, size, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireCube(Vector3 center, Quaternion rotation, Vector3 size, float lineWidth)
        {
            if (!CanRender) return inertDrawCall;

            var outline = new WireOutline(lineWidth);
            Vector3 halfSize = size * 0.5f;

            Vector3 Corner(float x, float y, float z) => center + rotation * Vector3.Scale(halfSize, new Vector3(x, y, z));

            foreach (float y in BottomAndTop)
            {
                outline.AddLine(Corner(-1f, y, -1f), Corner(1f, y, -1f));
                outline.AddLine(Corner(1f, y, -1f), Corner(1f, y, 1f));
                outline.AddLine(Corner(1f, y, 1f), Corner(-1f, y, 1f));
                outline.AddLine(Corner(-1f, y, 1f), Corner(-1f, y, -1f));
            }

            outline.AddLine(Corner(-1f, -1f, -1f), Corner(-1f, 1f, -1f));
            outline.AddLine(Corner(1f, -1f, -1f), Corner(1f, 1f, -1f));
            outline.AddLine(Corner(1f, -1f, 1f), Corner(1f, 1f, 1f));
            outline.AddLine(Corner(-1f, -1f, 1f), Corner(-1f, 1f, 1f));

            return outline.Finish();
        }

        public static MGizmoBaseDrawCall RenderWireSphere(Vector3 center, Quaternion rotation, float radius) => RenderWireSphere(center, rotation, radius, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireSphere(Vector3 center, Quaternion rotation, float radius, float lineWidth)
        {
            if (!CanRender) return inertDrawCall;

            var outline = new WireOutline(lineWidth);
            AddSphereRings(outline, center, rotation, radius);
            return outline.Finish();
        }

        public static MGizmoBaseDrawCall RenderWireCylinder(Vector3 center, Quaternion rotation, float radius, float height) => RenderWireCylinder(center, rotation, radius, height, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireCylinder(Vector3 center, Quaternion rotation, float radius, float height, float lineWidth)
        {
            if (!CanRender) return inertDrawCall;

            var outline = new WireOutline(lineWidth);
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 halfAxis = rotation * Vector3.up * (height * 0.5f);

            outline.AddRing(center - halfAxis, right, forward, radius);
            outline.AddRing(center + halfAxis, right, forward, radius);
            void AddSideLine(Vector3 outward) => outline.AddLine(center - halfAxis + outward * radius, center + halfAxis + outward * radius);

            AddSideLine(right);
            AddSideLine(-right);
            AddSideLine(forward);
            AddSideLine(-forward);

            return outline.Finish();
        }

        public static MGizmoBaseDrawCall RenderWireCapsule(Vector3 bottomSphereCenter, Vector3 topSphereCenter, float radius) => RenderWireTaperedCapsule(bottomSphereCenter, topSphereCenter, radius, radius, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireCapsule(Vector3 bottomSphereCenter, Vector3 topSphereCenter, float radius, float lineWidth) => RenderWireTaperedCapsule(bottomSphereCenter, topSphereCenter, radius, radius, lineWidth);

        public static MGizmoBaseDrawCall RenderWireTaperedCapsule(Vector3 bottomSphereCenter, Vector3 topSphereCenter, float bottomRadius, float topRadius) => RenderWireTaperedCapsule(bottomSphereCenter, topSphereCenter, bottomRadius, topRadius, DefaultLineWidth);

        //The surface of two spheres joined by the cone that touches both. The cone meets each sphere above or
        //below its equator when the radii differ, so the rings and side lines sit at those tangent latitudes
        //and the larger end's dome covers more than half a sphere. When one sphere holds the other, there is
        //no cone and the shape is the larger sphere.
        public static MGizmoBaseDrawCall RenderWireTaperedCapsule(Vector3 bottomSphereCenter, Vector3 topSphereCenter, float bottomRadius, float topRadius, float lineWidth)
        {
            if (!CanRender) return inertDrawCall;

            var outline = new WireOutline(lineWidth);
            Vector3 axis = topSphereCenter - bottomSphereCenter;
            float axisLength = axis.magnitude;
            if (axis.sqrMagnitude < ShortestCapsuleAxisSquared || Mathf.Abs(bottomRadius - topRadius) >= axisLength)
            {
                bool isBottomLarger = bottomRadius >= topRadius;
                AddSphereRings(outline, isBottomLarger ? bottomSphereCenter : topSphereCenter, Quaternion.identity, Mathf.Max(bottomRadius, topRadius));
                return outline.Finish();
            }

            Vector3 up = axis / axisLength;
            Vector3 reference = Mathf.Abs(up.y) < AxisCloseToVertical ? Vector3.up : Vector3.right;
            Vector3 firstAcross = Vector3.Cross(up, reference).normalized;
            Vector3 secondAcross = Vector3.Cross(up, firstAcross);

            float sinTangentLatitude = (bottomRadius - topRadius) / axisLength;
            float cosTangentLatitude = Mathf.Sqrt(1f - sinTangentLatitude * sinTangentLatitude);
            float tangentLatitude = Mathf.Asin(sinTangentLatitude);

            outline.AddRing(bottomSphereCenter + up * (bottomRadius * sinTangentLatitude), firstAcross, secondAcross, bottomRadius * cosTangentLatitude);
            outline.AddRing(topSphereCenter + up * (topRadius * sinTangentLatitude), firstAcross, secondAcross, topRadius * cosTangentLatitude);

            void AddSideLinesAndDomes(Vector3 across)
            {
                foreach (float side in BottomAndTop)
                {
                    Vector3 tangentDirection = across * (side * cosTangentLatitude) + up * sinTangentLatitude;
                    outline.AddLine(bottomSphereCenter + tangentDirection * bottomRadius, topSphereCenter + tangentDirection * topRadius);
                }

                Vector3 tangentOnThisSide = across * cosTangentLatitude + up * sinTangentLatitude;
                Vector3 turningUp = up * cosTangentLatitude - across * sinTangentLatitude;
                outline.AddArc(topSphereCenter, tangentOnThisSide, turningUp, topRadius, Mathf.PI - 2f * tangentLatitude);
                outline.AddArc(bottomSphereCenter, tangentOnThisSide, -turningUp, bottomRadius, Mathf.PI + 2f * tangentLatitude);
            }

            AddSideLinesAndDomes(firstAcross);
            AddSideLinesAndDomes(secondAcross);

            return outline.Finish();
        }

        private static void AddSphereRings(WireOutline outline, Vector3 center, Quaternion rotation, float radius)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            outline.AddRing(center, right, forward, radius);
            outline.AddRing(center, right, up, radius);
            outline.AddRing(center, up, forward, radius);
        }

        private readonly struct WireOutline
        {
            private readonly MGizmoCompositeDrawCall lines;

            private readonly float lineWidth;

            public WireOutline(float lineWidth)
            {
                lines = MGizmoCompositeDrawCall.Get();
                this.lineWidth = lineWidth;
            }

            public void AddLine(Vector3 from, Vector3 to)
            {
                lines.AddDrawCall(RenderLine(from, to, lineWidth));
            }

            public void AddRing(Vector3 center, Vector3 firstAxis, Vector3 secondAxis, float radius)
            {
                AddArc(center, firstAxis, secondAxis, radius, 2f * Mathf.PI);
            }

            //From startAxis towards turnAxis, both unit length and at right angles.
            public void AddArc(Vector3 center, Vector3 startAxis, Vector3 turnAxis, float radius, float sweep)
            {
                int segments = Mathf.Max(1, Mathf.CeilToInt(SegmentsPerRing * sweep / (2f * Mathf.PI)));
                Vector3 previous = center + startAxis * radius;
                for (int segment = 1; segment <= segments; segment++)
                {
                    float angle = sweep * segment / segments;
                    Vector3 current = center + (startAxis * Mathf.Cos(angle) + turnAxis * Mathf.Sin(angle)) * radius;
                    AddLine(previous, current);
                    previous = current;
                }
            }

            public MGizmoBaseDrawCall Finish()
            {
                InitializeMeshDrawCall(lines);
                return lines;
            }
        }
    }
}
