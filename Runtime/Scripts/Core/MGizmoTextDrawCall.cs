using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    //A text label. It reuses MGizmoDrawCall's mesh-drawing path but, when billboard is on, adopts the
    //rendering camera's orientation each frame so the label stays upright and screen-facing. Each camera
    //gets its own clone (see MGizmos.AddMeshDrawCall), so each clone faces its own camera. The glyph mesh
    //is built by MGizmoTextMesh and drawn with the font's material, whose GUI/Text shader is double-sided
    //and tints by colour.
    public class MGizmoTextDrawCall : MGizmoDrawCall
    {
        private bool billboard;

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

        public override void Draw(Camera camera, float deltaTime)
        {
            if (billboard && camera != null)
            {
                //screen-aligned: by adopting the camera's rotation the label's +X axis matches the
                //camera's right, so text reads left-to-right and never mirrors wherever the camera is.
                rotation = camera.transform.rotation;
                matrix = Matrix4x4.TRS(position, rotation, scale);
            }

            base.Draw(camera, deltaTime);
        }

        public override MGizmoBaseDrawCall Clone()
        {
            //mirror MGizmoDrawCall.Clone but keep this concrete type and the billboard flag
            var dc = pool.Count > 0 ? pool.Pop() : new MGizmoTextDrawCall();
            dc.pooled = false;
            dc.CopyFrom(this);
            dc.billboard = billboard;
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
