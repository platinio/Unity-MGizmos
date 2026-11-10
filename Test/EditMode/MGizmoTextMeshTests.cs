using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class MGizmoTextMeshTests
    {
        private const float LabelSize = 0.25f;
        private const int MoreLabelsThanTheCacheHolds = 100;
        private const string EveryGlyphTheLabelsUse = "label 0123456789";

        private static Font BuiltinFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [Test]
        public void MoreLabelsInOneFrameThanTheCacheHolds_AllKeepTheirMeshes()
        {
            var font = BuiltinFont();
            // Requesting the glyphs up front keeps an atlas rebuild, which rightly clears the cache, out of the test.
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
            var font = BuiltinFont();

            Assert.AreSame(MGizmoTextMesh.Build("label 7", font, LabelSize), MGizmoTextMesh.Build("label 7", font, LabelSize));
        }

        [Test]
        public void AKeptLabelWhoseMeshWasDestroyed_RebuildsItWhenDrawn()
        {
            var label = MGizmos.RenderText(Vector3.zero, "kept label", LabelSize);
            Assume.That(label, Is.InstanceOf<MGizmoTextDrawCall>(), "MGizmos must be enabled and configured to draw text.");
            Object.DestroyImmediate(DrawCallProbe.MeshOf(label));

            label.Draw(null, 0f);

            Assert.IsTrue(DrawCallProbe.MeshOf(label) != null);
        }
    }
}
