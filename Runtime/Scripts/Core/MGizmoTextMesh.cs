using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Builds a flat mesh of glyph quads from a string using a dynamic Font, so text can be drawn through
    //the same Graphics.DrawMesh path as every other gizmo. The block is centred on the local origin so a
    //billboard draw call can face it at the camera. Pair it with the font's material (font.material).
    //
    //Meshes are cached by font+size+text and reused, so calling this every frame (e.g. a hover label)
    //does not leak a mesh per call. A cached mesh stops being valid when its font's atlas is rebuilt (the
    //glyph UVs it baked in are gone) or when the cache is evicted. Either way Version changes, and a
    //holder that outlives the call - a retained label - builds again; see MGizmoTextDrawCall.Draw.
    public static class MGizmoTextMesh
    {
        private const int RenderFontSize = 64;
        private const int MaxCacheSize = 256;

        private readonly struct Entry
        {
            public readonly Mesh Mesh;
            public readonly Font Font;

            public Entry(Mesh mesh, Font font)
            {
                Mesh = mesh;
                Font = font;
            }
        }

        private static readonly Dictionary<string, Entry> cache = new();
        private static readonly List<string> keyScratch = new();
        private static readonly List<Mesh> retired = new();
        private static bool destroyScheduled;

        /// <summary>
        /// Changes whenever meshes this class handed out stopped being valid. Anything that keeps a mesh
        /// past the call that built it compares this and calls <see cref="Build"/> again.
        /// </summary>
        public static int Version { get; private set; }

        static MGizmoTextMesh()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            Font.textureRebuilt += OnFontTextureRebuilt;
        }

        public static Mesh Build(string text, Font font, float size)
        {
            if (string.IsNullOrEmpty(text) || font == null) return null;

            string key = font.GetInstanceID() + ":" + size.ToString("0.###") + ":" + text;
            if (cache.TryGetValue(key, out var cached) && cached.Mesh != null) return cached.Mesh;

            //make sure every glyph we need is in the atlas before we read its UVs
            font.RequestCharactersInTexture(text, RenderFontSize, FontStyle.Normal);

            float scale = size / RenderFontSize;
            //line spacing is tied to the requested label size, not font.lineHeight: the glyphs are
            //rasterised at RenderFontSize but font.lineHeight reports the font's import size, so mixing
            //them collapses the spacing and the lines render on top of each other
            float lineHeight = size * 1.25f;

            string[] lines = text.Split('\n');
            float totalHeight = lines.Length * lineHeight;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            //start at the top line and work down; the whole block is centred vertically on the origin
            float cursorY = totalHeight * 0.5f - lineHeight;

            foreach (var line in lines)
            {
                float cursorX = -MeasureWidth(line, font, scale) * 0.5f;  //centre each line horizontally

                foreach (char c in line)
                {
                    if (!font.GetCharacterInfo(c, out var info, RenderFontSize, FontStyle.Normal)) continue;

                    float minX = cursorX + info.minX * scale;
                    float maxX = cursorX + info.maxX * scale;
                    float minY = cursorY + info.minY * scale;
                    float maxY = cursorY + info.maxY * scale;

                    int v = vertices.Count;

                    vertices.Add(new Vector3(minX, minY, 0f));
                    vertices.Add(new Vector3(minX, maxY, 0f));
                    vertices.Add(new Vector3(maxX, maxY, 0f));
                    vertices.Add(new Vector3(maxX, minY, 0f));

                    uvs.Add(info.uvBottomLeft);
                    uvs.Add(info.uvTopLeft);
                    uvs.Add(info.uvTopRight);
                    uvs.Add(info.uvBottomRight);

                    colors.Add(Color.white);
                    colors.Add(Color.white);
                    colors.Add(Color.white);
                    colors.Add(Color.white);

                    triangles.Add(v);
                    triangles.Add(v + 1);
                    triangles.Add(v + 2);
                    triangles.Add(v);
                    triangles.Add(v + 2);
                    triangles.Add(v + 3);

                    cursorX += info.advance * scale;
                }

                cursorY -= lineHeight;
            }

            var mesh = new Mesh { name = "MGizmoText" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            if (cache.Count >= MaxCacheSize) Invalidate(null);
            cache[key] = new Entry(mesh, font);
            return mesh;
        }

        private static float MeasureWidth(string line, Font font, float scale)
        {
            float width = 0f;
            foreach (char c in line)
            {
                if (font.GetCharacterInfo(c, out var info, RenderFontSize, FontStyle.Normal))
                {
                    width += info.advance * scale;
                }
            }

            return width;
        }

        private static void OnFontTextureRebuilt(Font font) => Invalidate(font);

        //Drops the cached meshes of one font, or of every font when none is given. Unity raises
        //textureRebuilt from wherever the atlas happened to grow, which includes Canvas rendering, where
        //DestroyImmediate is illegal - so nothing is destroyed here, only retired.
        private static void Invalidate(Font font)
        {
            keyScratch.Clear();

            foreach (var pair in cache)
            {
                if (ReferenceEquals(font, null) || ReferenceEquals(pair.Value.Font, font)) keyScratch.Add(pair.Key);
            }

            if (keyScratch.Count == 0) return;

            for (int i = 0; i < keyScratch.Count; i++)
            {
                Retire(cache[keyScratch[i]].Mesh);
                cache.Remove(keyScratch[i]);
            }

            keyScratch.Clear();
            Version++;
        }

        private static void Retire(Mesh mesh)
        {
            if (mesh == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(mesh);
                return;
            }

#if UNITY_EDITOR
            //Destroy is not available in edit mode and DestroyImmediate is not legal from every caller,
            //so the mesh waits for the editor loop
            retired.Add(mesh);
            if (destroyScheduled) return;

            destroyScheduled = true;
            UnityEditor.EditorApplication.delayCall += DestroyRetired;
#endif
        }

#if UNITY_EDITOR
        private static void DestroyRetired()
        {
            destroyScheduled = false;

            for (int i = 0; i < retired.Count; i++)
            {
                if (retired[i] == null) continue;

                if (Application.isPlaying) Object.Destroy(retired[i]);
                else Object.DestroyImmediate(retired[i]);
            }

            retired.Clear();
        }
#endif
    }
}
