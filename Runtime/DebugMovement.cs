using UnityEngine;

namespace ArcaneOnyx
{
    public class DebugMovement : MonoBehaviour
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;
        [SerializeField] private Vector3 scale;
        [SerializeField] private float duration;
        [SerializeField] private float delta;

        private float timer;

        private void LateUpdate()
        {
            timer += Time.deltaTime;
            if (timer < delta) return;

            timer = 0;
            DebugMeshRenderer.DrawMesh(mesh, transform.position, transform.rotation, scale)
                .SetDuration(duration)
                .SetMaterial(material);

        }
    }
}