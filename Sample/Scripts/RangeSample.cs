using System;
using System.Collections;
using System.Collections.Generic;
using Platinio;
using UnityEditor;
using UnityEngine;


public class RangeSample : MonoBehaviour
{
    [SerializeField] private float range;
    [SerializeField] private Material material;
    [SerializeField] private Vector3 scale;
    [SerializeField] private Vector3 rot;
    
    private void OnDrawGizmos()
    {
        //DebugMeshRenderer.Instance.DrawSphere(transform.position, Quaternion.identity, range, material);
        DebugMeshRenderer.Instance.DrawCylinder(transform.position, Quaternion.Euler(rot) , scale, material);
        //RenderTest.RenderEvent?.Invoke(SceneView.lastActiveSceneView.camera);
    }
}
