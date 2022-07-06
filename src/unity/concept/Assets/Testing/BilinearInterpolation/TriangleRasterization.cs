using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleRasterization : MonoBehaviour
{
    public ComputeShader computeShader;
    public float pointRadius = 0.05f;

    private Transform v0, v1, v2;
    private RenderTexture result;
    private int rasterizationKernel;
    private uint threadGroupsX, threadGroupsY;

    private void Start()
    {
        v0 = GameObject.Find("v0").transform;
        v1 = GameObject.Find("v1").transform;
        v2 = GameObject.Find("v2").transform;

        result = new RenderTexture(100, 100, 1);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = result;

        rasterizationKernel = computeShader.FindKernel("TriangleRasterization");
        computeShader.GetKernelThreadGroupSizes(rasterizationKernel, out threadGroupsX, out threadGroupsY, out _);
        computeShader.SetTexture(rasterizationKernel, "Result", result);
    }

    private void OnDrawGizmos()
    {
        if (result == null) Start();

        Gizmos.DrawSphere(v0.position, pointRadius);
        Gizmos.DrawSphere(v1.position, pointRadius);
        Gizmos.DrawSphere(v2.position, pointRadius);

        computeShader.SetVector("V0", v0.position);
        computeShader.SetVector("V1", v1.position);
        computeShader.SetVector("V2", v2.position);

        computeShader.Dispatch(rasterizationKernel, (int)(result.width / threadGroupsX) + 1, (int)(result.height / threadGroupsY) + 1, 1);
    }
}
