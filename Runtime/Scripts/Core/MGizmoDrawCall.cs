using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    public class MGizmoDrawCall : MGizmoBaseDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Mesh mesh;
        protected float duration;
        protected Material material;
        private MaterialPropertyBlock materialPropertyBlock;
        private ShadowCastingMode shadowCastingMode;
        private bool receiveShadows;
        
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        public override float RemainingTime => duration;
        public Material Material => material;

        public override MaterialPropertyBlock MaterialPropertyBlock => materialPropertyBlock;
        
        private float timer;
        
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

            materialPropertyBlock = new MaterialPropertyBlock();
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
            materialPropertyBlock.SetColor(ColorPropertyId, color);
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
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            
            Graphics.DrawMesh(mesh, matrix, material, 0, camera, 0, materialPropertyBlock, shadowCastingMode, receiveShadows);
        }

        public override MGizmoBaseDrawCall Clone()
        {
            var dc = new MGizmoDrawCall(mesh, position, rotation, scale);
            dc.duration = duration;
            dc.material = material;
            dc.materialPropertyBlock = materialPropertyBlock;

            return dc;
        }
    }
}