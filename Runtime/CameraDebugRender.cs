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
            MGizmos.HandleCameraDrawCalls(renderCamera, Time.deltaTime);
        }
    }
}
