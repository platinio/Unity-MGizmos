using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class MGizmoWireShapeTests
    {
        private const float Tolerance = 1e-3f;

        private const float LineWidth = 0.02f;

        private static readonly Vector3 Center = new(2f, 3f, 4f);

        [Test]
        public void WireCube_HasTwelveEdgesBetweenItsCorners()
        {
            var size = new Vector3(2f, 4f, 6f);
            var rotation = Quaternion.Euler(10f, 20f, 30f);

            var edges = LinesOf(MGizmos.RenderWireCube(Center, rotation, size, LineWidth));

            Assert.AreEqual(12, edges.Count);
            foreach (var (from, to) in edges)
            {
                AssertIsCorner(from, rotation, size);
                AssertIsCorner(to, rotation, size);
            }
        }

        [Test]
        public void WireSphere_LiesOnItsRadius()
        {
            const float radius = 1.5f;

            foreach (var (from, to) in LinesOf(MGizmos.RenderWireSphere(Center, Quaternion.identity, radius, LineWidth)))
            {
                Assert.AreEqual(radius, Vector3.Distance(Center, from), Tolerance);
                Assert.AreEqual(radius, Vector3.Distance(Center, to), Tolerance);
            }
        }

        [Test]
        public void WireCylinder_SpansItsHeightAlongTheRotatedUpAxis()
        {
            const float radius = 0.5f;
            const float height = 3f;
            var rotation = Quaternion.Euler(0f, 0f, 90f);

            var points = PointsOf(MGizmos.RenderWireCylinder(Center, rotation, radius, height, LineWidth));
            var axis = rotation * Vector3.up;

            Assert.AreEqual(height / 2f, MaximumAlong(points, axis) - Vector3.Dot(Center, axis), Tolerance);
            Assert.AreEqual(height / 2f, Vector3.Dot(Center, axis) - MinimumAlong(points, axis), Tolerance);
        }

        [Test]
        public void WireCapsule_ReachesOneRadiusBeyondEachSphereCenter()
        {
            var bottom = new Vector3(0f, 0f, 0f);
            var top = new Vector3(0f, 0f, 4f);
            const float bottomRadius = 0.5f;
            const float topRadius = 1f;

            var points = PointsOf(MGizmos.RenderWireCapsule(bottom, top, bottomRadius, topRadius, LineWidth));

            Assert.AreEqual(top.z + topRadius, MaximumAlong(points, Vector3.forward), Tolerance, "the top dome");
            Assert.AreEqual(bottom.z - bottomRadius, MinimumAlong(points, Vector3.forward), Tolerance, "the bottom dome");
            Assert.AreEqual(topRadius, MaximumAlong(points, Vector3.right), Tolerance, "the wider end sets the width");
        }

        [Test]
        public void WireCapsuleWithBothCentersTogether_IsASphereOfTheLargerRadius()
        {
            var points = PointsOf(MGizmos.RenderWireCapsule(Center, Center, 0.5f, 2f, LineWidth));

            foreach (var point in points)
            {
                Assert.AreEqual(2f, Vector3.Distance(Center, point), Tolerance);
            }
        }

        private static void AssertIsCorner(Vector3 point, Quaternion rotation, Vector3 size)
        {
            var local = Quaternion.Inverse(rotation) * (point - Center);
            Assert.AreEqual(size.x / 2f, Mathf.Abs(local.x), Tolerance);
            Assert.AreEqual(size.y / 2f, Mathf.Abs(local.y), Tolerance);
            Assert.AreEqual(size.z / 2f, Mathf.Abs(local.z), Tolerance);
        }

        private static float MaximumAlong(List<Vector3> points, Vector3 direction)
        {
            var maximum = float.MinValue;
            foreach (var point in points) maximum = Mathf.Max(maximum, Vector3.Dot(point, direction));
            return maximum;
        }

        private static float MinimumAlong(List<Vector3> points, Vector3 direction) => -MaximumAlong(points, -direction);

        private static List<Vector3> PointsOf(MGizmoBaseDrawCall outline)
        {
            var points = new List<Vector3>();
            foreach (var (from, to) in LinesOf(outline))
            {
                points.Add(from);
                points.Add(to);
            }

            return points;
        }

        //every line is the 2-unit cylinder mesh stretched between its ends, so its -Y and +Y tips are the ends
        private static List<(Vector3 from, Vector3 to)> LinesOf(MGizmoBaseDrawCall outline)
        {
            var lines = new List<(Vector3, Vector3)>();
            foreach (var line in DrawCallProbe.Read<List<MGizmoBaseDrawCall>>(outline, "drawCalls"))
            {
                var matrix = DrawCallProbe.MatrixOf(line);
                lines.Add((matrix.MultiplyPoint3x4(Vector3.down), matrix.MultiplyPoint3x4(Vector3.up)));
            }

            return lines;
        }
    }
}
