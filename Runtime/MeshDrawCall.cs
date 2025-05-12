using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx
{
    public class MeshDrawCall : BaseMeshDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Color color;
        protected Mesh mesh;
        protected float duration;
        protected Material material;
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        public override float RemainingTime => duration;
        public Material Material => material;

        private float timer;
        
        public MeshDrawCall()
        {
        }

        public MeshDrawCall(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.mesh = mesh;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.duration = 0;
        }

        public override BaseMeshDrawCall SetMaterial(Material material)
        {
            this.material = material;
            return this;
        }

        public override BaseMeshDrawCall SetColor(Color color)
        {
            this.color = color;
            return this;
        }

        public override BaseMeshDrawCall SetDuration(float duration)
        {
            this.duration = duration;
            return this;
        }

        public override void Draw(Camera camera, float deltaTime)
        {
            if (camera == null || material == null)
            {
                duration = float.MinValue;
                return;
            }
           
            duration -= deltaTime;
            var matrix = Matrix4x4.TRS(position, rotation, scale);

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor(ColorPropertyId, color);
            
            Graphics.DrawMesh(mesh, matrix, material, 0, camera, 0, propertyBlock, ShadowCastingMode.On, true);
        }
    }
}