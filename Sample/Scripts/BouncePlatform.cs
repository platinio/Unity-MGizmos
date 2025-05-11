using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneOnyx
{
    public class BouncePlatform : MonoBehaviour
    {
        [SerializeField] private float bounceMultiplier;
        
        private void OnCollisionEnter(Collision other)
        {
            var otherRb = other.body.GetComponent<Rigidbody>();
            if (otherRb == null) return;
            
            otherRb.AddForce(other.impulse * bounceMultiplier);
        }
    }
}

