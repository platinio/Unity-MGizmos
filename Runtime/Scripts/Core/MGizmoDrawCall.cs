using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    public class MGizmoDrawCall : MGizmoBaseDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Matrix4x4 matrix = Matrix4x4.identity;
        protected Mesh mesh;
        protected float duration;
        protected Material material;
        protected Color color = Color.white;
        protected MaterialPropertyBlock materialPropertyBlock;
        private bool colorDirty;
        private ShadowCastingMode shadowCastingMode;
        private bool receiveShadows;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

        //recycles the per-camera clones MGizmos creates and destroys on every query - see Clone/Release
        private static readonly Stack<MGizmoDrawCall> pool = new();
        private const int MaxPoolSize = 1024;

        public override float RemainingTime => duration;
        public Material Material => material;

        //created on demand: instanced draws never need a block, and it is the single biggest per-gizmo
        //allocation (a finalizable native-backed object), so most gizmos now never pay for one
        public override MaterialPropertyBlock MaterialPropertyBlock
        {
            get
            {
                if (materialPropertyBlock == null) materialPropertyBlock = new MaterialPropertyBlock();
                return materialPropertyBlock;
            }
        }

        public MGizmoDrawCall()
        {
        }

        public MGizmoDrawCall(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.mesh = mesh;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.duration = 0;

            //position/rotation/scale never change after construction, so the matrix is built once here
            //instead of every frame (billboard text is the exception and rebuilds it in its Draw)
            matrix = Matrix4x4.TRS(position, rotation, scale);
        }

        public override MGizmoBaseDrawCall SetMaterial(Material material)
        {
            if (material == null) return this;
            this.material = material;
            return this;
        }

        public override MGizmoBaseDrawCall SetColor(Color color)
        {
            if (!MGizmos.IsEnable) return this;
            //stored as a plain field; the DrawMesh fallback mirrors it into the property block lazily,
            //the instanced path reads it directly
            this.color = color;
            colorDirty = true;
            return this;
        }

        public override MGizmoBaseDrawCall SetDuration(float duration)
        {
            this.duration = duration;
            return this;
        }

        public override MGizmoBaseDrawCall SetShadowCastingMode(ShadowCastingMode shadowCastingMode)
        {
            this.shadowCastingMode = shadowCastingMode;
            return this;
        }

        public override MGizmoBaseDrawCall SetReceiveShadows(bool value)
        {
            receiveShadows = value;
            return this;
        }

        public override void Draw(Camera camera, float deltaTime)
        {
            if (!MGizmos.IsEnable)
            {
                duration = float.MinValue;
                return;
            }

            if (camera == null || material == null)
            {
                duration = float.MinValue;
                return;
            }

            duration -= deltaTime;

            if (MGizmoInstancedBatcher.CanBatch(material))
            {
                //batched: MGizmos.HandleCameraDrawCalls flushes everything submitted here as a few
                //Graphics.DrawMeshInstanced calls targeting this same camera
                MGizmoInstancedBatcher.Submit(mesh, material, matrix, color, shadowCastingMode, receiveShadows);
            }
            else
            {
                if (colorDirty)
                {
                    var block = MaterialPropertyBlock;
                    block.SetColor(ColorPropertyId, color);
                    block.SetColor(BaseColorPropertyId, color);
                    colorDirty = false;
                }

                Graphics.DrawMesh(mesh, matrix, material, 0, camera, 0, materialPropertyBlock, shadowCastingMode, receiveShadows);
            }
        }

        public override MGizmoBaseDrawCall Clone()
        {
            var dc = pool.Count > 0 ? pool.Pop() : new MGizmoDrawCall();
            dc.pooled = false;
            dc.CopyFrom(this);
            return dc;
        }

        internal override void Release()
        {
            if (pooled) return;

            //only pool exact instances: a user subclass reaching this base Release must not end up in
            //this pool and later be handed out where a plain MGizmoDrawCall is expected
            if (GetType() != typeof(MGizmoDrawCall) || pool.Count >= MaxPoolSize) return;

            pooled = true;
            pool.Push(this);
        }

        protected void CopyFrom(MGizmoDrawCall source)
        {
            mesh = source.mesh;
            position = source.position;
            rotation = source.rotation;
            scale = source.scale;
            matrix = source.matrix;
            duration = source.duration;
            material = source.material;
            color = source.color;
            shadowCastingMode = source.shadowCastingMode;
            receiveShadows = source.receiveShadows;
            KeepOneFrame = false;
            AddThisFrame = false;
            colorDirty = true;

            //share the source's block when it has one so user customizations reach every clone;
            //otherwise keep our own from a previous pooled life, wiped of stale values (the colour is
            //rewritten on demand thanks to colorDirty)
            if (source.materialPropertyBlock != null) materialPropertyBlock = source.materialPropertyBlock;
            else materialPropertyBlock?.Clear();
        }
    }
}
