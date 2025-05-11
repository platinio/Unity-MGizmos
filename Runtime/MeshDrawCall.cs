using UnityEngine;
using UnityEngine.Rendering;

namespace Platinio
{
    public class MeshDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Color? color;
        protected Mesh mesh;
        protected float duration;
        protected Material material;

        public float Duration => duration;
        public Material Material => material;

        public MeshDrawCall(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float duration)
        {
            this.mesh = mesh;
            this.material = material;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.color = color;
            this.duration = duration;
        }
        
        public MeshDrawCall(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale, float duration)
        {
            this.mesh = mesh;
            this.material = material;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            color = null;
            this.duration = duration;
        }

        public void Draw(Camera camera)
        {
            if (camera == null || material == null)
            {
                duration = float.MinValue;
                return;
            }
           
            duration -= Time.deltaTime;
            if (color.HasValue) material.color = color.Value;
            var matrix = Matrix4x4.TRS(position, rotation, scale);
           
            Graphics.DrawMesh(mesh, matrix, material, 0, camera, 0, new MaterialPropertyBlock(), ShadowCastingMode.On, true);
        }
    }
}