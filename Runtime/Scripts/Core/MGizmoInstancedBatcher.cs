using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneOnyx.MeshGizmos
{
    //Batches gizmo draws that share a mesh and an instancing-enabled material into
    //Graphics.DrawMeshInstanced calls, so hundreds of spheres/lines cost a handful of draw calls
    //instead of one each. Draw calls submit while MGizmos.HandleCameraDrawCalls iterates them and the
    //accumulated batches are flushed at the end of that same pass, targeting that pass's camera.
    //Per-instance colours travel through the _Color instanced property, so they only vary per gizmo on
    //shaders that declare it (like ArcaneOnyx/MGizmos/Instanced Unlit); any other instancing-enabled
    //material still batches but renders with its own material colour.
    public static class MGizmoInstancedBatcher
    {
        //hard limit of Graphics.DrawMeshInstanced
        private const int MaxInstancesPerBatch = 1023;

        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly bool SupportsInstancing = SystemInfo.supportsInstancing;

        private readonly struct BatchKey : IEquatable<BatchKey>
        {
            public readonly Mesh Mesh;
            public readonly Material Material;
            public readonly ShadowCastingMode ShadowCastingMode;
            public readonly bool ReceiveShadows;

            public BatchKey(Mesh mesh, Material material, ShadowCastingMode shadowCastingMode, bool receiveShadows)
            {
                Mesh = mesh;
                Material = material;
                ShadowCastingMode = shadowCastingMode;
                ReceiveShadows = receiveShadows;
            }

            public bool Equals(BatchKey other)
            {
                return ReferenceEquals(Mesh, other.Mesh) &&
                       ReferenceEquals(Material, other.Material) &&
                       ShadowCastingMode == other.ShadowCastingMode &&
                       ReceiveShadows == other.ReceiveShadows;
            }

            public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Mesh != null ? Mesh.GetInstanceID() : 0;
                    hash = (hash * 397) ^ (Material != null ? Material.GetInstanceID() : 0);
                    hash = (hash * 397) ^ (int) ShadowCastingMode;
                    hash = (hash * 397) ^ (ReceiveShadows ? 1 : 0);
                    return hash;
                }
            }
        }

        private sealed class Batch
        {
            public readonly List<Matrix4x4> Matrices = new();
            public readonly List<Vector4> Colors = new();
        }

        private static readonly Dictionary<BatchKey, Batch> batches = new();
        private static readonly Stack<Batch> batchPool = new();
        private static readonly List<BatchKey> idleKeys = new();

        //chunk buffers reused by every instanced call; the colour array is always submitted at full
        //length because a MaterialPropertyBlock locks an array property to the size it was first set with
        private static readonly Matrix4x4[] matrixChunk = new Matrix4x4[MaxInstancesPerBatch];
        private static readonly Vector4[] colorChunk = new Vector4[MaxInstancesPerBatch];
        private static readonly MaterialPropertyBlock propertyBlock = new();

        public static bool CanBatch(Material material)
        {
            return SupportsInstancing && material != null && material.enableInstancing;
        }

        public static void Submit(Mesh mesh, Material material, in Matrix4x4 matrix, Color color, ShadowCastingMode shadowCastingMode, bool receiveShadows)
        {
            if (mesh == null || material == null) return;

            var key = new BatchKey(mesh, material, shadowCastingMode, receiveShadows);

            if (!batches.TryGetValue(key, out var batch))
            {
                batch = batchPool.Count > 0 ? batchPool.Pop() : new Batch();
                batches.Add(key, batch);
            }

            batch.Matrices.Add(matrix);
            batch.Colors.Add(color);
        }

        public static void Flush(Camera camera)
        {
            if (batches.Count == 0) return;

            idleKeys.Clear();

            foreach (var pair in batches)
            {
                var batch = pair.Value;

                //nothing submitted for this mesh/material since the last flush - recycle the entry so
                //gizmos that stop being drawn don't leave dead mesh/material references behind
                if (batch.Matrices.Count == 0)
                {
                    idleKeys.Add(pair.Key);
                    continue;
                }

                Draw(camera, pair.Key, batch);

                batch.Matrices.Clear();
                batch.Colors.Clear();
            }

            for (int i = 0; i < idleKeys.Count; i++)
            {
                batchPool.Push(batches[idleKeys[i]]);
                batches.Remove(idleKeys[i]);
            }
        }

        public static void Clear()
        {
            foreach (var pair in batches)
            {
                pair.Value.Matrices.Clear();
                pair.Value.Colors.Clear();
                batchPool.Push(pair.Value);
            }

            batches.Clear();
        }

        private static void Draw(Camera camera, in BatchKey key, Batch batch)
        {
            //the mesh or material may have been destroyed between submit and flush
            if (key.Mesh == null || key.Material == null) return;

            int total = batch.Matrices.Count;

            for (int start = 0; start < total; start += MaxInstancesPerBatch)
            {
                int count = Mathf.Min(MaxInstancesPerBatch, total - start);

                batch.Matrices.CopyTo(start, matrixChunk, 0, count);
                batch.Colors.CopyTo(start, colorChunk, 0, count);

                propertyBlock.SetVectorArray(ColorPropertyId, colorChunk);

                Graphics.DrawMeshInstanced(key.Mesh, 0, key.Material, matrixChunk, count, propertyBlock,
                    key.ShadowCastingMode, key.ReceiveShadows, 0, camera);
            }
        }
    }
}
