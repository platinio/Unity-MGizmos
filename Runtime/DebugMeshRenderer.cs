using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Platinio
{
    [CreateAssetMenu(menuName = "Singletons/Debug Mesh Renderer")]
    public class DebugMeshRenderer : ScriptableSingleton<DebugMeshRenderer>
    {
        [SerializeField] private Material material;
        
        [Header("Meshes")]
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh cylinderMesh;
        [SerializeField] private Mesh planeMesh;
        [SerializeField] private Mesh quadMesh;

        private List<MeshDrawCall> meshDrawCalls = new();


        protected override void OnEnableEvent()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        protected override void OnDisableEvent()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        public void DrawSphere(Vector3 position, Quaternion rotation, float radius, Color color, float duration = 0)
        {
            meshDrawCalls.Add(new MeshDrawCall(sphereMesh, position, rotation, Vector3.one * (radius * 2.0f), color, duration));
        }
        
        public void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float duration = 0)
        {
            meshDrawCalls.Add(new MeshDrawCall(cubeMesh, position, rotation, scale, color, duration));
        }

        public void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            for (int i = meshDrawCalls.Count - 1; i >= 0; i--)
            {
                meshDrawCalls[i].Draw(material);
                if (meshDrawCalls[i].Duration < 0)
                {
                    meshDrawCalls.RemoveAt(i);
                }
            }
        }
    }

    public class MeshDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Color color;
        protected Mesh mesh;
        protected float duration;

        public float Duration => duration;

        public MeshDrawCall(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float duration)
        {
            this.mesh = mesh;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.color = color;
            this.duration = duration;
        }

        public void Draw(Material material)
        {
            duration -= Time.deltaTime;
            material.color = color;
            material.SetPass(0);
            var matrix = Matrix4x4.TRS(position, rotation, scale);
           
            Graphics.DrawMeshNow(mesh, matrix, 0);
        }
    }
}

