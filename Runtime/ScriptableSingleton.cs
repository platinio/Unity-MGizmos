using UnityEditor;
using UnityEngine;

namespace Platinio
{
    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        private static T instance;
        
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    MonoBehaviorEventListener monoBehaviorEventListener =
                        new GameObject("", typeof(MonoBehaviorEventListener)).GetComponent<MonoBehaviorEventListener>();
                    monoBehaviorEventListener.gameObject.hideFlags |= HideFlags.HideInHierarchy;

                    if (EditorApplication.isPlaying)
                    {
                        DontDestroyOnLoad(monoBehaviorEventListener.gameObject);
                    }

                    instance = Resources.Load<T>(typeof(T).Name);
                    instance.eventListener = monoBehaviorEventListener;
                    
                    instance.eventListener.OnEnableEvent += instance.OnEnableEvent;
                    instance.eventListener.OnDisabledEvent += instance.OnDisableEvent;
                    instance.eventListener.OnDestroyEvent += instance.OnDestroyEventInternal;
                    instance.eventListener.OnUpdateEvent += instance.OnUpdateEvent;
                    
                    instance.OnAwakeEvent();
                    instance.OnStartEvent();
                    instance.OnEnableEvent();
                }

                return instance;
            }
        }

        private MonoBehaviorEventListener eventListener;

        private void OnDestroyEventInternal()
        {
            OnDestroyEvent();

            eventListener.OnDestroyEvent -= OnDestroyEvent;
            eventListener.OnEnableEvent -= OnEnableEvent;
            eventListener.OnDisabledEvent -= OnDisableEvent;

            if (EditorApplication.isPlaying)
            {
                Destroy(eventListener.gameObject);
            }
        }

        protected virtual void OnAwakeEvent()
        {
            
        }

        protected virtual void OnStartEvent()
        {
        }

        protected virtual void OnEnableEvent()
        {
        }

        protected virtual void OnUpdateEvent()
        {
            
        }

        protected virtual void OnDisableEvent()
        {
        }


        protected virtual void OnDestroyEvent()
        {
        }
    }
}

