using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    public static class TransformExtension
    {
        public static bool IsFacingPointXZ(this Transform transform, Vector3 target, float rotationOffset, float acceptableRotationValue, bool drawGizmo = false)
        {
            target.y = transform.position.y;
            Vector3 dir = (target - transform.position).normalized;
            Vector2 dir2D = new Vector2(dir.x, dir.z).normalized;
            Vector2 thisDir = new Vector2(transform.forward.x, transform.forward.z).normalized;
            thisDir = Quaternion.Euler(0, 0, rotationOffset) * thisDir;
            thisDir = thisDir.normalized;

            if (drawGizmo)
            {
                DrawFacingPointXZGizmo(transform.position, thisDir, dir);
            }
            
            return Vector2.Dot(dir2D, thisDir) > acceptableRotationValue;
        }
        
        private static void DrawFacingPointXZGizmo(Vector3 p, Vector2 a, Vector3 b)
        {
            Vector3 offset = Vector3.up;
            Vector3 center = p + offset;
           
            Vector3 thisDir3d = new Vector3(a.x, a.y, 0).normalized;
            Vector3 dir3d = new Vector3(b.x, b.y, 0).normalized;
            
            var da = MGizmos.RenderArrow(center, center + (thisDir3d * 2.0f)).SetColor(Color.cyan);
            var db = MGizmos.RenderArrow(center, center + (dir3d * 2.0f)).SetColor(Color.red);
            
            MGizmos.AddMeshDrawCall(da);
            MGizmos.AddMeshDrawCall(db);
        }
        
        private static void DrawRotateTowardsXZGizmo(Transform transform, Quaternion desireRot)
        {
            Vector3 offset = Vector3.up;
            Vector3 center = transform.position + offset;
            var d = MGizmos.RenderArrow(center, center + ((desireRot * Vector3.forward).normalized * 2.0f)).SetColor(Color.green);
            MGizmos.AddMeshDrawCall(d);
        }
        
        public static void RotateTowardsXZ(this Transform transform, Vector3 target, float rotationOffset, float rotationSpeed, bool drawGizmo = false)
        {
            target.y = transform.position.y;
            Vector3 dir = (target - transform.position).normalized;
            Quaternion desireRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, rotationOffset, 0);

            if (drawGizmo)
            {
                DrawRotateTowardsXZGizmo(transform, desireRot);
            }
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desireRot, rotationSpeed * Time.deltaTime);
        }
    }
}