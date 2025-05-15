using System;
using Platinio;
using UnityEngine;

namespace ArcaneOnyx
{
    public class DebugRigidBody : MonoBehaviour
    {
        [SerializeField] private float velocityScaler;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            MPhysics.DebugRigidBody(rb, velocityScaler);
        }
    }
}