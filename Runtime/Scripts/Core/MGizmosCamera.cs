using ArcaneOnyx;
using UnityEngine;

namespace ArcaneOnyx
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
    }
}
