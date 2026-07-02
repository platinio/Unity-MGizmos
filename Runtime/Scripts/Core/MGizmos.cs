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

        //shared no-op draw call returned by every Render* early-out (disabled or missing config), so a
        //disabled MGizmos doesn't allocate a dummy per call; it has no mesh/material and never draws
        private static readonly MGizmoDrawCall inertDrawCall = new();

        internal static MGizmoDrawCall InertDrawCall => inertDrawCall;
        
#if UNITY_EDITOR
        private static float sceneGuiLastTime = 0;
#endif

        public static bool IsEnable
        {
            get
            {
                #if UNITY_EDITOR || SHOW_MESH_GIZMOS_IN_BUILD
                return true;
                #else
                return false;
                #endif
            }
        }

        static MGizmos()
        {
            SceneManager.sceneUnloaded -= SceneManagerOnsceneLoaded;
            SceneManager.sceneUnloaded += SceneManagerOnsceneLoaded;

#if UNITY_EDITOR
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            //listen this for scene view render
            SceneView.beforeSceneGui -= BeforeSceneGui;
            SceneView.beforeSceneGui += BeforeSceneGui;
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
                SceneView.beforeSceneGui -= BeforeSceneGui;
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
            if (meshDrawCalls != null)
            {
                foreach (var pair in meshDrawCalls)
                {
                    ReleaseDrawCalls(pair.Value);
                }

                meshDrawCalls.Clear();
            }

            MGizmoInstancedBatcher.Clear();
        }

        private static void ReleaseDrawCalls(List<MGizmoBaseDrawCall> drawCalls)
        {
            for (int i = 0; i < drawCalls.Count; i++)
            {
                drawCalls[i].Release();
            }
        }

        public static void AddMeshDrawCall(MGizmoBaseDrawCall drawCall)
        {
            if (!IsEnable) return;
            if (ReferenceEquals(drawCall, inertDrawCall)) return;

            foreach (var pair in meshDrawCalls)
            {
                pair.Value.Add(drawCall.Clone());
            }
        }
        
#if UNITY_EDITOR
        private static void BeforeSceneGui(SceneView sceneView)
        {
            if (sceneGuiLastTime == 0)
            {
                sceneGuiLastTime = GetTimeSinceStartup();
            }
            
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            switch (Event.current.GetTypeForControl(controlID))
            {
                case EventType.Repaint:
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
#else
            return Time.time;
#endif
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
                    dc.Release();
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
                    drawCalls[i].Release();
                    drawCalls.RemoveAt(i);
                }
            }

            //draw calls whose material supports instancing submitted themselves to the batcher instead
            //of issuing an individual DrawMesh - render them now as instanced batches for this camera
            MGizmoInstancedBatcher.Flush(camera);
        }

        public static void RemoveRenderCamera(Camera camera)
        {
            if (camera == null) return;

            if (meshDrawCalls.TryGetValue(camera, out var drawCalls))
            {
                ReleaseDrawCalls(drawCalls);
                meshDrawCalls.Remove(camera);
            }
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
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.SphereMesh, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position) => RenderCylinder(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Quaternion rotation) => RenderCylinder(position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Vector3 scale) => RenderCylinder(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderCylinder(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.CylinderMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to) => RenderLine(from, to, 0.01f);

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;
            
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
            if (!IsEnable) return inertDrawCall;
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.CubeMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderQuad(Vector3 position) => RenderQuad(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Quaternion rotation) => RenderQuad(position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Vector3 scale) => RenderQuad(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderQuad(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;
            
            MGizmoDrawCall dc = new MGizmoDrawCall(Config.QuadMesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius) => RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, Vector3 upwards) => RenderCircle(center, sides, radius, 0.01f, upwards);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth) => RenderCircle(center, sides, radius, lineWidth, Vector3.up);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth, Vector3 upwards)
        {
            if (!IsEnable) return inertDrawCall;
            
            var compositeMeshDrawCall = new MGizmoCompositeDrawCall();

            Vector3 right = Quaternion.Euler(0, 0, 90) * upwards;
            Vector3 forward = Vector3.Cross(upwards, right);

            Vector3 PointOnCircle(int side)
            {
                float currentRadian = (float) side / sides * 2 * Mathf.PI;
                return center + right * (Mathf.Cos(currentRadian) * radius) + forward * (Mathf.Sin(currentRadian) * radius);
            }

            Vector3 first = PointOnCircle(0);
            Vector3 previous = first;

            for (int i = 1; i < sides; i++)
            {
                Vector3 current = PointOnCircle(i);
                Vector3 dir = (current - previous).normalized;
                compositeMeshDrawCall.AddDrawCall(RenderLine(previous, current + (dir * 0.01f), lineWidth));
                previous = current;
            }

            compositeMeshDrawCall.AddDrawCall(RenderLine(previous, first, lineWidth));

            InitializeMeshDrawCall(compositeMeshDrawCall);

            return compositeMeshDrawCall;
        }

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position) => RenderMesh(mesh, position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation) => RenderMesh(mesh, position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Vector3 scale) => RenderMesh(mesh, position, Quaternion.identity, scale);

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return inertDrawCall;
            
            MGizmoDrawCall dc = new MGizmoDrawCall(mesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderArrow(Vector3 from, Vector3 to, float stemWidth = 0.025f, float arrowHeadSize = 0.1f)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;
            
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

        public static MGizmoBaseDrawCall RenderText(Vector3 position, string text) => RenderText(position, text, 0.25f);

        //Draws a text label. size is roughly the world height of a capital letter. When billboard is true
        //(the default) the label faces the rendering camera. Configure the font on MGizmosRendererConfig.
        public static MGizmoBaseDrawCall RenderText(Vector3 position, string text, float size, bool billboard = true)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;

            var font = GetTextFont();
            if (font == null) return inertDrawCall;

            var mesh = MGizmoTextMesh.Build(text, font, size);
            if (mesh == null) return inertDrawCall;

            var dc = new MGizmoTextDrawCall(mesh, position, Quaternion.identity, Vector3.one, billboard);
            InitializeMeshDrawCall(dc);

            //override the default material with the font atlas material so the glyphs actually render
            dc.SetMaterial(font.material);
            return dc;
        }

        private static Font fallbackTextFont;

        private static Font GetTextFont()
        {
            if (Config != null && Config.TextFont != null) return Config.TextFont;
            if (fallbackTextFont == null) fallbackTextFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return fallbackTextFont;
        }
        #endregion
    }
}

