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

        //retained pictures - drawn on every camera pass until their owners clear them. Registration is
        //managed by MGizmoGroup's constructor and Dispose; see that class for the ownership rules.
        private static readonly List<MGizmoGroup> groups = new();

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

            //keeps game-view gizmos alive while the editor is paused
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
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
                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.pauseStateChanged -= OnPauseStateChanged;
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

        //While the editor is paused, MGizmosCamera.Update stops running and Graphics.Draw* submissions
        //only last one frame, so any Game view repaint during the pause (editor interactions trigger
        //them) would render without gizmos. Re-submit the game cameras' draw calls with a zero delta -
        //EditorApplication.update keeps ticking through a pause - so the gizmos stay visible and frozen.
        //The scene view camera is excluded: BeforeSceneGui already feeds it while paused.
        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPaused || !Application.isPlaying) return;

            SubmitGameCameraDrawCalls();
        }

        //The repaint triggered by engaging pause can run in the same editor frame, before the first
        //paused OnEditorUpdate tick - that repaint had no submissions and the gizmos blinked out for a
        //moment (most visible with the game view maximized, where the pause interaction repaints it
        //immediately). Submitting synchronously on the state change puts the draws in flight before that
        //first repaint renders.
        private static void OnPauseStateChanged(PauseState state)
        {
            if (state != PauseState.Paused || !Application.isPlaying) return;

            SubmitGameCameraDrawCalls();
        }

        private static void SubmitGameCameraDrawCalls()
        {
            foreach (var pair in meshDrawCalls)
            {
                var camera = pair.Key;
                if (camera == null || camera.cameraType != CameraType.Game) continue;

                HandleCameraDrawCalls(camera, 0.0f);
            }
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

            //contents only, never the registration: the positions a group describes belong to the scene
            //or play session that is going away, but the group's owner may survive the transition (an
            //[ExecuteAlways] tool does) and expects to rebuild into the same group
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Clear();
            }

            MGizmoInstancedBatcher.Clear();
        }

        internal static void RegisterGroup(MGizmoGroup group) => groups.Add(group);

        internal static void UnregisterGroup(MGizmoGroup group) => groups.Remove(group);

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
                    //while the editor is paused, gizmo lifetimes must not advance: the scene view keeps
                    //repainting on editor time (which never pauses), so without this the durations kept
                    //draining through a pause. Zero delta still draws everything, frozen, so the scene
                    //can be inspected; game cameras already freeze because their Update stops running.
                    float deltaTime = EditorApplication.isPaused && Application.isPlaying
                        ? 0.0f
                        : GetTimeSinceStartup() - sceneGuiLastTime;

                    HandleCameraDrawCalls(sceneView.camera, deltaTime);
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

            //if the camera doesnt exist add it. Retained groups still draw on this very first pass -
            //they are not per-camera state, and a scene view opened mid-session should show the current
            //picture immediately rather than after its second repaint
            if (!meshDrawCalls.TryGetValue(camera, out var drawCalls))
            {
                meshDrawCalls.Add(camera, new List<MGizmoBaseDrawCall>());
                DrawGroups(camera);
                MGizmoInstancedBatcher.Flush(camera);
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

            DrawGroups(camera);

            //draw calls whose material supports instancing submitted themselves to the batcher instead
            //of issuing an individual DrawMesh - render them now as instanced batches for this camera
            MGizmoInstancedBatcher.Flush(camera);
        }

        private static void DrawGroups(Camera camera)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].Draw(camera);
            }
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
            
            //the mesh is 1 unit across, so the caller's radius doubles into a diameter
            MGizmoDrawCall dc = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Sphere, position, Quaternion.identity, Vector3.one * (radius * 2.0f));
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
            
            MGizmoDrawCall dc = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Cylinder, position, rotation, scale);
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to) => RenderLine(from, to, 0.01f);

        public static MGizmoBaseDrawCall RenderLine(Vector3 from, Vector3 to, float lineWidth)
        {
            if (!IsEnable) return inertDrawCall;
            //same guard as every other primitive: without the config there is no material to draw with, and
            //a debug call must degrade to nothing rather than throw out of whoever asked for a gizmo
            if (Config == null) return inertDrawCall;

            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;

            //the cylinder mesh is 2 units tall, so half the distance is the right Y scale
            MGizmoDrawCall dc = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Cylinder, from + (dir * (d / 2.0f)), Quaternion.FromToRotation(Vector3.up, dir), new Vector3(lineWidth, d / 2.0f, lineWidth));
            InitializeMeshDrawCall(dc);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderCube(Vector3 position) => RenderCube(position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Quaternion rotation) => RenderCube(position, rotation, Vector3.one);

        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Vector3 scale) => RenderCube(position, Quaternion.identity, scale);
        
        public static MGizmoBaseDrawCall RenderCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;

            MGizmoDrawCall dc = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Cube, position, rotation, scale);
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
            
            MGizmoDrawCall dc = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Quad, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius) => RenderCircle(center, sides, radius, 0.01f, Vector3.up);
        
        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, Vector3 upwards) => RenderCircle(center, sides, radius, 0.01f, upwards);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth) => RenderCircle(center, sides, radius, lineWidth, Vector3.up);

        public static MGizmoBaseDrawCall RenderCircle(Vector3 center, int sides, float radius, float lineWidth, Vector3 upwards)
        {
            if (!IsEnable) return inertDrawCall;
            
            var compositeMeshDrawCall = MGizmoCompositeDrawCall.Get();

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

        public static MGizmoBaseDrawCall RenderDisc(Vector3 center, float radius) => RenderDisc(center, radius, Vector3.up);

        //Draws a filled disc of the given radius, facing along upwards. Double-sided, so it stays visible
        //from below - a range or area marker laid on the ground must not vanish when the camera dips
        //under the floor plane.
        public static MGizmoBaseDrawCall RenderDisc(Vector3 center, float radius, Vector3 upwards)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;

            MGizmoDrawCall dc = MGizmoDrawCall.Get(
                MGizmoProceduralMeshes.Disc, center, Quaternion.FromToRotation(Vector3.up, upwards),
                new Vector3(radius, 1.0f, radius));
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderCross(Vector3 center, float size) => RenderCross(center, size, 0.01f, Vector3.up);

        public static MGizmoBaseDrawCall RenderCross(Vector3 center, float size, float lineWidth) => RenderCross(center, size, lineWidth, Vector3.up);

        //Draws an X of two crossed lines in the plane perpendicular to upwards, spanning size from the
        //centre to each tip. The universal "ruled out" marker - a rejected candidate, a failed probe, an
        //unreachable point - kept distinct from a sphere so exclusion never reads as just another sample.
        public static MGizmoBaseDrawCall RenderCross(Vector3 center, float size, float lineWidth, Vector3 upwards)
        {
            if (!IsEnable) return inertDrawCall;

            Vector3 right = Quaternion.Euler(0, 0, 90) * upwards;
            Vector3 forward = Vector3.Cross(upwards, right);

            Vector3 a = (right + forward).normalized * size;
            Vector3 b = (right - forward).normalized * size;

            var compositeMeshDrawCall = MGizmoCompositeDrawCall.Get();
            compositeMeshDrawCall.AddDrawCall(RenderLine(center - a, center + a, lineWidth));
            compositeMeshDrawCall.AddDrawCall(RenderLine(center - b, center + b, lineWidth));

            InitializeMeshDrawCall(compositeMeshDrawCall);
            return compositeMeshDrawCall;
        }

        public static MGizmoBaseDrawCall RenderBar(Vector3 basePosition, float height, float width) => RenderBar(basePosition, Vector3.up, height, width);

        //Draws a square column of the given height standing on basePosition, growing along direction.
        //Base-anchored on purpose: the built-in cube is centre-anchored, and hand-offsetting a centre by
        //half a height is the arithmetic everyone visualizing a value field gets wrong once. A negative
        //height grows the bar the other way.
        public static MGizmoBaseDrawCall RenderBar(Vector3 basePosition, Vector3 direction, float height, float width)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;

            Vector3 dir = direction.normalized * Mathf.Sign(height);
            float length = Mathf.Abs(height);

            MGizmoDrawCall dc = MGizmoDrawCall.Get(
                MGizmoProceduralMeshes.Cube, basePosition + dir * (length * 0.5f),
                Quaternion.FromToRotation(Vector3.up, dir), new Vector3(width, length, width));
            InitializeMeshDrawCall(dc);
            return dc;
        }

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position) => RenderMesh(mesh, position, Quaternion.identity, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation) => RenderMesh(mesh, position, rotation, Vector3.one);
        
        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Vector3 scale) => RenderMesh(mesh, position, Quaternion.identity, scale);

        public static MGizmoBaseDrawCall RenderMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsEnable) return inertDrawCall;
            
            MGizmoDrawCall dc = MGizmoDrawCall.Get(mesh, position, rotation, scale);
            InitializeMeshDrawCall(dc);

            return dc;
        }
        
        public static MGizmoBaseDrawCall RenderArrow(Vector3 from, Vector3 to, float stemWidth = 0.025f, float arrowHeadSize = 0.1f)
        {
            if (!IsEnable) return inertDrawCall;
            if (Config == null) return inertDrawCall;
            
            var compositeMeshDrawCall = MGizmoCompositeDrawCall.Get();
            
            float d = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            float headLength = arrowHeadSize;

            Vector3 stemStartPosition = from + (dir * (d / 2.0f));
            Vector3 arrowHeadOffset = (dir * (headLength / 2.0f));
            Vector3 stemScale = new Vector3(stemWidth, (d / 2.0f) - (headLength / 2.0f), stemWidth);
            
            MGizmoDrawCall cylinderDrawCall = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Cylinder,  stemStartPosition - arrowHeadOffset, Quaternion.FromToRotation(Vector3.up, dir), stemScale);

            //the cone stands on its base pointing up +Y, so aligning up with dir is the whole rotation -
            //no axis correction, unlike the +Z-facing FBX this replaced
            Quaternion arrowHeadRotation = Quaternion.FromToRotation(Vector3.up, dir);
            Vector3 arrowHeadScale = Vector3.one * arrowHeadSize;

            MGizmoDrawCall arrowHeadDrawCall = MGizmoDrawCall.Get(MGizmoProceduralMeshes.Cone, to - (dir * headLength), arrowHeadRotation, arrowHeadScale);
            
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

            var dc = MGizmoTextDrawCall.Get(mesh, position, Quaternion.identity, Vector3.one, billboard);
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

