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
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Color defaultColor;
        
        [Header("Meshes")]
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh cylinderMesh;
        [SerializeField] private Mesh planeMesh;
        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Mesh arrowHead;

        private Dictionary<Camera, List<BaseMeshDrawCall>> meshDrawCalls = new();
      
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
        }

        public BaseMeshDrawCall DrawSphere(Vector3 position, float radius)
        {
            MeshDrawCall drawCall = new MeshDrawCall(sphereMesh, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
            AddMeshDrawCall(drawCall);
            return drawCall;
        }
        
        public BaseMeshDrawCall DrawCylinder(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(cylinderMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);
            return drawCall;
        }

        public BaseMeshDrawCall DrawArrow(Vector3 from, Vector3 to, float stemWidth, float arrowHeadSize)
        {
            var compositeMeshDrawCall = new CompositeMeshDrawCall();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;

            MeshDrawCall cylinderDrawCall = new MeshDrawCall(cylinderMesh, from + (dir * (d / 2.0f)) - (dir * (headLength / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth));
            MeshDrawCall arrowHeadDrawCall = new MeshDrawCall(arrowHead, to - (dir * headLength), Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(-90, 0, 0), Vector3.one * arrowHeadSize);
            
            compositeMeshDrawCall.AddDrawCall(cylinderDrawCall);
            compositeMeshDrawCall.AddDrawCall(arrowHeadDrawCall);
            AddMeshDrawCall(compositeMeshDrawCall);
            
            return compositeMeshDrawCall;
        }
        
        public BaseMeshDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;

            MeshDrawCall drawCall = new MeshDrawCall(cylinderMesh, from + (dir * (d / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(lineWidth, d / 2.0f, lineWidth));
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        private void AddMeshDrawCall(BaseMeshDrawCall meshDrawCall)
        {
            meshDrawCall.SetColor(defaultColor)
                .SetMaterial(defaultMaterial);
            
            var cameras = meshDrawCalls.Keys;

            foreach (var camera in cameras)
            {
                meshDrawCalls[camera].Add(meshDrawCall);
            }
        }

        public BaseMeshDrawCall DrawCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(cubeMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public BaseMeshDrawCall DrawQuad(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(quadMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public BaseMeshDrawCall DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(mesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }

        public BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius)
        {
            return RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        }

        public BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth, Vector3 upwards)
        {
            var compositeMeshDrawCall = new CompositeMeshDrawCall();
            
            Vector3[] positions = new Vector3[sides];
            Vector3 right = Quaternion.Euler(0, 0, 90) * upwards;
            Vector3 forward = Vector3.Cross(upwards, right);
            
            for (int currentSide = 0; currentSide < sides; currentSide++)
            {
                float p = (float) currentSide / sides;
                float currentRadian = p * 2 * Mathf.PI;

                float xScaled = Mathf.Cos(currentRadian);
                float yScaled = Mathf.Sin(currentRadian);

                float x = xScaled * radius;
                float y = yScaled * radius;

                Vector3 position = center;
                position += right * x;
                position += forward * y;

                positions[currentSide] = position;
            }

            for (int i = 1; i < positions.Length; i++)
            {
                Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                var dc = RenderLine(positions[i - 1], positions[i] + (dir * 0.01f), lineWidth);
                compositeMeshDrawCall.AddDrawCall(dc);
            }
            
            var lastDC = RenderLine(positions[positions.Length - 1], positions[0], lineWidth);
            compositeMeshDrawCall.AddDrawCall(lastDC);
            
            return compositeMeshDrawCall;
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            HandleCameraDrawCalls(sceneView.camera, 1 / 30.0f);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            HandleCameraDrawCalls(camera, Time.deltaTime);
        }

        public void HandleCameraDrawCalls(Camera camera, float deltaTime)
        {
            //if the camera doesnt exist add it
            if (!meshDrawCalls.TryGetValue(camera, out var drawCalls))
            {
                meshDrawCalls.Add(camera, new List<BaseMeshDrawCall>());
                return;
            }
            
            for (int i = drawCalls.Count - 1; i >= 0; i--)
            {
                drawCalls[i].Draw(camera, deltaTime);
                
                if (drawCalls[i].RemainingTime < 0)
                {
                    drawCalls.RemoveAt(i);
                }
            }
        }
    }
}

