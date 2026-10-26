using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Outlines of the volumes physics engines collide with. A solid gizmo hides the object it describes;
    //an outline lets the reader compare the collision volume against the mesh it stands for.
    public static partial class MGizmos
    {
        private const float DefaultLineWidth = 0.01f;

        private const int SegmentsPerRing = 24;

        private const int SegmentsPerHalfRing = SegmentsPerRing / 2;

        private static readonly float[] BottomAndTop = { -1f, 1f };

        private static readonly Vector2[] CornersAroundTheVerticalEdges = { new(-1f, -1f), new(1f, -1f), new(1f, 1f), new(-1f, 1f) };

        public static MGizmoBaseDrawCall RenderWireCube(Vector3 center, Quaternion rotation, Vector3 size) => RenderWireCube(center, rotation, size, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireCube(Vector3 center, Quaternion rotation, Vector3 size, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;

            var outline = MGizmoCompositeDrawCall.Get();
            Vector3 halfSize = size * 0.5f;

            Vector3 Corner(float x, float y, float z) => center + rotation * Vector3.Scale(halfSize, new Vector3(x, y, z));

            foreach (float y in BottomAndTop)
            {
                AddLine(outline, Corner(-1f, y, -1f), Corner(1f, y, -1f), lineWidth);
                AddLine(outline, Corner(1f, y, -1f), Corner(1f, y, 1f), lineWidth);
                AddLine(outline, Corner(1f, y, 1f), Corner(-1f, y, 1f), lineWidth);
                AddLine(outline, Corner(-1f, y, 1f), Corner(-1f, y, -1f), lineWidth);
            }

            foreach (Vector2 corner in CornersAroundTheVerticalEdges)
            {
                AddLine(outline, Corner(corner.x, -1f, corner.y), Corner(corner.x, 1f, corner.y), lineWidth);
            }

            InitializeMeshDrawCall(outline);
            return outline;
        }

        public static MGizmoBaseDrawCall RenderWireSphere(Vector3 center, Quaternion rotation, float radius) => RenderWireSphere(center, rotation, radius, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireSphere(Vector3 center, Quaternion rotation, float radius, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;

            var outline = MGizmoCompositeDrawCall.Get();
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            Vector3 forward = rotation * Vector3.forward;

            AddRing(outline, center, right, forward, radius, lineWidth);
            AddRing(outline, center, right, up, radius, lineWidth);
            AddRing(outline, center, up, forward, radius, lineWidth);

            InitializeMeshDrawCall(outline);
            return outline;
        }

        public static MGizmoBaseDrawCall RenderWireCylinder(Vector3 center, Quaternion rotation, float radius, float height) => RenderWireCylinder(center, rotation, radius, height, DefaultLineWidth);

        //The cylinder stands along the rotated Y axis, height measured end to end.
        public static MGizmoBaseDrawCall RenderWireCylinder(Vector3 center, Quaternion rotation, float radius, float height, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;

            var outline = MGizmoCompositeDrawCall.Get();
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 halfHeight = rotation * Vector3.up * (height * 0.5f);

            AddRing(outline, center - halfHeight, right, forward, radius, lineWidth);
            AddRing(outline, center + halfHeight, right, forward, radius, lineWidth);
            AddSideLines(outline, center - halfHeight, center + halfHeight, right, forward, radius, radius, lineWidth);

            InitializeMeshDrawCall(outline);
            return outline;
        }

        public static MGizmoBaseDrawCall RenderWireCapsule(Vector3 bottomCenter, Vector3 topCenter, float radius) => RenderWireCapsule(bottomCenter, topCenter, radius, radius, DefaultLineWidth);

        public static MGizmoBaseDrawCall RenderWireCapsule(Vector3 bottomCenter, Vector3 topCenter, float radius, float lineWidth) => RenderWireCapsule(bottomCenter, topCenter, radius, radius, lineWidth);

        //The centres are those of the two end spheres. Different radii draw a tapered capsule.
        public static MGizmoBaseDrawCall RenderWireCapsule(Vector3 bottomCenter, Vector3 topCenter, float bottomRadius, float topRadius, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;

            Vector3 axis = topCenter - bottomCenter;
            if (axis.sqrMagnitude < Mathf.Epsilon) return RenderWireSphere(bottomCenter, Quaternion.identity, Mathf.Max(bottomRadius, topRadius), lineWidth);

            var outline = MGizmoCompositeDrawCall.Get();
            Vector3 up = axis.normalized;
            Vector3 side = Vector3.Cross(up, Mathf.Abs(up.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 otherSide = Vector3.Cross(up, side);

            AddRing(outline, bottomCenter, side, otherSide, bottomRadius, lineWidth);
            AddRing(outline, topCenter, side, otherSide, topRadius, lineWidth);
            AddSideLines(outline, bottomCenter, topCenter, side, otherSide, bottomRadius, topRadius, lineWidth);

            AddHalfRing(outline, topCenter, side, up, topRadius, lineWidth);
            AddHalfRing(outline, topCenter, otherSide, up, topRadius, lineWidth);
            AddHalfRing(outline, bottomCenter, side, -up, bottomRadius, lineWidth);
            AddHalfRing(outline, bottomCenter, otherSide, -up, bottomRadius, lineWidth);

            InitializeMeshDrawCall(outline);
            return outline;
        }

        private static void AddSideLines(MGizmoCompositeDrawCall outline, Vector3 bottomCenter, Vector3 topCenter, Vector3 side, Vector3 otherSide, float bottomRadius, float topRadius, float lineWidth)
        {
            AddSideLine(outline, bottomCenter, topCenter, side, bottomRadius, topRadius, lineWidth);
            AddSideLine(outline, bottomCenter, topCenter, -side, bottomRadius, topRadius, lineWidth);
            AddSideLine(outline, bottomCenter, topCenter, otherSide, bottomRadius, topRadius, lineWidth);
            AddSideLine(outline, bottomCenter, topCenter, -otherSide, bottomRadius, topRadius, lineWidth);
        }

        private static void AddSideLine(MGizmoCompositeDrawCall outline, Vector3 bottomCenter, Vector3 topCenter, Vector3 outward, float bottomRadius, float topRadius, float lineWidth)
        {
            AddLine(outline, bottomCenter + outward * bottomRadius, topCenter + outward * topRadius, lineWidth);
        }

        private static void AddRing(MGizmoCompositeDrawCall outline, Vector3 center, Vector3 firstAxis, Vector3 secondAxis, float radius, float lineWidth)
        {
            AddArc(outline, center, firstAxis, secondAxis, radius, 2f * Mathf.PI, SegmentsPerRing, lineWidth);
        }

        //Half a ring from +across over the bulge to -across: the dome that closes a capsule end.
        private static void AddHalfRing(MGizmoCompositeDrawCall outline, Vector3 center, Vector3 across, Vector3 bulge, float radius, float lineWidth)
        {
            AddArc(outline, center, across, bulge, radius, Mathf.PI, SegmentsPerHalfRing, lineWidth);
        }

        private static void AddArc(MGizmoCompositeDrawCall outline, Vector3 center, Vector3 startAxis, Vector3 turnAxis, float radius, float sweep, int segments, float lineWidth)
        {
            Vector3 PointAt(int segment)
            {
                float angle = sweep * segment / segments;
                return center + (startAxis * Mathf.Cos(angle) + turnAxis * Mathf.Sin(angle)) * radius;
            }

            Vector3 previous = PointAt(0);
            for (int segment = 1; segment <= segments; segment++)
            {
                Vector3 current = PointAt(segment);
                AddLine(outline, previous, current, lineWidth);
                previous = current;
            }
        }

        private static void AddLine(MGizmoCompositeDrawCall outline, Vector3 from, Vector3 to, float lineWidth)
        {
            outline.AddDrawCall(RenderLine(from, to, lineWidth));
        }
    }
}
