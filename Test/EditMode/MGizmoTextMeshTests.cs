using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <remarks>
    /// Unity raises <c>Font.textureRebuilt</c> from wherever the atlas grew, including Canvas rendering, where
    /// <c>DestroyImmediate</c> is an error. A test cannot stand inside a render callback, so what is pinned is the
    /// property that makes the callback safe: nothing is destroyed synchronously.
    /// </remarks>
    public class MGizmoTextMeshTests
    {
        private const float LabelSize = 0.2f;
        private const int MoreLabelsThanTheCacheHolds = 100;
        private const string EveryGlyphTheLabelsUse = "label 0123456789.";

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

        private Camera TestCamera => cameraObject.GetComponent<Camera>();

        private static void RaiseTextureRebuilt(Font rebuilt)
        {
            var handler = typeof(MGizmoTextMesh).GetMethod("OnFontTextureRebuilt", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(handler, "OnFontTextureRebuilt not found; the test would pass without testing anything");
            handler.Invoke(null, new object[] { rebuilt });
        }

        [Test]
        public void MoreLabelsInOneFrameThanTheCacheHolds_AllKeepTheirMeshes()
        {
            // Requesting the glyphs up front keeps an atlas rebuild, which rightly invalidates the cache, out of the test.
            font.RequestCharactersInTexture(EveryGlyphTheLabelsUse, MGizmoTextMesh.RenderFontSize, FontStyle.Normal);

            var meshes = new List<Mesh>();
            for (int i = 0; i < MoreLabelsThanTheCacheHolds; i++)
            {
                meshes.Add(MGizmoTextMesh.Build($"label {i}", font, LabelSize));
            }

            int destroyed = meshes.FindAll(mesh => mesh == null).Count;
            Assert.AreEqual(0, destroyed, "A label built earlier this frame was destroyed before it could render.");
        }

        [Test]
        public void TheSameLabel_ReusesItsMesh()
        {
            Assert.AreSame(MGizmoTextMesh.Build("label 7", font, LabelSize), MGizmoTextMesh.Build("label 7", font, LabelSize));
        }

        [Test]
        public void TheSameTextInTwoFonts_IsTwoMeshes()
        {
            Assert.AreNotSame(MGizmoTextMesh.Build("0.61", font, LabelSize), MGizmoTextMesh.Build("0.61", otherFont, LabelSize));
        }

        [Test]
        public void AtlasRebuild_InvalidatesWithoutDestroyingTheMeshOnTheSpot()
        {
            var mesh = MGizmoTextMesh.Build("0.61", font, LabelSize);
            int version = MGizmoTextMesh.Version;

            RaiseTextureRebuilt(font);

            Assert.IsTrue(mesh != null, "the handler can run inside Canvas rendering, where DestroyImmediate is an error");
            Assert.AreNotEqual(version, MGizmoTextMesh.Version, "holders have to be told their mesh is stale");
            Assert.AreNotSame(mesh, MGizmoTextMesh.Build("0.61", font, LabelSize), "the stale mesh must not be served again");
        }

        [Test]
        public void AtlasRebuildOfAnotherFont_LeavesTheCacheAlone()
        {
            var mesh = MGizmoTextMesh.Build("0.61", font, LabelSize);
            int version = MGizmoTextMesh.Version;

            RaiseTextureRebuilt(otherFont);

            Assert.AreEqual(version, MGizmoTextMesh.Version);
            Assert.AreSame(mesh, MGizmoTextMesh.Build("0.61", font, LabelSize));
        }

        [Test]
        public void ARetainedLabel_BuildsItsMeshAgainAfterAnInvalidation()
        {
            using var group = new MGizmoGroup();
            var label = MGizmos.RenderText(Vector3.zero, "0.61", LabelSize);
            Assume.That(label, Is.InstanceOf<MGizmoTextDrawCall>(), "MGizmos must be enabled and configured to draw text.");
            group.Add(label);
            var before = DrawCallProbe.MeshOf(label);

            RaiseTextureRebuilt(DrawCallProbe.Read<Font>(label, "font"));
            label.Draw(TestCamera, 0f);

            var after = DrawCallProbe.MeshOf(label);
            Assert.AreNotSame(before, after, "the label kept drawing a mesh whose glyph UVs are gone");
            Assert.IsTrue(after != null);
        }

        [Test]
        public void AKeptLabelWhoseMeshWasDestroyed_RebuildsItWhenDrawn()
        {
            var label = MGizmos.RenderText(Vector3.zero, "kept label", LabelSize);
            Assume.That(label, Is.InstanceOf<MGizmoTextDrawCall>(), "MGizmos must be enabled and configured to draw text.");
            Object.DestroyImmediate(DrawCallProbe.MeshOf(label));

            label.Draw(TestCamera, 0f);

            Assert.IsTrue(DrawCallProbe.MeshOf(label) != null);
        }

        [Test]
        public void AClone_CarriesTheSourceAndHealsToo()
        {
            var label = MGizmos.RenderText(Vector3.zero, "0.61", LabelSize);
            Assume.That(label, Is.InstanceOf<MGizmoTextDrawCall>(), "MGizmos must be enabled and configured to draw text.");
            var clone = label.Clone();
            var before = DrawCallProbe.MeshOf(clone);

            RaiseTextureRebuilt(DrawCallProbe.Read<Font>(label, "font"));
            clone.Draw(TestCamera, 0f);

            Assert.AreNotSame(before, DrawCallProbe.MeshOf(clone), "per-camera clones are what the timed path actually draws");
        }

        [Test]
        public void APooledLabel_DoesNotKeepThePreviousText()
        {
            using var group = new MGizmoGroup();
            var first = MGizmos.RenderText(Vector3.zero, "0.61", LabelSize);
            Assume.That(first, Is.InstanceOf<MGizmoTextDrawCall>(), "MGizmos must be enabled and configured to draw text.");
            var mesh = DrawCallProbe.MeshOf(first);
            group.Add(first);
            group.Clear();

            var second = MGizmoTextDrawCall.Get(mesh, Vector3.zero, Quaternion.identity, Vector3.one, true);

            Assert.AreSame(first, second, "the released label should be the next one handed out");
            Assert.IsNull(DrawCallProbe.Read<string>(second, "text"), "a label built from a mesh has no text to rebuild from");
        }
    }
}
