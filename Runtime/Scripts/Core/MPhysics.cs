using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
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
                    particlesStandardUnlit = GetMaterial("Particles/Standard Unlit");
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
                    guiText = GetMaterial("GUI/Text Shader");
                }

                return guiText;
            }
        }

        private static Material GetMaterial(string shaderPath)
        {
            if (MGizmos.Config == null) return null;
            
            var shader = Shader.Find(shaderPath);
            if (shader == null)
            {
                Debug.LogError($"Could find shader {shaderPath} using default material instead");
                return MGizmos.Config.DefaultMaterial;
            }

            return new Material(shader);
        }

        #endregion
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(ray, results);
            }

            int size = Physics.RaycastNonAlloc(ray, results);
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
           
            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(ray, results, maxDistance);
            }

            int size = Physics.RaycastNonAlloc(ray, results, maxDistance);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask);
            }

            int size = Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);
            }

            int size = Physics.RaycastNonAlloc(ray, results, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(origin, direction, results);
            }

            int size = Physics.RaycastNonAlloc(origin, direction, results);
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(origin, direction, results, maxDistance);
            }

            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask);
            }

            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }

            int size = Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            for (int i = 0; i < size; i++)
            {
                dc = RenderRaycastHit(results[i]).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return size;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray)
        {
            var raycastHits = Physics.RaycastAll(ray);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
      
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
            if (!MGizmos.IsEnable) return raycastHits;

            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }

            return raycastHits;
        }
        
        public static bool Raycast(Ray ray)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray);
            }

            var raycastHits = Physics.RaycastAll(ray);
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Ray ray, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, maxDistance);
            }

            var raycastHits = Physics.RaycastAll(ray, maxDistance);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
               dc = RenderRaycastHit(hit).SetDuration(Duration);
               MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, maxDistance, layerMask);
            }

            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, maxDistance, layerMask, queryTriggerInteraction);
            }

            var raycastHits = Physics.RaycastAll(ray, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, out hitInfo);
            }

            var result = Physics.Raycast(ray, out hitInfo);
            var dc = RenderRay(ray).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, out hitInfo, maxDistance);
            }

            var result = Physics.Raycast(ray, out hitInfo, maxDistance);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
            }

            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
            }

            var result = Physics.Raycast(ray, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(ray, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(hitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction);
            }

            var raycastHits = Physics.RaycastAll(origin, direction);
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, maxDistance);
            }

            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, maxDistance, layerMask);
            }

            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
            }

            var raycastHits = Physics.RaycastAll(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);

            foreach (var hit in raycastHits)
            {
                dc = RenderRaycastHit(hit).SetDuration(Duration);
                MGizmos.AddMeshDrawCall(dc);
            }
            
            return raycastHits != null && raycastHits.Length > 0;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, out raycastHitInfo);
            }

            var result = Physics.Raycast(origin, direction, out raycastHitInfo);
            var dc = RenderRay(origin, direction).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
            }

            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
            }

            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }
        
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit raycastHitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (!MGizmos.IsEnable)
            {
                return Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction); 
            }

            var result = Physics.Raycast(origin, direction, out raycastHitInfo, maxDistance, layerMask, queryTriggerInteraction);
            var dc = RenderRay(origin, direction, maxDistance).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            dc = RenderRaycastHit(raycastHitInfo).SetDuration(Duration);
            MGizmos.AddMeshDrawCall(dc);
            
            return result;
        }

        public static MGizmoBaseDrawCall RenderRigidBody(Rigidbody rb, float velocityScaler)
        {
            if (!MGizmos.IsEnable)
            {
                return new MGizmoDrawCall();
            }

            Vector3 rbVelocity = Vector3.zero;
            
            #if UNITY_6000_0_OR_NEWER
            rbVelocity = rb.linearVelocity;
            #else
            rbVelocity = rb.velocity;
            #endif
            
            var mat = GUITextMaterial;
            var dc = MGizmos.RenderArrow(rb.position, rb.position + (rbVelocity * velocityScaler), VelocityStemWidth, VelocityArrowHeadSize);
            dc.SetMaterial(mat);
            dc.SetColor(VelocityColor);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderCollision(Collision c)
        {
            if (!MGizmos.IsEnable) return new MGizmoDrawCall();
            return RenderContactPoints(c.contacts);
        }

        public static MGizmoBaseDrawCall RenderContactPoints(ContactPoint[] contactPoints)
        {
            if (!MGizmos.IsEnable) return new MGizmoDrawCall();
            
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
            if (!MGizmos.IsEnable) return new MGizmoDrawCall();
            if (MGizmos.Config == null) return new MGizmoDrawCall();
            
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
            if (!MGizmos.IsEnable) return new MGizmoDrawCall();
            
            Vector3 rayEndPosition = ray.origin + (ray.direction * maxDistance);
            
            var dc = MGizmos.RenderArrow(ray.origin, rayEndPosition, RaycastStemWidth, RaycastArrowHeadSize);
            dc.SetMaterial(ParticlesStandardUnlitMaterial);
            dc.SetColor(RaycastColor);

            return dc;
        }

        public static MGizmoBaseDrawCall RenderRaycastHit(RaycastHit hit)
        {
            if (!MGizmos.IsEnable) return new MGizmoDrawCall();
            
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