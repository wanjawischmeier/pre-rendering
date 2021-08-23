using UnityEngine;

public class ComputeSizeTest : MonoBehaviour
{
    public ComputeShader computeShader;
    RenderTexture result;
    ComputeBuffer buffer;

    uint numThreadsX, numThreadsY, numThreadsZ;

    void Start()
    {
        result = new RenderTexture(2, 2, 24);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;
        computeShader.SetTexture(0, "Result", result);

        buffer = new ComputeBuffer(result.width * result.height, sizeof(float) * 3);
        Vector3[] vectors = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 1, 1)
        };
        buffer.SetData(vectors);
        computeShader.SetBuffer(0, "Buff", buffer);

        computeShader.GetKernelThreadGroupSizes(0, out numThreadsX, out numThreadsY, out numThreadsZ);
        computeShader.Dispatch(0, result.width / (int)numThreadsX, result.height / (int)numThreadsY, (int)numThreadsZ);
    }
    private void OnDestroy()
    {
        if (buffer != null) buffer.Release();
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}