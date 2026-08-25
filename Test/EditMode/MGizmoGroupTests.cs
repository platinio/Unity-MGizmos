using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <summary>
    /// The retained-group contract and the pooled-original guarantees it depends on.
    /// </summary>
    /// <remarks>
    /// These run in the editor, where <see cref="MGizmos.IsEnable"/> is compile-time true and the
    /// renderer config ships in the module's Resources - so every <c>Render*</c> call here produces a
    /// real draw call, not the inert no-op.
    /// </remarks>
    public class MGizmoGroupTests
    {
        [Test]
        public void AddAndClear_TrackTheGroupContents()
        {
            using var group = new MGizmoGroup();

            group.Add(MGizmos.RenderSphere(Vector3.zero, 1f));
            group.Add(MGizmos.RenderSphere(Vector3.one, 1f));

            Assert.AreEqual(2, group.Count);

            group.Clear();

            Assert.AreEqual(0, group.Count);
        }

        [Test]
        public void AddAfterDispose_IsIgnored()
        {
            var group = new MGizmoGroup();
            group.Dispose();

            group.Add(MGizmos.RenderSphere(Vector3.zero, 1f));

            Assert.AreEqual(0, group.Count, "a disposed group must never accumulate work again");
        }

        [Test]
        public void AReleasedOriginal_ComesBackFromThePoolFullyReset()
        {
            // The pool bug class this pins: state from a draw call's previous life leaking into its next
            // one. Duration is the observable field - a stale one would make a new gizmo expire early or
            // linger. The identity assertion is deliberate: without it, a fresh instance also reports
            // duration 0 and the test would pass without testing the pool at all.
            using var group = new MGizmoGroup();

            var first = MGizmos.RenderSphere(Vector3.zero, 1f);
            first.SetDuration(5f);
            group.Add(first);
            group.Clear();   //releases into the pool

            var second = MGizmos.RenderSphere(Vector3.one, 2f);

            Assert.AreSame(first, second, "the released original should be the next one handed out");
            Assert.AreEqual(0f, second.RemainingTime, "and it must come back with its duration reset");
        }

        [Test]
        public void RenderBar_IsBaseAnchored()
        {
            // the one piece of arithmetic in RenderBar, and the one everyone doing it by hand gets wrong:
            // the cube mesh is centre-anchored, so the centre must sit half the height above the base
            var bar = MGizmos.RenderBar(new Vector3(2f, 1f, 3f), 4f, 0.5f);

            Vector3 position = DrawCallProbe.Read<Vector3>(bar, "position");
            Vector3 scale = DrawCallProbe.Read<Vector3>(bar, "scale");

            Assert.AreEqual(new Vector3(2f, 3f, 3f), position, "centre = base + up * height/2");
            Assert.AreEqual(4f, scale.y, 1e-4f);
            Assert.AreEqual(0.5f, scale.x, 1e-4f);
        }

        [Test]
        public void RenderBar_WithNegativeHeight_GrowsDownward()
        {
            var bar = MGizmos.RenderBar(new Vector3(0f, 10f, 0f), -4f, 0.5f);

            Vector3 position = DrawCallProbe.Read<Vector3>(bar, "position");
            Vector3 scale = DrawCallProbe.Read<Vector3>(bar, "scale");

            Assert.AreEqual(new Vector3(0f, 8f, 0f), position, "a negative height hangs the bar below its base");
            Assert.AreEqual(4f, scale.y, 1e-4f, "the mesh scale stays positive; direction carries the sign");
        }

        [Test]
        public void RenderCross_IsTwoCrossedLines()
        {
            var cross = MGizmos.RenderCross(Vector3.zero, 1f);

            Assert.IsInstanceOf<MGizmoCompositeDrawCall>(cross);

            var lines = DrawCallProbe.Read<System.Collections.Generic.List<MGizmoBaseDrawCall>>(cross, "drawCalls");
            Assert.AreEqual(2, lines.Count);
        }
    }
}
