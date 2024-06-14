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
            RenderPipelineManager.endCameraRendering += OnPostRender;
        }

        protected override void OnDisableEvent()
        {
            RenderPipelineManager.endCameraRendering -= OnPostRender;
        }

        public void DrawSphere(Vector3 position, Quaternion rotation, float radius, Color color)
        {
            meshDrawCalls.Add(new MeshDrawCall(sphereMesh, position, rotation, Vector3.one * (radius * 2.0f), color));
        }
        
        public void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            meshDrawCalls.Add(new MeshDrawCall(cubeMesh, position, rotation, scale, color));
        }

        public void OnPostRender(ScriptableRenderContext context, Camera camera)
        {
            foreach (var meshDrawCall in meshDrawCalls)
            {
                meshDrawCall.Draw(material);
            }
            
            meshDrawCalls.Clear();
        }
    }

    public class MeshDrawCall
    {
        protected Vector3 position;
        protected Vector3 scale;
        protected Quaternion rotation;
        protected Color color;
        protected Mesh mesh;

        public MeshDrawCall(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            this.mesh = mesh;
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
            this.color = color;
        }

        public void Draw(Material material)
        {
            material.color = color;
            material.SetPass(0);
            var matrix = Matrix4x4.TRS(position, rotation, scale);
           
            Graphics.DrawMeshNow(mesh, matrix, 0);
        }
    }
}

