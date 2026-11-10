using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //A text label. It reuses MGizmoDrawCall's mesh-drawing path but, when billboard is on, adopts the
    //rendering camera's orientation each frame so the label stays upright and screen-facing. Each camera
    //gets its own clone (see MGizmos.AddMeshDrawCall), so each clone faces its own camera. The glyph mesh
    //is built by MGizmoTextMesh and drawn with the font's material, whose GUI/Text shader is double-sided
    //and tints by colour. It remembers its text so a label kept across frames (in a group, or with a
    //duration) keeps its cached mesh in use, and rebuilds it if an eviction or an atlas rebuild destroyed it.
    public class MGizmoTextDrawCall : MGizmoDrawCall
    {
        private bool billboard;
        private string text;
        private Font font;
        private float size;
        private string cacheKey;

        //recycles the per-camera clones MGizmos creates and destroys - see Clone/Release
        private static readonly Stack<MGizmoTextDrawCall> pool = new();
        private const int MaxPoolSize = 64;

        private MGizmoTextDrawCall()
        {
        }

        public MGizmoTextDrawCall(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, bool billboard)
            : base(mesh, position, rotation, scale)
        {
            this.billboard = billboard;
        }

        /// <summary>A label from the pool, fully reset - see <see cref="MGizmoDrawCall.Get"/> for why.</summary>
        public static MGizmoTextDrawCall Get(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, bool billboard)
        {
            var dc = pool.Count > 0 ? pool.Pop() : new MGizmoTextDrawCall();
            dc.pooled = false;
            dc.Reinitialize(mesh, position, rotation, scale);
            dc.billboard = billboard;
            dc.text = null;
            dc.font = null;
            dc.cacheKey = null;
            return dc;
        }

        /// <summary>The label this draw call shows, so it can rebuild its mesh. Set by <see cref="MGizmos.RenderText(Vector3, string, float, bool)"/>.</summary>
        internal void RememberText(string text, Font font, float size)
        {
            this.text = text;
            this.font = font;
            this.size = size;
            cacheKey = MGizmoTextMesh.KeyOf(text, font, size);
        }

        public override void Draw(Camera camera, float deltaTime)
        {
            KeepMeshAlive();

            if (billboard && camera != null)
            {
                //screen-aligned: by adopting the camera's rotation the label's +X axis matches the
                //camera's right, so text reads left-to-right and never mirrors wherever the camera is.
                rotation = camera.transform.rotation;
                matrix = Matrix4x4.TRS(position, rotation, scale);
            }

            base.Draw(camera, deltaTime);
        }

        private void KeepMeshAlive()
        {
            if (cacheKey == null) return;

            if (mesh == null) mesh = MGizmoTextMesh.Build(text, font, size);
            else MGizmoTextMesh.MarkUsed(cacheKey);
        }

        public override MGizmoBaseDrawCall Clone()
        {
            //mirror MGizmoDrawCall.Clone but keep this concrete type and the billboard flag
            var dc = pool.Count > 0 ? pool.Pop() : new MGizmoTextDrawCall();
            dc.pooled = false;
            dc.CopyFrom(this);
            dc.billboard = billboard;
            dc.text = text;
            dc.font = font;
            dc.size = size;
            dc.cacheKey = cacheKey;
            return dc;
        }

        internal override void Release()
        {
            if (pooled) return;
            if (GetType() != typeof(MGizmoTextDrawCall) || pool.Count >= MaxPoolSize) return;

            pooled = true;
            pool.Push(this);
        }
    }
}
