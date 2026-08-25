using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <summary>
    /// The dimensions the generated primitive meshes have to keep, and the caller arithmetic that depends
    /// on them.
    /// </summary>
    /// <remarks>
    /// These exist because the failure mode is silent. The meshes used to come from Unity's built-in
    /// resources, and every <c>Render*</c> call was written against those conventions - a cylinder 2 units
    /// tall, a sphere 1 unit across. Generate them a unit off and nothing throws, nothing logs: every line
    /// in every project using the package simply comes out half length. So the sizes are asserted here
    /// rather than left to be noticed in a scene view.
    /// </remarks>
    public class MGizmoPrimitiveMeshTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void SphereMesh_IsOneUnitAcross()
        {
            //the built-in convention RenderSphere's radius * 2 scaling is written against
            Bounds bounds = DrawCallProbe.MeshOf(MGizmos.RenderSphere(Vector3.zero, 1f)).bounds;

            AssertApproximately(Vector3.one, bounds.size, "diameter 1, not radius 1");
            AssertApproximately(Vector3.zero, bounds.center, "centred on the origin");
        }

        [Test]
        public void CylinderMesh_IsTwoUnitsTall()
        {
            //the other built-in convention that fails silently: RenderLine scales Y by half the distance
            Bounds bounds = DrawCallProbe.MeshOf(MGizmos.RenderCylinder(Vector3.zero)).bounds;

            AssertApproximately(new Vector3(1f, 2f, 1f), bounds.size, "radius 0.5, height 2");
            AssertApproximately(Vector3.zero, bounds.center, "centred on the origin");
        }

        [Test]
        public void CubeMesh_IsAUnitCube()
        {
            Bounds bounds = DrawCallProbe.MeshOf(MGizmos.RenderCube(Vector3.zero)).bounds;

            AssertApproximately(Vector3.one, bounds.size);
            AssertApproximately(Vector3.zero, bounds.center);
        }

        [Test]
        public void QuadMesh_IsAUnitQuadInTheXYPlaneFacingBack()
        {
            Mesh quad = DrawCallProbe.MeshOf(MGizmos.RenderQuad(Vector3.zero));

            AssertApproximately(new Vector3(1f, 1f, 0f), quad.bounds.size);

            foreach (Vector3 normal in quad.normals)
                Assert.AreEqual(1f, Vector3.Dot(normal, Vector3.back), Tolerance, "the built-in quad faces -Z");
        }

        [Test]
        public void ConeMesh_StandsOnItsBaseAndPointsUp()
        {
            // The arrowhead FBX pointed down +Z with its base on the origin, and RenderArrow corrected for
            // that with an extra -90 degree rotation. The generated cone is Y-up instead, which is only
            // safe as long as it really is base-anchored: a centre-anchored one would still look like a
            // cone and sit half a head length short of where the arrow ends.
            Mesh cone = ArrowHeadMesh();

            AssertApproximately(Vector3.one, cone.bounds.size, "base diameter 1, height 1");
            AssertApproximately(new Vector3(0f, 0.5f, 0f), cone.bounds.center, "base at y = 0, tip at y = 1");
        }

        [Test]
        public void RenderLine_SpansExactlyFromTo()
        {
            var from = new Vector3(1f, 2f, 3f);
            var to = new Vector3(1f, 2f, 13f);

            Matrix4x4 matrix = DrawCallProbe.MatrixOf(MGizmos.RenderLine(from, to, 0.05f));

            AssertApproximately(from, matrix.MultiplyPoint3x4(Vector3.down), "the mesh's -Y end lands on 'from'");
            AssertApproximately(to, matrix.MultiplyPoint3x4(Vector3.up), "the mesh's +Y end lands on 'to'");
        }

        [Test]
        public void RenderSphere_HasTheRequestedWorldRadius()
        {
            var sphere = MGizmos.RenderSphere(new Vector3(5f, 0f, 0f), 3f);

            Bounds local = DrawCallProbe.MeshOf(sphere).bounds;
            Vector3 rim = DrawCallProbe.MatrixOf(sphere).MultiplyPoint3x4(new Vector3(local.extents.x, 0f, 0f));

            Assert.AreEqual(3f, rim.x - 5f, Tolerance, "radius in, radius out");
        }

        [Test]
        public void RenderArrow_PutsTheConeTipOnTheEndPoint()
        {
            var from = new Vector3(1f, 2f, 3f);
            var to = new Vector3(1f, 2f, 8f);
            const float headSize = 0.5f;

            Matrix4x4 head = DrawCallProbe.MatrixOf(ArrowParts(MGizmos.RenderArrow(from, to, 0.02f, headSize))[1]);

            AssertApproximately(to, head.MultiplyPoint3x4(Vector3.up), "the tip is the point of the arrow");
            AssertApproximately(to - Vector3.forward * headSize, head.MultiplyPoint3x4(Vector3.zero),
                "and the base sits one head length back along the arrow");
        }

        [Test]
        public void EveryPrimitiveMesh_CarriesRealNormals()
        {
            // The instanced gizmo shader reads position only, so a mesh with no normals renders correctly
            // right up until someone calls SetMaterial with a lit shader - and then lights up black with
            // nothing logged. The disc is the interesting case: it is double-sided, and sharing vertices
            // between its two windings would average every normal to zero.
            foreach (Mesh mesh in AllPrimitiveMeshes())
            {
                Vector3[] normals = mesh.normals;

                Assert.AreEqual(mesh.vertexCount, normals.Length, $"{mesh.name} has no normals at all");

                for (int i = 0; i < normals.Length; i++)
                    Assert.AreEqual(1f, normals[i].magnitude, 1e-3f, $"{mesh.name} vertex {i} has a degenerate normal");
            }
        }

        [Test]
        public void PrimitiveMeshes_AreBuiltOnceAndReused()
        {
            //rebuilding per call would allocate a mesh per gizmo, and break instancing: the batcher keys
            //on the mesh instance, so a fresh one every frame is a fresh batch every frame
            List<Mesh> first = new(AllPrimitiveMeshes());
            List<Mesh> second = new(AllPrimitiveMeshes());

            for (int i = 0; i < first.Count; i++)
                Assert.AreSame(first[i], second[i], $"{first[i].name} was rebuilt instead of served from the cache");
        }

        private static IEnumerable<Mesh> AllPrimitiveMeshes()
        {
            yield return DrawCallProbe.MeshOf(MGizmos.RenderSphere(Vector3.zero, 1f));
            yield return DrawCallProbe.MeshOf(MGizmos.RenderCube(Vector3.zero));
            yield return DrawCallProbe.MeshOf(MGizmos.RenderCylinder(Vector3.zero));
            yield return DrawCallProbe.MeshOf(MGizmos.RenderQuad(Vector3.zero));
            yield return DrawCallProbe.MeshOf(MGizmos.RenderDisc(Vector3.zero, 1f));
            yield return ArrowHeadMesh();
        }

        private static Mesh ArrowHeadMesh() =>
            DrawCallProbe.MeshOf(ArrowParts(MGizmos.RenderArrow(Vector3.zero, Vector3.forward))[1]);

        //RenderArrow returns a composite of exactly two calls: the cylinder stem, then the cone head
        private static List<MGizmoBaseDrawCall> ArrowParts(MGizmoBaseDrawCall arrow)
        {
            var parts = DrawCallProbe.Read<List<MGizmoBaseDrawCall>>(arrow, "drawCalls");
            Assert.AreEqual(2, parts.Count, "an arrow is a stem plus a head");
            return parts;
        }

        private static void AssertApproximately(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.AreEqual(0f, (expected - actual).magnitude, Tolerance,
                $"{message ?? "vector mismatch"} - expected {expected:F4}, was {actual:F4}");
        }
    }
}
