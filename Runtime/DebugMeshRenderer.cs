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

        private Dictionary<Camera, List<MeshDrawCall>> meshDrawCalls = new();
        private Camera lastActiveSceneViewCamera;
        private List<Material> materialPool = new List<Material>();

        protected override void OnEnableEvent()
        {
            meshDrawCalls.Clear();
            materialPool.Clear();
            lastActiveSceneViewCamera = null;
            
            RenderPipelineManager.beginCameraRendering -= OnEndCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnEndCameraRendering;
            var cameras = GetCameras();
            
            #if UNITY_EDITOR
            lastActiveSceneViewCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
            #endif
            
            foreach (var camera in cameras)
            {
                meshDrawCalls.Add(camera, new List<MeshDrawCall>());
            }
        }

        protected override void OnUpdateEvent()
        {
            #if UNITY_EDITOR
            if (UnityEditor.SceneView.lastActiveSceneView == null) return;
            if (lastActiveSceneViewCamera != UnityEditor.SceneView.lastActiveSceneView.camera)
            {
                meshDrawCalls.Remove(lastActiveSceneViewCamera);
                lastActiveSceneViewCamera = UnityEditor.SceneView.lastActiveSceneView.camera;
                meshDrawCalls.Add(lastActiveSceneViewCamera, new List<MeshDrawCall>());
                
            }
            #endif
        }

        private List<Camera> GetCameras()
        {
            List<Camera> cameras = new List<Camera>();
            
            #if UNITY_EDITOR
            cameras.Add(UnityEditor.SceneView.lastActiveSceneView.camera);
            #endif

            foreach (var camera in Camera.allCameras)
            {
                cameras.Add(camera);
            }

            return cameras;
        }
        
        protected override void OnDisableEvent()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        public void DrawSphere(Vector3 position, Quaternion rotation, float radius, Color color, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(sphereMesh, CreateMaterial(), position, rotation, Vector3.one * (radius * 2.0f), color, duration));
        }

        private void AddMeshDrawCall(MeshDrawCall meshDrawCall)
        {
            var cameras = meshDrawCalls.Keys;

            foreach (var camera in cameras)
            {
                meshDrawCalls[camera].Add(meshDrawCall);
            }
        }

        public void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(cubeMesh, CreateMaterial(), position, rotation, scale, color, duration));
        }
        
        public void DrawQuad(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(quadMesh, CreateMaterial(), position, rotation, scale, color, duration));
        }

        public void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!meshDrawCalls.TryGetValue(camera, out var drawCalls)) return;
            
            for (int i = drawCalls.Count - 1; i >= 0; i--)
            {
                drawCalls[i].Draw(camera);
                if (drawCalls[i].Duration < 0)
                {
                    if (!materialPool.Contains(drawCalls[i].Material)) materialPool.Add(drawCalls[i].Material);
                    drawCalls.RemoveAt(i);
                }
            }
        }

        private Material CreateMaterial()
        {
            if (materialPool.Count > 0)
            {
                var m = materialPool[0];
                materialPool.RemoveAt(0);
                return m;
            }

            var newMat = Instantiate(material);
            return newMat;
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

        public void Draw(Camera camera)
        {
            if (camera == null || material == null)
            {
                duration = float.MinValue;
                return;
            }

            duration -= Time.deltaTime;
            material.color = color;
            var matrix = Matrix4x4.TRS(position, rotation, scale);
           
            Graphics.DrawMesh(mesh, matrix, material, 0, camera);
        }
    }
}

