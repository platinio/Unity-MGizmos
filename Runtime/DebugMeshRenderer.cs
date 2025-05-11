using System.Collections.Generic;
using ArcaneOnyx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Platinio
{
    [CreateAssetMenu(menuName = "Singletons/Debug Mesh Renderer")]
    public class DebugMeshRenderer : ScriptableSingleton<DebugMeshRenderer>
    {
        [SerializeField] private Material material;
        [SerializeField] private Material raycastMaterial;
        [SerializeField] private Material hitMaterial;
        [SerializeField] private Material normalMaterial;
        
        [Header("Meshes")]
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh cylinderMesh;
        [SerializeField] private Mesh planeMesh;
        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Mesh arrowHead;

        private Dictionary<Camera, List<MeshDrawCall>> meshDrawCalls = new();
        private Camera lastActiveSceneViewCamera;
        private List<Material> materialPool = new List<Material>();

        protected override void OnEnableEvent()
        {
            Reset();
            
            SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
            
            //listen this for scriptable base pipelines
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            
            //listen this for scene view render
            SceneView.duringSceneGui -= DuringSceneGui;
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnBeginCameraRendering;
            SceneView.duringSceneGui -= DuringSceneGui;
            SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
        }
        
        private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Reset();
        }

        private void Reset()
        {
            meshDrawCalls.Clear();
            materialPool.Clear();
        }

        public void DrawSphere(Vector3 position, Quaternion rotation, float radius, Color color, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(sphereMesh, CreateMaterial(), position, rotation, Vector3.one * (radius * 2.0f), color, duration));
        }
        
        public void DrawSphere(Vector3 position, Quaternion rotation, float radius, Material mat, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(sphereMesh, mat, position, rotation, Vector3.one * (radius * 2.0f), duration));
        }
        
        public void DrawCylinder(Vector3 position, Quaternion rotation, Vector3 scale, Material mat, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(cylinderMesh, mat, position, rotation, scale, duration));
        }

        public void DrawArrow(Vector3 from, Vector3 to, float stemWidth, float arrowHeadSize, Material mat, float duration = 0)
        {
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;
            
            AddMeshDrawCall(new MeshDrawCall(cylinderMesh, mat, from + (dir * (d / 2.0f)) - (dir * (headLength / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth), duration));
            AddMeshDrawCall(new MeshDrawCall(arrowHead, mat, to - (dir * headLength), Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(-90, 0, 0), Vector3.one * arrowHeadSize, duration));
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
        
        public void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material mat, float duration = 0)
        {
            AddMeshDrawCall(new MeshDrawCall(mesh, mat, position, rotation, scale, duration));
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            HandleCameraDrawCalls(sceneView.camera);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            HandleCameraDrawCalls(camera);
        }

        public void HandleCameraDrawCalls(Camera camera)
        {
            //if the camera doesnt exist add it
            if (!meshDrawCalls.TryGetValue(camera, out var drawCalls))
            {
                meshDrawCalls.Add(camera, new List<MeshDrawCall>());
                return;
            }
            
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
}

