using UnityEngine;

namespace ArcaneOnyx.MeshGizmos
{
    public class MGizmosCamera : MonoBehaviour
    {
        private Camera renderCamera;

        private void Awake()
        {
            renderCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            MGizmos.HandleCameraDrawCalls(renderCamera, Time.deltaTime);
        }

        private void OnDisable()
        {
            MGizmos.RemoveRenderCamera(renderCamera);
        }
    }
}
