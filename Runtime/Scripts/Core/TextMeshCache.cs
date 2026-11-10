using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Text meshes by key, each with its font and the last frame it was built or drawn. A full cache evicts the
    //labels nobody used this frame, at most once a frame; a frame that uses more labels than the cache holds
    //grows it. Nothing is destroyed here: dropped meshes go to the retire action, because the cache can be
    //invalidated from inside Unity callbacks where destroying is illegal. Version changes on every drop, so a
    //label that keeps a mesh knows to build it again. The frame is passed in, so the rules can be tested
    //without a running player loop.
    internal sealed class TextMeshCache
    {
        private readonly Dictionary<string, CachedText> entries = new();
        private readonly List<string> keysToDrop = new();
        private readonly int sizeBeforeEviction;
        private readonly int sizeBeforeClearing;
        private readonly Action<Mesh> retire;
        private int lastEvictionFrame = -1;

        private struct CachedText
        {
            public Mesh Mesh;
            public Font Font;
            public int LastUsedFrame;
        }

        //sizeBeforeClearing is the backstop for when nothing can be evicted, e.g. in edit mode where the
        //frame count may not advance
        public TextMeshCache(int sizeBeforeEviction, int sizeBeforeClearing, Action<Mesh> retire)
        {
            this.sizeBeforeEviction = sizeBeforeEviction;
            this.sizeBeforeClearing = sizeBeforeClearing;
            this.retire = retire;
        }

        public int Count => entries.Count;

        /// <summary>Changes whenever a mesh this cache handed out stopped being valid.</summary>
        public int Version { get; private set; }

        public bool TryGet(string key, int frame, out Mesh mesh)
        {
            mesh = null;
            if (!entries.TryGetValue(key, out var cached) || cached.Mesh == null) return false;

            RememberUse(key, cached.Mesh, cached.Font, frame);
            mesh = cached.Mesh;
            return true;
        }

        public void MarkUsed(string key, int frame)
        {
            if (entries.TryGetValue(key, out var cached)) RememberUse(key, cached.Mesh, cached.Font, frame);
        }

        public void Add(string key, Mesh mesh, Font font, int frame)
        {
            EvictOrClearWhenFull(frame);
            RememberUse(key, mesh, font, frame);
        }

        /// <summary>Drops the meshes of <paramref name="font"/>, whose atlas was rebuilt and whose glyph UVs are gone.</summary>
        public void InvalidateFont(Font font) => Drop(cached => ReferenceEquals(cached.Font, font));

        public void Clear() => Drop(_ => true);

        private void RememberUse(string key, Mesh mesh, Font font, int frame)
            => entries[key] = new CachedText { Mesh = mesh, Font = font, LastUsedFrame = frame };

        private void EvictOrClearWhenFull(int frame)
        {
            if (entries.Count >= sizeBeforeClearing) Clear();
            else if (entries.Count >= sizeBeforeEviction && frame != lastEvictionFrame) EvictNotUsedIn(frame);
        }

        private void EvictNotUsedIn(int frame)
        {
            lastEvictionFrame = frame;
            Drop(cached => cached.LastUsedFrame != frame);
        }

        private void Drop(Func<CachedText, bool> shouldDrop)
        {
            keysToDrop.Clear();

            foreach (var pair in entries)
            {
                if (shouldDrop(pair.Value)) keysToDrop.Add(pair.Key);
            }

            if (keysToDrop.Count == 0) return;

            foreach (var key in keysToDrop)
            {
                retire(entries[key].Mesh);
                entries.Remove(key);
            }

            Version++;
        }
    }
}
