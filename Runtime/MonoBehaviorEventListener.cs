using System;
using UnityEngine;

namespace Platinio
{
    [ExecuteAlways]
    public class MonoBehaviorEventListener : MonoBehaviour
    {
        public event Action OnDestroyEvent;
        public event Action OnEnableEvent;
        public event Action OnDisabledEvent;
        public Action OnUpdateEvent;
        public Action OnPostRenderEvent;
        public Action OnRenderObjectEvent;

        private void OnEnable()
        {
            OnEnableEvent?.Invoke();
        }

        private void Update()
        {
            OnUpdateEvent?.Invoke();
        }

        private void OnDisable()
        {
            OnDisabledEvent?.Invoke();
        }

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }

        private void OnPostRender()
        {
            OnPostRenderEvent?.Invoke();
        }

        private void OnRenderObject()
        {
            OnRenderObjectEvent?.Invoke();
        }
    }
}