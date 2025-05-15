using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ArcaneOnyx
{
    public static class MGizmos
    {
        public static DebugMeshRendererConfig Config => DebugMeshRendererConfig.Instance;
        private static Dictionary<Camera, List<BaseMeshDrawCall>> meshDrawCalls = new();
        
        static MGizmos()
        {
            SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
            
            //listen this for scriptable base pipelines
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            
            //listen this for scene view render
            SceneView.duringSceneGui -= DuringSceneGui;
            SceneView.duringSceneGui += DuringSceneGui;
        }
        
        //https://stackoverflow.com/questions/256077/static-finalizer/256278#256278
        private static readonly Destructor Finalise = new Destructor();
        private sealed class Destructor
        {
            ~Destructor()
            {
                SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                SceneView.duringSceneGui -= DuringSceneGui;
            }
        }
        
        private static void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Reset();
        }

        private static void Reset()
        {
            meshDrawCalls.Clear();
        }
        
        private static void AddMeshDrawCall(BaseMeshDrawCall meshDrawCall)
        {
            meshDrawCall.SetColor(Config.DefaultColor)
                .SetMaterial(Config.DefaultMaterial);
            
            var cameras = meshDrawCalls.Keys;

            foreach (var camera in cameras)
            {
                meshDrawCalls[camera].Add(meshDrawCall);
            }
        }
        
        private static void DuringSceneGui(SceneView sceneView)
        {
            HandleCameraDrawCalls(sceneView.camera, 1 / 30.0f);
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            HandleCameraDrawCalls(camera, Time.deltaTime);
        }

        public static void HandleCameraDrawCalls(Camera camera, float deltaTime)
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

        #region Render
        public static BaseMeshDrawCall RenderSphere(Vector3 position, float radius)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.SphereMesh, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
            AddMeshDrawCall(drawCall);
            return drawCall;
        }

        public static BaseMeshDrawCall RenderCylinder(Vector3 position) => RenderCylinder(position, Quaternion.identity, Vector3.one);
        
        public static BaseMeshDrawCall RenderCylinder(Vector3 position, Quaternion rotation) => RenderCylinder(position, rotation, Vector3.one);
        
        public static BaseMeshDrawCall RenderCylinder(Vector3 position, Vector3 scale) => RenderCylinder(position, Quaternion.identity, scale);
        
        public static BaseMeshDrawCall RenderCylinder(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.CylinderMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);
            return drawCall;
        }

        public static BaseMeshDrawCall RenderLine(Vector3 from, Vector3 to) => RenderLine(from, to, 0.01f);

        public static BaseMeshDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;

            MeshDrawCall drawCall = new MeshDrawCall(Config.CylinderMesh, from + (dir * (d / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(lineWidth, d / 2.0f, lineWidth));
            AddMeshDrawCall(drawCall);

            return drawCall;
        }

        public static BaseMeshDrawCall RenderCube(Vector3 position) => RenderCube(position, Quaternion.identity, Vector3.one);
        
        public static BaseMeshDrawCall RenderCube(Vector3 position, Quaternion rotation) => RenderCube(position, rotation, Vector3.one);

        public static BaseMeshDrawCall RenderCube(Vector3 position, Vector3 scale) => RenderCube(position, Quaternion.identity, scale);
        
        public static BaseMeshDrawCall RenderCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.CubeMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }

        public static BaseMeshDrawCall RenderQuad(Vector3 position) => RenderQuad(position, Quaternion.identity, Vector3.one);
        
        public static BaseMeshDrawCall RenderQuad(Vector3 position, Quaternion rotation) => RenderQuad(position, rotation, Vector3.one);
        
        public static BaseMeshDrawCall RenderQuad(Vector3 position, Vector3 scale) => RenderQuad(position, Quaternion.identity, scale);
        
        public static BaseMeshDrawCall RenderQuad(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.QuadMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public static BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius) => RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        
        public static BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius, Vector3 upwards) => RenderCircle(center, sides, radius, 0.01f, upwards);

        public static BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth) => RenderCircle(center, sides, radius, lineWidth, Vector3.up);

        public static BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth, Vector3 upwards)
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

        public static BaseMeshDrawCall RenderMesh(Mesh mesh, Vector3 position) => RenderMesh(mesh, position, Quaternion.identity, Vector3.one);
        
        public static BaseMeshDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation) => RenderMesh(mesh, position, rotation, Vector3.one);
        
        public static BaseMeshDrawCall RenderMesh(Mesh mesh, Vector3 position, Vector3 scale) => RenderMesh(mesh, position, Quaternion.identity, scale);

        public static BaseMeshDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(mesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public static BaseMeshDrawCall RenderArrow(Vector3 from, Vector3 to, float stemWidth = 0.01f, float arrowHeadSize = 0.5f)
        {
            var compositeMeshDrawCall = new CompositeMeshDrawCall();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;

            Vector3 stemStartPosition = from + (dir * (d / 2.0f));
            Vector3 arrowHeadOffset = (dir * (headLength / 2.0f));
            Vector3 stemScale = new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth);
            
            MeshDrawCall cylinderDrawCall = new MeshDrawCall(Config.CylinderMesh,  stemStartPosition - arrowHeadOffset, Quaternion.FromToRotation(Vector3.up, dir), stemScale);

            Quaternion arrowHeadRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(-90, 0, 0);
            Vector3 arrowHeadScale = Vector3.one * arrowHeadSize;
            
            MeshDrawCall arrowHeadDrawCall = new MeshDrawCall(Config.ArrowHead, to - (dir * headLength), arrowHeadRotation, arrowHeadScale);
            
            compositeMeshDrawCall.AddDrawCall(cylinderDrawCall);
            compositeMeshDrawCall.AddDrawCall(arrowHeadDrawCall);
            AddMeshDrawCall(compositeMeshDrawCall);
            
            return compositeMeshDrawCall;
        }
        #endregion
    }
}

