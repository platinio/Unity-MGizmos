using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    /// <summary>
    /// A retained set of gizmo draw calls: everything added is drawn on every gizmo camera, every frame,
    /// until the group is cleared, rebuilt or disposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The timed <see cref="MGizmos.AddMeshDrawCall"/> path answers "show this for n seconds" - right for
    /// events, wrong for state. A tool that owns a picture of something (a path, a sensor range, a scored
    /// field of candidates) would have to re-issue every call each frame and tune durations against frame
    /// cadence. A group states the intent directly: this is the current picture, keep showing it; here is
    /// the new picture, show that instead.
    /// </para>
    /// <para>
    /// Typical use: <see cref="Clear"/>, then <c>Add(MGizmos.RenderSphere(...))</c> per element, whenever
    /// the state being visualized changes. Between rebuilds the group costs only its draws.
    /// </para>
    /// <para>
    /// <b>Ownership:</b> <see cref="Add"/> transfers the draw call to the group. Configure it (colour,
    /// material, shadows) before or after adding, but do not touch it after the group is cleared - cleared
    /// calls are recycled into the draw-call pools and may already be someone else's gizmo.
    /// </para>
    /// <para>
    /// Durations are ignored: retained calls do not age, so a group never needs <c>SetDuration</c> and a
    /// call added with one keeps drawing anyway. Scene loads and play-mode transitions clear every group's
    /// contents (the positions they describe belong to the world that is going away) but keep the group
    /// registered, so an owner that survives - an <c>[ExecuteAlways]</c> tool, a persistent manager -
    /// simply rebuilds into the same group. Dispose the group when its owner goes away for good; an
    /// undisposed group keeps itself registered and drawing forever, which is a leak by design rather than
    /// a safety net.
    /// </para>
    /// </remarks>
    public sealed class MGizmoGroup : System.IDisposable
    {
        private readonly List<MGizmoBaseDrawCall> drawCalls = new();
        private bool disposed;

        /// <summary>Whether the group draws at all. Cheaper than clearing when the picture may come back.</summary>
        public bool Visible { get; set; } = true;

        public int Count => drawCalls.Count;

        public MGizmoGroup()
        {
            MGizmos.RegisterGroup(this);
        }

        /// <summary>Adds a draw call to the picture, taking ownership of it. See the ownership note above.</summary>
        public void Add(MGizmoBaseDrawCall drawCall)
        {
            if (disposed || drawCall == null) return;

            //the shared no-op every Render* returns when gizmos are disabled - keeping it out here means
            //Draw never has to consider it, and callers never have to check what Render* handed back
            if (ReferenceEquals(drawCall, MGizmos.InertDrawCall)) return;

            drawCalls.Add(drawCall);
        }

        /// <summary>Discards the picture. The released calls go back to the pools.</summary>
        public void Clear()
        {
            for (int i = 0; i < drawCalls.Count; i++)
            {
                drawCalls[i].Release();
            }

            drawCalls.Clear();
        }

        /// <summary>Clears and unregisters. The group draws nothing ever again.</summary>
        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            Clear();
            MGizmos.UnregisterGroup(this);
        }

        //Called by MGizmos once per camera pass. Zero delta on purpose: retained calls do not age, and
        //passing the real delta would drain the durations of calls that happened to be created with one.
        internal void Draw(Camera camera)
        {
            if (!Visible || disposed) return;

            for (int i = 0; i < drawCalls.Count; i++)
            {
                drawCalls[i].Draw(camera, 0f);
            }
        }
    }
}
