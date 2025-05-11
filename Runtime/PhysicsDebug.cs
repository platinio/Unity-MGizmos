using System.Collections.Generic;
using Platinio;
using UnityEngine;

namespace ArcaneOnyx
{
    public static class PhysicsDebug
    {
        private const float RaycastHitSize = 0.065f;
        private const float RaycastStemWidth = 0.0125f;
        private const float RaycastArrowHeadSize = 0.08f;
        
        private const float NormalStemWidth = 0.015f;
        private const float NormalArrowHeadSize = 0.05f;
        private const float NormalLength = 0.9f;

        private const float ContantPointSize = 0.065f;

        private const float CollisionImpulseMultiplier = 2.5f;
        
        private const float VelocityStemWidth = 0.03f;
        private const float VelocityArrowHeadSize = 0.1f;

        private const float FakeInfinity = 100000.0f;

        private static Dictionary<string, Material> cacheMaterials = new();
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(ray, results);
            DrawRay(ray);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(ray, results);
#endif
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(ray, results, maxDistance);
            DrawRay(ray, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(ray, results, maxDistance);
#endif
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask);
            DrawRay(ray, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask);
#endif
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(ray, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(origin, direction, results);
            DrawRay(origin, direction);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(origin, direction, results);
#endif
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance);
            DrawRay(origin, direction, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(origin, direction, results, maxDistance);
#endif
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask);
            DrawRay(origin, direction, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask);
#endif
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(origin, direction, maxDistance);

            for (int i = 0; i < size; i++)
            {
                DrawRaycastHit(results[i]);
            }
            
            return size;
#else
            return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static RaycastHit[] RaycastAll(Ray ray)
        {
            var raycastHits = Physics.RaycastAll(ray);
#if UNITY_EDITOR
            DrawRay(ray);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance);
#if UNITY_EDITOR
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask);
#if UNITY_EDITOR
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
#if UNITY_EDITOR
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
      
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction);
#if UNITY_EDITOR
            DrawRay(origin, direction);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance);
#if UNITY_EDITOR
            DrawRay(origin, direction, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
#if UNITY_EDITOR
            DrawRay(origin, direction, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
#if UNITY_EDITOR
            DrawRay(origin, direction, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
#endif
            return raycastHits;
        }
        
        public static bool Raycast(Ray ray)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(ray);
            DrawRay(ray);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(ray);
#endif
        }
        
        public static bool Raycast(Ray ray, float maxDistance)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(ray, maxDistance);
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
               DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(ray, maxDistance);
#endif
        }
        
        public static bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask);
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(ray, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(ray, maxDistance);
            
            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(ray, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo);
            DrawRay(ray);
            DrawRaycastHit(hitInfo);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo));
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance);
            DrawRay(ray, maxDistance);
            DrawRaycastHit(hitInfo);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance);
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
            DrawRay(ray, maxDistance);
            DrawRaycastHit(hitInfo);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(ray, maxDistance);
            DrawRaycastHit(hitInfo);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(origin, direction);
            DrawRay(origin, direction);

            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(origin, direction);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance);
            DrawRay(origin, direction, maxDistance);

            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(origin, direction, maxDistance);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
            DrawRay(origin, direction, maxDistance);

            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(origin, direction, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(origin, direction, maxDistance);

            foreach (var hit in raycastHits)
            {
                DrawRaycastHit(hit);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
#else
            return Physics.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo);
            DrawRay(origin, direction);
            DrawRaycastHit(raycastHitInfo);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
            DrawRay(origin, direction, maxDistance);
            DrawRaycastHit(raycastHitInfo);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
            DrawRay(origin, direction, maxDistance);
            DrawRaycastHit(raycastHitInfo);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction);
            DrawRay(origin, direction, maxDistance);
            DrawRaycastHit(raycastHitInfo);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }

        public static void DebugRigidBody(Rigidbody rb, float velocityScaler)
        {
            var mat = GetMaterial("Velocity");
            DebugMeshRenderer.Instance.DrawArrow(rb.position, rb.position + (rb.velocity.normalized * rb.velocity.magnitude * velocityScaler), VelocityStemWidth, VelocityArrowHeadSize, mat);
        }

        public static void DrawCollision(Collision c, float duration)
        {
            DrawContactPoints(c.contacts, duration);
        }

        public static void DrawContactPoints(ContactPoint[] contactPoints, float duration)
        {
            foreach (var contactPoint in contactPoints)
            {
                DrawContactPoint(contactPoint, duration);
            }
        }

        public static void DrawContactPoint(ContactPoint p, float duration)
        {
            DebugMeshRenderer.Instance.DrawSphere(p.point, Quaternion.identity, ContantPointSize, GetMaterial("ContactPoint"), duration);
            
            var normalMaterial = GetMaterial("NormalMaterial");
            
            Vector3 impulseEnd = p.point + (p.impulse.normalized * (p.impulse.magnitude * Time.fixedDeltaTime) * CollisionImpulseMultiplier);
            DebugMeshRenderer.Instance.DrawArrow(p.point, impulseEnd, NormalStemWidth, NormalArrowHeadSize, normalMaterial, duration);
            
        }

        private static void DrawRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            DrawRay(new Ray(origin, direction), maxDistance);
        }
        
        private static void DrawRay(Vector3 origin, Vector3 direction)
        {
            DrawRay(new Ray(origin, direction));
        }

        private static void DrawRay(Ray ray, float maxDistance = FakeInfinity)
        {
            Vector3 rayEndPosition = ray.origin + (ray.direction * maxDistance);
            var raycastMaterial = GetMaterial("RaycastMaterial");
            
            DebugMeshRenderer.Instance.DrawArrow(ray.origin, rayEndPosition, RaycastStemWidth, RaycastArrowHeadSize, raycastMaterial);
        }

        private static void DrawRaycastHit(RaycastHit hit)
        {
            var hitMaterial = GetMaterial("HitMaterial");
            var normalMaterial = GetMaterial("NormalMaterial");
            Vector3 normalEnd = hit.point + (hit.normal.normalized * NormalLength);
            
            DebugMeshRenderer.Instance.DrawSphere(hit.point, Quaternion.identity, RaycastHitSize, hitMaterial);
            DebugMeshRenderer.Instance.DrawArrow(hit.point, normalEnd, NormalStemWidth, NormalArrowHeadSize, normalMaterial);
        }

        private static Material GetMaterial(string materialName)
        {
            if (cacheMaterials.TryGetValue(materialName, out var mat)) return mat;

            cacheMaterials[materialName] = Resources.Load<Material>(materialName);
            return cacheMaterials[materialName];
        }
    }
}