using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class MGizmoWireShapeTests
    {
        private const float Tolerance = 1e-3f;

        private const float LineWidth = 0.02f;

        private const int LinesPerRing = 24;

        private static readonly Vector3 Center = new(2f, 3f, 4f);

        private static readonly Quaternion Turned = Quaternion.Euler(10f, 20f, 30f);

        private static readonly Vector3 LineStartTip = Vector3.down;

        private static readonly Vector3 LineEndTip = Vector3.up;

        [Test]
        public void WireCube_DrawsEachOfItsTwelveEdgesOnce()
        {
            var size = new Vector3(2f, 4f, 6f);

            var edges = new HashSet<(Vector3Int, Vector3Int)>();
            foreach (var (from, to) in LinesOf(MGizmos.RenderWireCube(Center, Turned, size, LineWidth)))
            {
                var fromCorner = CornerOf(from, size);
                var toCorner = CornerOf(to, size);
                Assert.AreEqual(1, AxesThatDiffer(fromCorner, toCorner), "an edge runs along exactly one axis");
                edges.Add(Ordered(fromCorner, toCorner));
            }

            Assert.AreEqual(12, edges.Count);
        }

        [Test]
        public void WireSphere_IsThreeRingsOnItsRadiusInTheRotatedPlanes()
        {
            const float radius = 1.5f;

            var points = PointsOf(MGizmos.RenderWireSphere(Center, Turned, radius, LineWidth));

            foreach (var point in points)
            {
                Assert.AreEqual(radius, Vector3.Distance(Center, point), Tolerance);
            }

            foreach (var normal in new[] { Turned * Vector3.right, Turned * Vector3.up, Turned * Vector3.forward })
            {
                var linesInThePlane = 0;
                foreach (var (from, to) in LinesOf(MGizmos.RenderWireSphere(Center, Turned, radius, LineWidth)))
                {
                    linesInThePlane += IsInPlane(from, normal) && IsInPlane(to, normal) ? 1 : 0;
                }

                Assert.AreEqual(LinesPerRing, linesInThePlane, $"one ring in the plane facing {normal}");
            }
        }

        [Test]
        public void WireCylinder_HasItsRadiusAndHeightAlongTheRotatedAxes()
        {
            const float radius = 0.5f;
            const float height = 3f;

            var lines = LinesOf(MGizmos.RenderWireCylinder(Center, Turned, radius, height, LineWidth));
            var points = PointsOf(lines);
            var axis = Turned * Vector3.up;

            Assert.AreEqual(height / 2f, MaximumAlong(points, axis) - Vector3.Dot(Center, axis), Tolerance);
            Assert.AreEqual(radius, MaximumAlong(points, Turned * Vector3.right) - Vector3.Dot(Center, Turned * Vector3.right), Tolerance);
            Assert.AreEqual(4, lines.FindAll(line => Mathf.Abs(Vector3.Dot((line.to - line.from).normalized, axis)) > 1f - Tolerance).Count, "four side lines");
        }

        [TestCase(0f, 0f, 1f)]
        [TestCase(0f, 1f, 0f)]
        public void WireCapsule_ReachesOneRadiusBeyondEachSphereCenterAlongAnyAxis(float x, float y, float z)
        {
            var axis = new Vector3(x, y, z);
            var top = Center + axis * 4f;
            const float radius = 0.5f;

            var points = PointsOf(MGizmos.RenderWireCapsule(Center, top, radius, LineWidth));

            Assert.AreEqual(Vector3.Dot(top, axis) + radius, MaximumAlong(points, axis), Tolerance, "the top dome");
            Assert.AreEqual(Vector3.Dot(Center, axis) - radius, -MaximumAlong(points, -axis), Tolerance, "the bottom dome");
            // A ring of 24 segments reaches the radius only at its corners, so the width is checked to a segment's sag.
            var across = Vector3.Cross(axis, Vector3.one).normalized;
            var segmentSag = radius * (1f - Mathf.Cos(Mathf.PI / LinesPerRing));
            Assert.AreEqual(radius, MaximumAlong(points, across) - Vector3.Dot(Center, across), segmentSag + Tolerance, "as wide as the radius");
        }

        [Test]
        public void WireTaperedCapsule_SideLinesTouchBothSpheres()
        {
            var bottom = Vector3.zero;
            var top = Vector3.up;
            const float bottomRadius = 1f;
            const float topRadius = 0.2f;

            var sideLines = 0;
            foreach (var (from, to) in LinesOf(MGizmos.RenderWireTaperedCapsule(bottom, top, bottomRadius, topRadius, LineWidth)))
            {
                if (Mathf.Abs(Vector3.Distance(from, bottom) - bottomRadius) > Tolerance || Mathf.Abs(Vector3.Distance(to, top) - topRadius) > Tolerance)
                {
                    continue;
                }

                sideLines++;
                var along = (to - from).normalized;
                Assert.AreEqual(0f, Vector3.Dot(along, (from - bottom).normalized), Tolerance, "tangent to the bottom sphere");
                Assert.AreEqual(0f, Vector3.Dot(along, (to - top).normalized), Tolerance, "tangent to the top sphere");
            }

            Assert.AreEqual(4, sideLines);
        }

        [Test]
        public void WireTaperedCapsule_EveryPointLiesOnOneOfItsSpheres()
        {
            var top = Center + Vector3.forward * 2f;

            foreach (var point in PointsOf(MGizmos.RenderWireTaperedCapsule(Center, top, 1.2f, 0.4f, LineWidth)))
            {
                var onBottom = Mathf.Abs(Vector3.Distance(point, Center) - 1.2f) < Tolerance;
                var onTop = Mathf.Abs(Vector3.Distance(point, top) - 0.4f) < Tolerance;
                Assert.IsTrue(onBottom || onTop, $"{point} is on neither sphere");
            }
        }

        [TestCase(0f)]
        [TestCase(1e-6f)]
        public void WireCapsuleWithItsCentersTogether_IsASphereOfTheLargerRadius(float apart)
        {
            var points = PointsOf(MGizmos.RenderWireTaperedCapsule(Center, Center + Vector3.up * apart, 0.5f, 2f, LineWidth));

            Assert.IsNotEmpty(points);
            foreach (var point in points)
            {
                Assert.AreEqual(2f, Vector3.Distance(Center, point), Tolerance);
            }
        }

        [Test]
        public void WireTaperedCapsuleWhoseLargerSphereHoldsTheOther_IsThatSphere()
        {
            var top = Center + Vector3.up * 0.5f;

            foreach (var point in PointsOf(MGizmos.RenderWireTaperedCapsule(Center, top, 2f, 0.5f, LineWidth)))
            {
                Assert.AreEqual(2f, Vector3.Distance(Center, point), Tolerance);
            }
        }

        private static Vector3Int CornerOf(Vector3 point, Vector3 size)
        {
            var local = Quaternion.Inverse(Turned) * (point - Center);
            Assert.AreEqual(size.x / 2f, Mathf.Abs(local.x), Tolerance);
            Assert.AreEqual(size.y / 2f, Mathf.Abs(local.y), Tolerance);
            Assert.AreEqual(size.z / 2f, Mathf.Abs(local.z), Tolerance);
            return new Vector3Int((int)Mathf.Sign(local.x), (int)Mathf.Sign(local.y), (int)Mathf.Sign(local.z));
        }

        private static int AxesThatDiffer(Vector3Int a, Vector3Int b)
        {
            return (a.x != b.x ? 1 : 0) + (a.y != b.y ? 1 : 0) + (a.z != b.z ? 1 : 0);
        }

        private static (Vector3Int, Vector3Int) Ordered(Vector3Int a, Vector3Int b)
        {
            var aFirst = a.x < b.x || a.x == b.x && (a.y < b.y || a.y == b.y && a.z < b.z);
            return aFirst ? (a, b) : (b, a);
        }

        private static bool IsInPlane(Vector3 point, Vector3 normal)
        {
            return Mathf.Abs(Vector3.Dot(point - Center, normal)) < Tolerance;
        }

        private static float MaximumAlong(List<Vector3> points, Vector3 direction)
        {
            var maximum = float.MinValue;
            foreach (var point in points) maximum = Mathf.Max(maximum, Vector3.Dot(point, direction));
            return maximum;
        }

        private static List<Vector3> PointsOf(MGizmoBaseDrawCall outline) => PointsOf(LinesOf(outline));

        private static List<Vector3> PointsOf(List<(Vector3 from, Vector3 to)> lines)
        {
            var points = new List<Vector3>();
            foreach (var (from, to) in lines)
            {
                points.Add(from);
                points.Add(to);
            }

            return points;
        }

        private static List<(Vector3 from, Vector3 to)> LinesOf(MGizmoBaseDrawCall outline)
        {
            var lines = new List<(Vector3, Vector3)>();
            foreach (var line in DrawCallProbe.Read<List<MGizmoBaseDrawCall>>(outline, "drawCalls"))
            {
                var matrix = DrawCallProbe.MatrixOf(line);
                lines.Add((matrix.MultiplyPoint3x4(LineStartTip), matrix.MultiplyPoint3x4(LineEndTip)));
            }

            return lines;
        }
    }
}
