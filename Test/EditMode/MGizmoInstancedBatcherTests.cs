using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    public class MGizmoInstancedBatcherTests
    {
        private Material material;

        [SetUp]
        public void SetUp()
        {
            material = new Material(Shader.Find("Hidden/Internal-Colored"));
        }

        [TearDown]
        public void TearDown()
        {
            MGizmoInstancedBatcher.Clear();
            Object.DestroyImmediate(material);
        }

        [Test]
        public void FlushingAnIdleBatch_AfterItsMeshWasDestroyed_DoesNotThrow()
        {
            var mesh = new Mesh();
            MGizmoInstancedBatcher.Submit(mesh, material, Matrix4x4.identity, Color.white, ShadowCastingMode.Off, false);
            Object.DestroyImmediate(mesh);

            MGizmoInstancedBatcher.Flush(null);

            Assert.DoesNotThrow(() => MGizmoInstancedBatcher.Flush(null),
                "the second flush finds the batch idle and looks its key up again");
        }
    }
}
