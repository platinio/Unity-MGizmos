using UnityEngine;

namespace ArcaneOnyx
{
    public static class MPhysics
    {
        private const float RaycastHitSize = 0.01f;
        private const float RaycastStemWidth = 0.005f;
        private const float RaycastArrowHeadSize = 0.075f;
        
        private const float NormalStemWidth = 0.005f;
        private const float NormalArrowHeadSize = 0.025f;
        private const float NormalLength = 0.5f;

        private const float ContantPointSize = 0.065f;

        private const float CollisionImpulseMultiplier = 2.5f;
        
        private const float VelocityStemWidth = 0.03f;
        private const float VelocityArrowHeadSize = 0.1f;

        private const float FakeInfinity = 100000.0f;

        private static Color RaycastColor = new(216.0f / 255, 235.0f / 255, 52.0f / 255, 177.0f / 255);
        private static Color HitColor = new(1, 52.0f / 255, 0, 177.0f / 255);
        private static Color NormalColor = new(52.0f / 255, 235.0f / 255, 219.0f / 255);
        private static Color ContactPointColor = new(1, 52.0f / 255, 0, 177.0f / 255);
        private static Color VelocityColor = new(9.0f / 255, 180.0f / 255, 0, 177.0f / 255);

        public static float Duration = 0;
        
        #region Materials
        private static Material particlesStandardUnlit;
        private static Material guiText;
       
        public static Material ParticlesStandardUnlitMaterial
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
        
        public static Material GUITextMaterial
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
        #endregion
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
#if UNITY_EDITOR
            int size = Physics.RaycastNonAlloc(ray, results);
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
           
            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance);
#if UNITY_EDITOR
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask);
#if UNITY_EDITOR
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
#if UNITY_EDITOR
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
      
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction);
#if UNITY_EDITOR
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance);
#if UNITY_EDITOR
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
#if UNITY_EDITOR
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
#if UNITY_EDITOR
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
#endif
            return raycastHits;
        }
        
        public static bool Raycast(Ray ray)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(ray);
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
               dc = RenderRaycastHit(hit).SetDuration(Duration);
               MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo));
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance);
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction)
        {
#if UNITY_EDITOR
            var raycastHits = Physics.RaycastAll(origin, direction);
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
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
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
#endif
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
#if UNITY_EDITOR
            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
#else
            return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction);
#endif
        }

        public static MGizmoBaseDrawCall RenderRigidBody(Rigidbody rb, float velocityScaler)
        {
            var mat = GUITextMaterial;
            var dc = MGizmos.RenderArrow(rb.position, rb.position + (rb.linearVelocity.normalized * rb.linearVelocity.magnitude * velocityScaler), VelocityStemWidth, VelocityArrowHeadSize);
            dc.SetMaterial(mat);
            dc.SetColor(VelocityColor);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderCollision(Collision c)
        {
            return RenderContactPoints(c.contacts);
        }

        public static MGizmoCompositeDrawCall RenderContactPoints(ContactPoint[] contactPoints)
        {
            MGizmoCompositeDrawCall compositeDrawCall = new();
            
            foreach (var contactPoint in contactPoints)
            {
                var dc = RenderContactPoint(contactPoint);
                compositeDrawCall.AddDrawCall(dc);
            }

            return compositeDrawCall;
        }

        public static MGizmoBaseDrawCall RenderContactPoint(ContactPoint p)
        {
            MGizmoCompositeDrawCall compositeDrawCall = new();
            
            var contactPointMaterial = MGizmos.Config.DefaultMaterial;
            var dc = MGizmos.RenderSphere(p.point, ContantPointSize);
            dc.SetMaterial(contactPointMaterial);
            dc.SetColor(ContactPointColor);
            compositeDrawCall.AddDrawCall(dc);
            
            var normalMaterial = ParticlesStandardUnlitMaterial;
            Vector3 impulseEnd = p.point + (p.impulse.normalized * (p.impulse.magnitude * Time.fixedDeltaTime) * CollisionImpulseMultiplier);
            dc = MGizmos.RenderArrow(p.point, impulseEnd, NormalStemWidth, NormalArrowHeadSize);
            dc.SetMaterial(normalMaterial);
            dc.SetColor(NormalColor);
            compositeDrawCall.AddDrawCall(dc);

            return compositeDrawCall;
        }

        public static MGizmoBaseDrawCall RenderRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return RenderRay(new Ray(origin, direction), maxDistance);
        }
        
        public static MGizmoBaseDrawCall RenderRay(Vector3 origin, Vector3 direction)
        {
            return RenderRay(new Ray(origin, direction));
        }

        public static MGizmoBaseDrawCall RenderRay(Ray ray, float maxDistance = FakeInfinity)
        {
            Vector3 rayEndPosition = ray.origin + (ray.direction * maxDistance);
            
            var dc = MGizmos.RenderArrow(ray.origin, rayEndPosition, RaycastStemWidth, RaycastArrowHeadSize);
            dc.SetMaterial(ParticlesStandardUnlitMaterial);
            dc.SetColor(RaycastColor);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderRaycastHit(RaycastHit hit)
        {
            MGizmoCompositeDrawCall compositeDrawCall = new();
            
            var hitMaterial = ParticlesStandardUnlitMaterial;
            var normalMaterial = ParticlesStandardUnlitMaterial;
            Vector3 normalEnd = hit.point + (hit.normal.normalized * NormalLength);
            
            var dc = MGizmos.RenderSphere(hit.point, RaycastHitSize);
            dc.SetMaterial(hitMaterial);
            dc.SetColor(HitColor);
            compositeDrawCall.AddDrawCall(dc);
            
            dc = MGizmos.RenderArrow(hit.point, normalEnd, NormalStemWidth, NormalArrowHeadSize);
            dc.SetMaterial(normalMaterial);
            dc.SetColor(NormalColor);
            compositeDrawCall.AddDrawCall(dc);

            return compositeDrawCall;
        }
    }
}