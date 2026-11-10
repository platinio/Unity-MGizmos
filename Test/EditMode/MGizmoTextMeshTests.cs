using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <summary>The text mesh cache never destroys a label the current frame still has to draw.</summary>
    public class MGizmoTextMeshTests
    {
        private const float LabelSize = 0.25f;
        private const int MoreLabelsThanTheCacheHolds = 100;
        private const string EveryGlyphTheLabelsUse = "label 0123456789";

        [Test]
        public void MoreLabelsInOneFrameThanTheCacheHolds_AllKeepTheirMeshes()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Requesting the glyphs up front keeps an atlas rebuild, which rightly clears the cache, out of the test.
            font.RequestCharactersInTexture(EveryGlyphTheLabelsUse, 64, FontStyle.Normal);

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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Assert.AreSame(MGizmoTextMesh.Build("label 7", font, LabelSize), MGizmoTextMesh.Build("label 7", font, LabelSize));
        }
    }
}
