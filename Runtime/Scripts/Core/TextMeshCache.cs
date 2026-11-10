using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Text meshes by key, each with the last frame it was built or drawn. A full cache evicts the labels nobody
    //used this frame, at most once a frame; a frame that uses more labels than the cache holds grows it. The
    //frame is passed in, so the rule can be tested without a running player loop.
    internal sealed class TextMeshCache
    {
        private readonly Dictionary<string, CachedText> entries = new();
        private readonly List<string> keysToEvict = new();
        private readonly int sizeBeforeEviction;
        private readonly int sizeBeforeClearing;
        private int lastEvictionFrame = -1;

        private struct CachedText
        {
            public Mesh Mesh;
            public int LastUsedFrame;
        }

        //sizeBeforeClearing is the backstop for when nothing can be evicted, e.g. in edit mode where the
        //frame count may not advance
        public TextMeshCache(int sizeBeforeEviction, int sizeBeforeClearing)
        {
            this.sizeBeforeEviction = sizeBeforeEviction;
            this.sizeBeforeClearing = sizeBeforeClearing;
        }

        public int Count => entries.Count;

        public bool TryGet(string key, int frame, out Mesh mesh)
        {
            mesh = null;
            if (!entries.TryGetValue(key, out var cached) || cached.Mesh == null) return false;

            RememberUse(key, cached.Mesh, frame);
            mesh = cached.Mesh;
            return true;
        }

        public void MarkUsed(string key, int frame)
        {
            if (entries.TryGetValue(key, out var cached)) RememberUse(key, cached.Mesh, frame);
        }

        public void Add(string key, Mesh mesh, int frame)
        {
            EvictOrClearWhenFull(frame);
            RememberUse(key, mesh, frame);
        }

        public void Clear()
        {
            foreach (var cached in entries.Values) DestroyMesh(cached.Mesh);
            entries.Clear();
        }

        private void RememberUse(string key, Mesh mesh, int frame)
            => entries[key] = new CachedText { Mesh = mesh, LastUsedFrame = frame };

        private void EvictOrClearWhenFull(int frame)
        {
            if (entries.Count >= sizeBeforeClearing) Clear();
            else if (entries.Count >= sizeBeforeEviction && frame != lastEvictionFrame) EvictNotUsedIn(frame);
        }

        private void EvictNotUsedIn(int frame)
        {
            lastEvictionFrame = frame;
            keysToEvict.Clear();

            foreach (var pair in entries)
            {
                if (pair.Value.LastUsedFrame != frame) keysToEvict.Add(pair.Key);
            }

            foreach (var key in keysToEvict)
            {
                DestroyMesh(entries[key].Mesh);
                entries.Remove(key);
            }
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) Object.Destroy(mesh);
            else Object.DestroyImmediate(mesh);
        }
    }
}
