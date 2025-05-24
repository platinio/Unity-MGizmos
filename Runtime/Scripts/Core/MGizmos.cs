#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneOnyx.MeshGizmos
{
    public static class MGizmos
    {
        public static MGizmosRendererConfig Config => MGizmosRendererConfig.Instance;
        private static Dictionary<Camera, List<MGizmoBaseDrawCall>> meshDrawCalls = new();
        
        private static float sceneGuiLastTime = 0;

        public static bool IsEnable
        {
            get
            {
                #if UNITY_EDITOR || SHOW_MESH_GIZMOS_IN_BUILD
                return true;
                #endif

                return false;
            }
        }

        static MGizmos()
        {
            SceneManager.sceneUnloaded -= SceneManagerOnsceneLoaded;
            SceneManager.sceneUnloaded += SceneManagerOnsceneLoaded;

#if UNITY_EDITOR
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            //listen this for scene view render
            SceneView.duringSceneGui -= DuringSceneGui;
            SceneView.duringSceneGui += DuringSceneGui;
#endif
        }

        //https://stackoverflow.com/questions/256077/static-finalizer/256278#256278
        private static readonly Destructor Finalise = new Destructor();
        private sealed class Destructor
        {
            ~Destructor()
            {
                SceneManager.sceneUnloaded -= SceneManagerOnsceneLoaded;
                
#if UNITY_EDITOR
                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SceneView.duringSceneGui -= DuringSceneGui;
#endif
            }
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            Reset();
        }
        
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Reset();
        }
#endif
        
        private static void SceneManagerOnsceneLoaded(Scene current)
        {
            Reset();
        }

        private static void Reset()
        {
            meshDrawCalls?.Clear();
        }
        
        public static void AddMeshDrawCall(MGizmoBaseDrawCall drawCall)
        {
            if (!IsEnable) return;
            
            var cameras = meshDrawCalls.Keys;
            
            foreach (var camera in cameras)
            {
                meshDrawCalls[camera].Add(drawCall.Clone());
            }
        }
        
#if UNITY_EDITOR
        private static void DuringSceneGui(SceneView sceneView)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.Repaint:
                    if (sceneGuiLastTime == 0)
                    {
                        sceneGuiLastTime = GetTimeSinceStartup();
                    }

                    HandleCameraDrawCalls(sceneView.camera, GetTimeSinceStartup() - sceneGuiLastTime);
                    sceneGuiLastTime = GetTimeSinceStartup();
                    break;
            }
        }
#endif

        private static float GetTimeSinceStartup()
        {
#if UNITY_EDITOR
            return (float) EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }

        public static void HandleCameraDrawCalls(Camera camera, float deltaTime)
        {
            if (!IsEnable) return;

            //if the camera doesnt exist add it
            if (!meshDrawCalls.TryGetValue(camera, out var drawCalls))
            {
                meshDrawCalls.Add(camera, new List<MGizmoBaseDrawCall>());
                return;
            }
            
            for (int i = drawCalls.Count - 1; i >= 0; i--)
            {
                var dc = drawCalls[i];
                if (!dc.AddThisFrame && dc.KeepOneFrame)
                {
                    drawCalls.RemoveAt(i);
                    continue;
                }

                dc.AddThisFrame = false;
            }
            
            for (int i = drawCalls.Count - 1; i >= 0; i--)
            {
                drawCalls[i].Draw(camera, deltaTime);
                
                if (!drawCalls[i].KeepOneFrame && drawCalls[i].RemainingTime <= 0)
                {
                    drawCalls.RemoveAt(i);
                }
            }
        }

        public static void RemoveRenderCamera(Camera camera)
        {
            if (camera == null) return;
            meshDrawCalls.Remove(camera);
        }

        #region Render
        private static void InitializeMeshDrawCall(MGizmoBaseDrawCall drawCall)
        {
            if (Config == null) return;

            drawCall.KeepOneFrame = drawCall.RemainingTime <= 0;
            drawCall.AddThisFrame = true;
            drawCall.SetColor(Config.DefaultColor)
                .SetMaterial(Config.DefaultMaterial);
        }
        
        public static MGizmoBaseDrawCall RenderSphere(Vector3 position, float radius)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            if (Config == null) return new MGizmoDrawCall();
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.SphereMesh, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position) => RenderCylinder(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Quaternion rotation) => RenderCylinder(position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Vector3 scale) => RenderCylinder(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            if (Config == null) return new MGizmoDrawCall();
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.CylinderMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to) => RenderLine(from, to, 0.01f);

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;

            MGizmoDrawCall dc = new MGizmoDrawCall(Config.CylinderMesh, from + (dir * (d / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(lineWidth, d / 2.0f, lineWidth));
            InitializeMeshDrawCall(dc);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderCube(Vector3 position) => RenderCube(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Quaternion rotation) => RenderCube(position, rotation, Vector3.one);

        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Vector3 scale) => RenderCube(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.CubeMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderQuad(Vector3 position) => RenderQuad(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Quaternion rotation) => RenderQuad(position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Vector3 scale) => RenderQuad(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            if (Config == null) return new MGizmoDrawCall();
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.QuadMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius) => RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, Vector3 upwards) => RenderCircle(center, sides, radius, 0.01f, upwards);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth) => RenderCircle(center, sides, radius, lineWidth, Vector3.up);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth, Vector3 upwards)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            
            var compositeMeshDrawCall = new MGizmoCompositeDrawCall();
            
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
            
            InitializeMeshDrawCall(compositeMeshDrawCall);
            
            return compositeMeshDrawCall;
        }

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position) => RenderMesh(mesh, position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation) => RenderMesh(mesh, position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Vector3 scale) => RenderMesh(mesh, position, Quaternion.identity, scale);

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            
            MGizmoDrawCall dc = new MGizmoDrawCall(mesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderArrow(Vector3 from, Vector3 to, float stemWidth = 0.025f, float arrowHeadSize = 0.1f)
        {
            if (!IsEnable) return new MGizmoDrawCall();
            if (Config == null) return new MGizmoDrawCall();
            
            var compositeMeshDrawCall = new MGizmoCompositeDrawCall();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;

            Vector3 stemStartPosition = from + (dir * (d / 2.0f));
            Vector3 arrowHeadOffset = (dir * (headLength / 2.0f));
            Vector3 stemScale = new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth);
            
            MGizmoDrawCall cylinderDrawCall = new MGizmoDrawCall(Config.CylinderMesh,  stemStartPosition - arrowHeadOffset, Quaternion.FromToRotation(Vector3.up, dir), stemScale);

            Quaternion arrowHeadRotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(-90, 0, 0);
            Vector3 arrowHeadScale = Vector3.one * arrowHeadSize;
            
            MGizmoDrawCall arrowHeadDrawCall = new MGizmoDrawCall(Config.ArrowHead, to - (dir * headLength), arrowHeadRotation, arrowHeadScale);
            
            compositeMeshDrawCall.AddDrawCall(cylinderDrawCall);
            compositeMeshDrawCall.AddDrawCall(arrowHeadDrawCall);
           
            InitializeMeshDrawCall(compositeMeshDrawCall);
            return compositeMeshDrawCall;
        }
        #endregion
    }
}

