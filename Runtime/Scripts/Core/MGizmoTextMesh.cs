using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //Builds a flat mesh of glyph quads from a string using a dynamic Font, so text can be drawn through
    //the same Graphics.DrawMesh path as every other gizmo. The block is centred on the local origin so a
    //billboard draw call can face it at the camera. Pair it with the font's material (font.material).
    //
    //Meshes are cached by text+size and reused, so calling this every frame (e.g. a hover label) does not
    //leak a mesh per call. The cache is cleared whenever the font atlas is rebuilt, because that
    //invalidates the glyph UVs the cached meshes baked in.
    public static class MGizmoTextMesh
    {
        private const int RenderFontSize = 64;
        private const int MaxCacheSize = 64;

        private static readonly Dictionary<string, Mesh> cache = new();

        static MGizmoTextMesh()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            Font.textureRebuilt += OnFontTextureRebuilt;
        }

        public static Mesh Build(string text, Font font, float size)
        {
            if (string.IsNullOrEmpty(text) || font == null) return null;

            string key = size.ToString("0.###") + ":" + text;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

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

            if (cache.Count >= MaxCacheSize) ClearCache();
            cache[key] = mesh;
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

        private static void OnFontTextureRebuilt(Font font) => ClearCache();

        private static void ClearCache()
        {
            foreach (var mesh in cache.Values) DestroyMesh(mesh);
            cache.Clear();
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) Object.Destroy(mesh);
            else Object.DestroyImmediate(mesh);
        }
    }
}
