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
    
    private void OnDrawGizmos()
    {
        DebugMeshRenderer.Instance.DrawSphere(transform.position, Quaternion.identity, range, material);
        //RenderTest.RenderEvent?.Invoke(SceneView.lastActiveSceneView.camera);
    }
}
