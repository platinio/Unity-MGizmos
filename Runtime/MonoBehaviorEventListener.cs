using System;
using UnityEngine;

namespace Platinio
{
    public class MonoBehaviorEventListener : MonoBehaviour
    {
        public event Action OnDestroyEvent;
        public event Action OnEnableEvent;
        public event Action OnDisabledEvent;
        public Action OnUpdateEvent;

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
    }
}