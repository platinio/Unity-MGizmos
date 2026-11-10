using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class TextMeshCacheTests
    {
        private const int HoldsTwo = 2;
        private const int NeverClears = 100;
        private const int FirstFrame = 1;
        private const int NextFrame = 2;

        private readonly List<Mesh> retired = new();
        private Font font;
        private Font otherFont;

        [SetUp]
        public void SetUp()
        {
            retired.Clear();
            font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            otherFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(font);
            Object.DestroyImmediate(otherFont);
        }

        private TextMeshCache CacheWith(int sizeBeforeEviction, int sizeBeforeClearing, params string[] keysBuiltInTheFirstFrame)
        {
            var cache = new TextMeshCache(sizeBeforeEviction, sizeBeforeClearing, retired.Add);
            foreach (var key in keysBuiltInTheFirstFrame) cache.Add(key, new Mesh(), font, FirstFrame);
            return cache;
        }

        private static bool Holds(TextMeshCache cache, string key) => cache.TryGet(key, NextFrame, out _);

        [Test]
        public void AFullCache_EvictsTheLabelsNobodyUsedThisFrame_RetiringRatherThanDestroyingThem()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");
            cache.TryGet("a", FirstFrame, out var meshOfA);
            int version = cache.Version;

            cache.Add("c", new Mesh(), font, NextFrame);

            Assert.AreEqual(1, cache.Count);
            CollectionAssert.Contains(retired, meshOfA);
            Assert.IsTrue(meshOfA != null, "A label may still be drawing the evicted mesh this frame.");
            Assert.AreNotEqual(version, cache.Version, "Labels holding an evicted mesh have to be told.");
        }

        [Test]
        public void ALabelDrawnThisFrame_SurvivesEviction()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");

            cache.MarkUsed("a", NextFrame);
            cache.Add("c", new Mesh(), font, NextFrame);

            Assert.IsTrue(Holds(cache, "a"));
            Assert.IsFalse(Holds(cache, "b"));
        }

        [Test]
        public void ALabelReusedThisFrame_SurvivesEviction()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");

            cache.TryGet("a", NextFrame, out _);
            cache.Add("c", new Mesh(), font, NextFrame);

            Assert.IsTrue(Holds(cache, "a"));
            Assert.IsFalse(Holds(cache, "b"));
        }

        [Test]
        public void AFrameUsingMoreLabelsThanTheCacheHolds_GrowsIt()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b", "c");

            Assert.AreEqual(3, cache.Count);
            Assert.IsEmpty(retired);
        }

        [Test]
        public void TheHardLimit_ClearsEverything()
        {
            var cache = CacheWith(HoldsTwo, 3, "a", "b", "c");

            cache.Add("d", new Mesh(), font, FirstFrame);

            Assert.AreEqual(1, cache.Count);
            Assert.AreEqual(3, retired.Count);
            Assert.IsTrue(Holds(cache, "d"));
        }

        [Test]
        public void InvalidatingAFont_DropsOnlyThatFontsLabels()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a");
            cache.Add("other", new Mesh(), otherFont, FirstFrame);
            int version = cache.Version;

            cache.InvalidateFont(otherFont);

            Assert.IsTrue(Holds(cache, "a"));
            Assert.IsFalse(Holds(cache, "other"));
            Assert.AreNotEqual(version, cache.Version);
        }

        [Test]
        public void InvalidatingAFontWithNoLabels_ChangesNothing()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a");
            int version = cache.Version;

            cache.InvalidateFont(otherFont);

            Assert.AreEqual(version, cache.Version);
            Assert.IsTrue(Holds(cache, "a"));
        }
    }
}
