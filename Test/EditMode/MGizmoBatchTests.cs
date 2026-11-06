using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class MGizmoBatchTests
    {
        private static readonly Color32 Red = new(255, 0, 0, 255);

        private static readonly Color32 Blue = new(0, 0, 255, 255);

        private MGizmoBatch batch;

        [SetUp]
        public void SetUp()
        {
            batch = new MGizmoBatch();
        }

        [TearDown]
        public void TearDown()
        {
            batch.Dispose();
        }

        [Test]
        public void AddedLinesAndTriangles_AreCounted()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            batch.AddLine(Vector3.zero, Vector3.right, Red);
            batch.AddTriangle(Vector3.zero, Vector3.up, Vector3.right, Blue);

            Assert.AreEqual(2, batch.LineCount);
            Assert.AreEqual(1, batch.TriangleCount);
        }

        [Test]
        public void Clearing_EmptiesTheBatch()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            batch.AddTriangle(Vector3.zero, Vector3.up, Vector3.right, Blue);

            batch.Clear();

            Assert.IsTrue(batch.IsEmpty);
        }

        [Test]
        public void AnEmptyBatch_RendersNothing()
        {
            using var group = new MGizmoGroup();

            group.Add(MGizmos.RenderBatch(batch));

            Assert.AreEqual(0, group.Count, "an empty batch must hand back the inert draw call, which a group ignores");
        }

        [Test]
        public void RenderingALine_UploadsItAsALinesMesh_WithItsVertexColour()
        {
            batch.AddLine(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f), Red);

            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(MeshTopology.Lines, mesh.GetTopology(0));
            CollectionAssert.AreEqual(new[] { new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f) }, mesh.vertices);
            CollectionAssert.AreEqual(new[] { Red, Red }, mesh.colors32);
        }

        [Test]
        public void RenderingATriangle_UploadsItAsATrianglesMesh()
        {
            batch.AddTriangle(Vector3.zero, Vector3.up, Vector3.right, Blue);

            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(MeshTopology.Triangles, mesh.GetTopology(0));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, mesh.GetIndices(0));
            CollectionAssert.AreEqual(new[] { Blue, Blue, Blue }, mesh.colors32);
        }

        [Test]
        public void TheDrawCall_UsesTheVertexColourMaterial_TintedWhite()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            batch.AddTriangle(Vector3.zero, Vector3.up, Vector3.right, Blue);

            var drawCall = MGizmos.RenderBatch(batch);

            var parts = PartsOf(drawCall);
            Assert.AreEqual(2, parts.Count, "one draw for the lines, one for the triangles");
            foreach (var part in parts)
            {
                Assert.AreSame(MGizmos.Config.VertexColorMaterial, DrawCallProbe.Read<Material>(part, "material"));
                Assert.AreEqual(Color.white, DrawCallProbe.Read<Color>(part, "color"), "a tint other than white would recolour every vertex");
            }
        }

        [Test]
        public void ABatchRefilledAfterRendering_UploadsItsNewContents()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            MGizmos.RenderBatch(batch);

            batch.Clear();
            batch.AddLine(Vector3.zero, Vector3.forward, Blue);
            batch.AddLine(Vector3.zero, Vector3.back, Blue);
            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(4, mesh.vertexCount);
            Assert.AreEqual(Vector3.forward, mesh.vertices[1]);
        }

        [Test]
        public void ABatchRefilledWithLess_DrawsOnlyWhatItHoldsNow()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            batch.AddLine(Vector3.zero, Vector3.down, Red);
            batch.AddLine(Vector3.zero, Vector3.left, Red);
            MGizmos.RenderBatch(batch);

            batch.Clear();
            batch.AddLine(Vector3.zero, Vector3.forward, Blue);
            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(2, mesh.vertexCount);
            Assert.AreEqual(2, mesh.GetIndices(0).Length, "indices left over from the bigger picture would draw lines that are gone");
        }

        [Test]
        public void ABatchPastSixteenBitIndices_KeepsEveryLine()
        {
            const int lineCount = 40000;
            for (var line = 0; line < lineCount; line++)
            {
                batch.AddLine(new Vector3(line, 0f, 0f), new Vector3(line, 1f, 0f), Red);
            }

            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(lineCount * 2, mesh.GetIndices(0).Length);
        }

        [Test]
        public void DisposingTheBatch_DestroysItsMeshes()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            batch.Dispose();

            Assert.IsTrue(mesh == null, "the batch owns its meshes and must not leak them");
        }

        [Test]
        public void TheUploadedBounds_HoldEveryLine_SoTheBatchIsNotCulledWhileOnScreen()
        {
            batch.AddLine(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f), Red);
            MGizmos.RenderBatch(batch);
            batch.Clear();
            batch.AddLine(Vector3.zero, Vector3.right, Red);

            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];

            Assert.AreEqual(Vector3.zero, mesh.bounds.min);
            Assert.AreEqual(Vector3.right, mesh.bounds.max);
        }

        [Test]
        public void RenderingAnUnchangedBatchAgain_UploadsNothing()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            var mesh = MeshesOf(MGizmos.RenderBatch(batch))[0];
            var sentinel = new[] { Vector3.one * 7f, Vector3.one * 8f };
            mesh.vertices = sentinel;

            MGizmos.RenderBatch(batch);

            CollectionAssert.AreEqual(sentinel, mesh.vertices, "an upload would have overwritten the sentinel");
        }

        [Test]
        public void RefillingABatchThatHasGrown_AllocatesNothing()
        {
            FillWithLines(2000);
            MGizmos.RenderBatch(batch);

            Assert.That(() =>
            {
                batch.Clear();
                FillWithLines(2000);
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void SetColor_TintsTheWholeBatch()
        {
            batch.AddLine(Vector3.zero, Vector3.up, Red);
            batch.AddTriangle(Vector3.zero, Vector3.up, Vector3.right, Blue);

            var drawCall = MGizmos.RenderBatch(batch).SetColor(Color.green);

            foreach (var part in PartsOf(drawCall))
            {
                Assert.AreEqual(Color.green, DrawCallProbe.Read<Color>(part, "color"));
            }
        }

        [Test]
        public void ATimedPictureOfADisposedBatch_IsDroppedAtTheNextCameraPass()
        {
            var cameraObject = new GameObject("Batch Test Camera");
            var camera = cameraObject.AddComponent<Camera>();
            try
            {
                MGizmos.HandleCameraDrawCalls(camera, 0f);
                batch.AddLine(Vector3.zero, Vector3.up, Red);
                MGizmos.AddMeshDrawCall(MGizmos.RenderBatch(batch).SetDuration(5f));

                batch.Dispose();

                Assert.DoesNotThrow(() => MGizmos.HandleCameraDrawCalls(camera, 0.1f));
                Assert.AreEqual(0, DrawCallProbe.DrawCallsOf(camera).Count, "a call whose mesh is gone must leave the camera rather than draw a destroyed mesh for its remaining seconds");
            }
            finally
            {
                MGizmos.RemoveRenderCamera(camera);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private void FillWithLines(int lineCount)
        {
            for (var line = 0; line < lineCount; line++)
            {
                batch.AddLine(new Vector3(line, 0f, 0f), new Vector3(line, 1f, 0f), Red);
            }
        }

        private static List<MGizmoBaseDrawCall> PartsOf(MGizmoBaseDrawCall drawCall)
        {
            return DrawCallProbe.Read<List<MGizmoBaseDrawCall>>(drawCall, "drawCalls");
        }

        private static List<Mesh> MeshesOf(MGizmoBaseDrawCall drawCall)
        {
            return PartsOf(drawCall).ConvertAll(part => DrawCallProbe.MeshOf(part));
        }
    }
}
