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

        private static TextMeshCache CacheWith(int sizeBeforeEviction, int sizeBeforeClearing, params string[] keysBuiltInTheFirstFrame)
        {
            var cache = new TextMeshCache(sizeBeforeEviction, sizeBeforeClearing);
            foreach (var key in keysBuiltInTheFirstFrame) cache.Add(key, new Mesh(), FirstFrame);
            return cache;
        }

        private static bool Holds(TextMeshCache cache, string key) => cache.TryGet(key, NextFrame, out _);

        [Test]
        public void AFullCache_EvictsTheLabelsNobodyUsedThisFrame_AndDestroysTheirMeshes()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");
            cache.TryGet("a", FirstFrame, out var meshOfA);

            cache.Add("c", new Mesh(), NextFrame);

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(meshOfA == null, "An evicted label's mesh is destroyed, not leaked.");
        }

        [Test]
        public void ALabelDrawnThisFrame_SurvivesEviction()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");

            cache.MarkUsed("a", NextFrame);
            cache.Add("c", new Mesh(), NextFrame);

            Assert.IsTrue(Holds(cache, "a"));
            Assert.IsFalse(Holds(cache, "b"));
        }

        [Test]
        public void ALabelReusedThisFrame_SurvivesEviction()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b");

            cache.TryGet("a", NextFrame, out _);
            cache.Add("c", new Mesh(), NextFrame);

            Assert.IsTrue(Holds(cache, "a"));
            Assert.IsFalse(Holds(cache, "b"));
        }

        [Test]
        public void AFrameUsingMoreLabelsThanTheCacheHolds_GrowsIt()
        {
            var cache = CacheWith(HoldsTwo, NeverClears, "a", "b", "c");

            Assert.AreEqual(3, cache.Count);
        }

        [Test]
        public void TheHardLimit_ClearsEverything()
        {
            var cache = CacheWith(HoldsTwo, 3, "a", "b", "c");

            cache.Add("d", new Mesh(), FirstFrame);

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(Holds(cache, "d"));
        }
    }
}
