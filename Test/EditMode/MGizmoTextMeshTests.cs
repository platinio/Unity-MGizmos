using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <summary>
    /// The text mesh cache's contract with whoever holds a mesh it handed out: invalidating never
    /// destroys a mesh on the spot, and a label re-builds its own mesh afterwards.
    /// </summary>
    /// <remarks>
    /// Unity raises <c>Font.textureRebuilt</c> from wherever the atlas grew, including Canvas rendering,
    /// where <c>DestroyImmediate</c> is an error. A test cannot stand inside a render callback, so what is
    /// pinned is the property that makes the callback safe: nothing is destroyed synchronously.
    /// </remarks>
    public class MGizmoTextMeshTests
    {
        private Font font;
        private Font otherFont;
        private GameObject cameraObject;

        [SetUp]
        public void SetUp()
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            otherFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            cameraObject = new GameObject("MGizmoTextMeshTests camera", typeof(Camera));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(font);
            Object.DestroyImmediate(otherFont);
        }

        private static void RaiseTextureRebuilt(Font rebuilt)
        {
            var handler = typeof(MGizmoTextMesh).GetMethod("OnFontTextureRebuilt", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(handler, "OnFontTextureRebuilt not found; the test would pass without testing anything");
            handler.Invoke(null, new object[] { rebuilt });
        }

        [Test]
        public void AtlasRebuild_InvalidatesWithoutDestroyingTheMeshOnTheSpot()
        {
            var mesh = MGizmoTextMesh.Build("0.61", font, 0.2f);
            int version = MGizmoTextMesh.Version;

            RaiseTextureRebuilt(font);

            Assert.IsTrue(mesh != null, "the handler can run inside Canvas rendering, where DestroyImmediate is an error");
            Assert.AreNotEqual(version, MGizmoTextMesh.Version, "holders have to be told their mesh is stale");
            Assert.AreNotSame(mesh, MGizmoTextMesh.Build("0.61", font, 0.2f), "the stale mesh must not be served again");
        }

        [Test]
        public void AtlasRebuildOfAnotherFont_LeavesTheCacheAlone()
        {
            var mesh = MGizmoTextMesh.Build("0.61", font, 0.2f);
            int version = MGizmoTextMesh.Version;

            RaiseTextureRebuilt(otherFont);

            Assert.AreEqual(version, MGizmoTextMesh.Version);
            Assert.AreSame(mesh, MGizmoTextMesh.Build("0.61", font, 0.2f));
        }

        [Test]
        public void TheSameTextInTwoFonts_IsTwoMeshes()
        {
            Assert.AreNotSame(MGizmoTextMesh.Build("0.61", font, 0.2f), MGizmoTextMesh.Build("0.61", otherFont, 0.2f));
        }

        [Test]
        public void Eviction_InvalidatesWithoutDestroyingTheMeshOnTheSpot()
        {
            var first = MGizmoTextMesh.Build("evict 0", font, 0.2f);
            int version = MGizmoTextMesh.Version;

            for (int i = 1; i <= 600 && version == MGizmoTextMesh.Version; i++)
            {
                MGizmoTextMesh.Build("evict " + i, font, 0.2f);
            }

            Assert.AreNotEqual(version, MGizmoTextMesh.Version, "the cache never evicted; the cap moved past what this test fills");
            Assert.IsTrue(first != null, "a retained label may still be drawing the evicted mesh this frame");
        }

        [Test]
        public void ARetainedLabel_BuildsItsMeshAgainAfterAnInvalidation()
        {
            using var group = new MGizmoGroup();
            var label = MGizmos.RenderText(Vector3.zero, "0.61", 0.2f);
            group.Add(label);
            var before = DrawCallProbe.MeshOf(label);

            RaiseTextureRebuilt(DrawCallProbe.Read<Font>(label, "font"));
            label.Draw(cameraObject.GetComponent<Camera>(), 0f);

            var after = DrawCallProbe.MeshOf(label);
            Assert.AreNotSame(before, after, "the label kept drawing a mesh whose glyph UVs are gone");
            Assert.IsTrue(after != null);
        }

        [Test]
        public void AClone_CarriesTheSourceAndHealsToo()
        {
            var label = MGizmos.RenderText(Vector3.zero, "0.61", 0.2f);
            var clone = label.Clone();
            var before = DrawCallProbe.MeshOf(clone);

            RaiseTextureRebuilt(DrawCallProbe.Read<Font>(label, "font"));
            clone.Draw(cameraObject.GetComponent<Camera>(), 0f);

            Assert.AreNotSame(before, DrawCallProbe.MeshOf(clone), "per-camera clones are what the timed path actually draws");
        }

        [Test]
        public void APooledLabel_DoesNotKeepThePreviousText()
        {
            using var group = new MGizmoGroup();
            var first = MGizmos.RenderText(Vector3.zero, "0.61", 0.2f);
            var mesh = DrawCallProbe.MeshOf(first);
            group.Add(first);
            group.Clear();

            var second = MGizmoTextDrawCall.Get(mesh, Vector3.zero, Quaternion.identity, Vector3.one, true);

            Assert.AreSame(first, second, "the released label should be the next one handed out");
            Assert.IsNull(DrawCallProbe.Read<string>(second, "text"), "a label built from a mesh has no text to rebuild from");
        }
    }
}
