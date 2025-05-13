using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ArcaneOnyx
{
    public static class DebugMeshRenderer
    {
        public static DebugMeshRendererConfig Config => DebugMeshRendererConfig.Instance;
        
        private static Dictionary<Camera, List<BaseMeshDrawCall>> meshDrawCalls = new();

        private static Material particlesStandardUnlit;
        private static Material guiText;
       
        public static Material ParticlesStandardUnlit
        {
            get
            {
                if (particlesStandardUnlit == null)
                {
                    particlesStandardUnlit = new Material(Shader.Find("Particles/Standard Unlit"));
                }

                return particlesStandardUnlit;
            }
        }
        
        public static Material GUIText
        {
            get
            {
                if (guiText == null)
                {
                    guiText = new Material(Shader.Find("GUI/Text Shader"));
                }

                return guiText;
            }
        }
        
        static DebugMeshRenderer()
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
        
        private static void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Reset();
        }

        private static void Reset()
        {
            meshDrawCalls.Clear();
        }

        public static BaseMeshDrawCall DrawSphere(Vector3 position, float radius)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.SphereMesh, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
            AddMeshDrawCall(drawCall);
            return drawCall;
        }
        
        public static BaseMeshDrawCall DrawCylinder(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.CylinderMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);
            return drawCall;
        }

        public static BaseMeshDrawCall DrawArrow(Vector3 from, Vector3 to, float stemWidth, float arrowHeadSize)
        {
            var compositeMeshDrawCall = new CompositeMeshDrawCall();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;

            MeshDrawCall cylinderDrawCall = new MeshDrawCall(Config.CylinderMesh, from + (dir * (d / 2.0f)) - (dir * (headLength / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth));
            MeshDrawCall arrowHeadDrawCall = new MeshDrawCall(Config.ArrowHead, to - (dir * headLength), Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(-90, 0, 0), Vector3.one * arrowHeadSize);
            
            compositeMeshDrawCall.AddDrawCall(cylinderDrawCall);
            compositeMeshDrawCall.AddDrawCall(arrowHeadDrawCall);
            AddMeshDrawCall(compositeMeshDrawCall);
            
            return compositeMeshDrawCall;
        }
        
        public static BaseMeshDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;

            MeshDrawCall drawCall = new MeshDrawCall(Config.CylinderMesh, from + (dir * (d / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(lineWidth, d / 2.0f, lineWidth));
            AddMeshDrawCall(drawCall);

            return drawCall;
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

        public static BaseMeshDrawCall DrawCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.CubeMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public static BaseMeshDrawCall DrawQuad(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(Config.QuadMesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }
        
        public static BaseMeshDrawCall DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            MeshDrawCall drawCall = new MeshDrawCall(mesh, position, rotation, scale);
            AddMeshDrawCall(drawCall);

            return drawCall;
        }

        public static BaseMeshDrawCall RenderCircle(Vector3 center, int sides, float radius)
        {
            return RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        }

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
    }
}

