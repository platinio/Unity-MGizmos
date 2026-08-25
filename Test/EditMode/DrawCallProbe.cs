using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneOnyx.MeshGizmos.Tests
{
    /// <summary>
    /// Reads the geometry a draw call was built from.
    /// </summary>
    /// <remarks>
    /// Position, rotation, scale and mesh are implementation details of a draw call, and a public accessor
    /// added only so a test can see them would be API the next caller starts depending on. Reflection keeps
    /// that cost inside the test assembly, and keeping it in one place stops each test file from growing
    /// its own copy.
    /// </remarks>
    internal static class DrawCallProbe
    {
        internal static T Read<T>(object target, string fieldName)
        {
            var type = target.GetType();
            FieldInfo field = null;

            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }

            Assert.IsNotNull(field, $"field '{fieldName}' not found on {target.GetType().Name}");
            return (T)field.GetValue(target);
        }

        internal static Mesh MeshOf(object drawCall) => Read<Mesh>(drawCall, "mesh");

        /// <summary>
        /// The local-to-world transform the draw call will render with, for asserting where mesh-local
        /// points actually land in the world.
        /// </summary>
        internal static Matrix4x4 MatrixOf(object drawCall) => Matrix4x4.TRS(
            Read<Vector3>(drawCall, "position"),
            Read<Quaternion>(drawCall, "rotation"),
            Read<Vector3>(drawCall, "scale"));
    }
}
