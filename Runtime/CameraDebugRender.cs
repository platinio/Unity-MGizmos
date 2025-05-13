using ArcaneOnyx;
using UnityEngine;

namespace Platinio
{
    public class CameraDebugRender : MonoBehaviour
    {
        private Camera renderCamera;

        private void Awake()
        {
            renderCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            DebugMeshRenderer.HandleCameraDrawCalls(renderCamera, Time.deltaTime);
        }
    }
}
