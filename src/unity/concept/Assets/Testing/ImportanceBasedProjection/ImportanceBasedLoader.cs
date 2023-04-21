using UnityEngine;

public class ImportanceBasedLoader : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public float magnitude, offset;
    public int fac;
    public bool debug;
    public Transform point;
    public Vector2 a0, a1, a2, b0, b1, b2;

    private const float debugRadius = 0.05f;
    private RenderTexture result, transformed;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernel, interpolate, threadGroupsX, threadGroupsY;

    private void Start()
    {
        kernel = computeShader.FindKernel("CSMain");
        interpolate = computeShader.FindKernel("Interpolate");
        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        transformed = new RenderTexture(result);
        transformed.format = RenderTextureFormat.ARGBFloat;

        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(kernel, "Input", input);
        computeShader.SetTexture(interpolate, "Input", input);
        computeShader.SetTexture(kernel, "Transformed", transformed);
        computeShader.SetTexture(interpolate, "Transformed", transformed);
        computeShader.SetTexture(interpolate, "Result", result);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = transformed;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetFloat("MAG", magnitude);
        computeShader.SetFloat("OFF", offset);
        computeShader.SetFloat("FAC", fac);
        computeShader.SetBool("DEBUG", debug);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(interpolate, threadGroupsX * fac, threadGroupsY * fac, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }

    private Vector2 Reduce(Vector3 vector)
    {
        return new Vector2(vector.x, vector.y);
    }

    private Vector3 Expand(Vector2 vector)
    {
        return new Vector3(vector.x, vector.y);
    }

    // thanks to MBo for the idea: https://stackoverflow.com/a/61096648/13215204
    private Vector2 TransformTriangle(Vector2 p, Vector2 a0, Vector2 a1, Vector2 a2, Vector2 b0, Vector2 b1, Vector2 b2)
    {
        // calculate determinant
        float det = (a1.y - a2.y) * (a0.x - a2.x) + (a2.x - a1.x) * (a0.y - a2.y);

        // get barycentric coordinates (3rd one is implied)
        float lambda0 = ((a1.y - a2.y) * (p.x - a2.x) + (a2.x - a1.x) * (p.y - a2.y)) / det;
        float lambda1 = ((a2.y - a0.y) * (p.x - a2.x) + (a0.x - a2.x) * (p.y - a2.y)) / det;

        // apply weighting
        return lambda0 * b0 + lambda1 * b1 + (1 - lambda0 - lambda1) * b2;
    }


    private void OnDrawGizmos()
    {
        Vector2 P = Reduce(point.position);
        Vector2 rep = TransformTriangle(P, a0, a1, a2, b0, b1, b2);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(a0, a1);
        Gizmos.DrawLine(a1, a2);
        Gizmos.DrawLine(a2, a0);
        Gizmos.DrawSphere(Expand(a0), debugRadius);
        Gizmos.DrawSphere(Expand(a1), debugRadius);
        Gizmos.DrawSphere(Expand(a2), debugRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(b0, b1);
        Gizmos.DrawLine(b1, b2);
        Gizmos.DrawLine(b2, b0);
        Gizmos.DrawSphere(Expand(b0), debugRadius);
        Gizmos.DrawSphere(Expand(b1), debugRadius);
        Gizmos.DrawSphere(Expand(b2), debugRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(P, debugRadius);
        Gizmos.DrawSphere(rep, debugRadius);
    }
}
