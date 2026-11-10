using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Builds a flat mesh of glyph quads from a string using a dynamic Font, so text can be drawn through
    //the same Graphics.DrawMesh path as every other gizmo. The block is centred on the local origin so a
    //billboard draw call can face it at the camera. Pair it with the font's material (font.material).
    //
    //Meshes are cached by font, size and text and reused, so calling this every frame (e.g. a hover label)
    //does not leak a mesh per call. A cached mesh stops being valid when the cache evicts it (it holds the
    //labels nobody built or drew this frame; see TextMeshCache) or when its font's atlas is rebuilt, since the
    //glyph UVs it baked in are gone. Either way Version changes, and a label that keeps its mesh across frames
    //builds it again (see MGizmoTextDrawCall). Dropped meshes are retired, never destroyed on the spot: Unity
    //rebuilds a font atlas from inside Canvas rendering, where DestroyImmediate is illegal.
    public static class MGizmoTextMesh
    {
        internal const int RenderFontSize = 64;
        private const int CacheSizeBeforeEviction = 64;
        private const int CacheSizeBeforeClearing = 1024;

        private static readonly TextMeshCache cache = new(CacheSizeBeforeEviction, CacheSizeBeforeClearing, Retire);
        private static readonly List<Mesh> retiredInEditMode = new();
        private static bool isDestroyScheduled;

        static MGizmoTextMesh()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            Font.textureRebuilt += OnFontTextureRebuilt;
        }

        /// <summary>
        /// Changes whenever meshes this class handed out stopped being valid. Anything that keeps a mesh past the
        /// call that built it compares this and calls <see cref="Build"/> again.
        /// </summary>
        public static int Version => cache.Version;

        public static Mesh Build(string text, Font font, float size)
        {
            if (string.IsNullOrEmpty(text) || font == null) return null;

            string key = KeyOf(text, font, size);
            if (cache.TryGet(key, Time.frameCount, out var cached)) return cached;

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

            cache.Add(key, mesh, font, Time.frameCount);
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

        /// <summary>The cache key of a label: the font matters too, because the mesh holds that font's atlas UVs.</summary>
        internal static string KeyOf(string text, Font font, float size)
            => font.GetInstanceID() + ":" + size.ToString("0.###") + ":" + text;

        /// <summary>A label drawn again this frame without being rebuilt: keeps its mesh out of eviction.</summary>
        internal static void MarkUsed(string key) => cache.MarkUsed(key, Time.frameCount);

        private static void OnFontTextureRebuilt(Font font) => cache.InvalidateFont(font);

        private static void Retire(Mesh mesh)
        {
            if (mesh == null) return;

            if (Application.isPlaying)
            {
                Object.Destroy(mesh);
                return;
            }

#if UNITY_EDITOR
            //Destroy is not available in edit mode and DestroyImmediate is not legal from every caller, so the
            //mesh waits for the editor loop
            retiredInEditMode.Add(mesh);
            if (isDestroyScheduled) return;

            isDestroyScheduled = true;
            UnityEditor.EditorApplication.delayCall += DestroyRetired;
#endif
        }

#if UNITY_EDITOR
        private static void DestroyRetired()
        {
            isDestroyScheduled = false;

            foreach (var mesh in retiredInEditMode)
            {
                if (mesh == null) continue;

                if (Application.isPlaying) Object.Destroy(mesh);
                else Object.DestroyImmediate(mesh);
            }

            retiredInEditMode.Clear();
        }
#endif
    }
}
