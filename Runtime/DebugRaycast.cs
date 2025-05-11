using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx
{
    public class DebugRaycast : MonoBehaviour
    {
        [SerializeField] private Transform fromRaycast;
        [SerializeField] private Transform toRaycast;
        
        private void OnDrawGizmos()
        {
            if (fromRaycast == null || toRaycast == null) return;
            
            Ray ray = new Ray(fromRaycast.position, (toRaycast.position - fromRaycast.position).normalized);
            float d = Vector3.Distance(fromRaycast.position, toRaycast.position);
            PhysicsDebug.Raycast(ray, d);
        }
    }

}

