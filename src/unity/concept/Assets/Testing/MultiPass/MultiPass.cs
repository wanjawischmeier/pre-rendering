using UnityEngine;

public class MultiPass : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public Shader postProcessing;
    public Vector2Int groupSize;
    public int searchRadius = 10;
    public bool debug = false;

    public RenderTexture motionVectors, transformed, transformedResult, invTransformed, depth, depthReprojected, depthResult, result;
    private Material postProcessingMaterial;
    private Vector3 previousPosition = Vector3.one;
    private Camera mainCamera;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int calculateMotionVectors, project, interpolate, reproject, reinterpolate;
    private bool previousDebug = false;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();

        calculateMotionVectors = computeShader.FindKernel("CalculateMotionVectors");
        project = computeShader.FindKernel("Project");
        interpolate = computeShader.FindKernel("Interpolate");
        // reproject = computeShader.FindKernel("Reproject");
        reinterpolate = computeShader.FindKernel("Reinterpolate");
        computeShader.GetKernelThreadGroupSizes(project, out threadGroupSizeX, out threadGroupSizeY, out _);

        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Bilinear;
        invTransformed = new RenderTexture(result);
        invTransformed.format = RenderTextureFormat.ARGBFloat;
        motionVectors = new RenderTexture(result);
        motionVectors.format = RenderTextureFormat.ARGBFloat;
        // depth = new RenderTexture(input.width / groupSize.x, input.height / groupSize.y, 0);
        depth = new RenderTexture(input.width, input.height, 0);
        depth.enableRandomWrite = true;
        depth.format = RenderTextureFormat.RFloat;
        depthResult = new RenderTexture(depth);
        depthReprojected = new RenderTexture(depthResult);
        transformed = new RenderTexture(depth);
        transformed.format = RenderTextureFormat.RGFloat;
        transformedResult = new RenderTexture(transformed);

        computeShader.SetInt("SEARCH_RADIUS", searchRadius);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("GROUP_SIZE", new Vector2(groupSize.x, groupSize.y));
        computeShader.SetTexture(calculateMotionVectors, "Input", input);
        computeShader.SetTexture(calculateMotionVectors, "MotionVectors", motionVectors);
        computeShader.SetTexture(project, "Input", input);
        computeShader.SetTexture(project, "Transformed", transformed);
        computeShader.SetTexture(project, "MotionVectors", motionVectors);
        computeShader.SetTexture(project, "Depth", depth);
        computeShader.SetTexture(interpolate, "Input", input);
        computeShader.SetTexture(interpolate, "Transformed", transformed);
        computeShader.SetTexture(interpolate, "Depth", depth);
        computeShader.SetTexture(interpolate, "TransformedResult", transformedResult);
        computeShader.SetTexture(interpolate, "DepthResult", depthResult);
        computeShader.SetTexture(interpolate, "Result", result);
        /*
        computeShader.SetTexture(reproject, "Input", input);
        computeShader.SetTexture(reproject, "Transformed", transformed);
        computeShader.SetTexture(reproject, "DepthReprojected", depthReprojected);
        computeShader.SetTexture(reproject, "Result", result);
        computeShader.SetTexture(reproject, "InvTransformed", invTransformed);
        */
        computeShader.SetTexture(reinterpolate, "Input", input);
        computeShader.SetTexture(reinterpolate, "InvTransformed", invTransformed);
        computeShader.SetTexture(reinterpolate, "DepthReprojected", depthReprojected);
        computeShader.SetTexture(reinterpolate, "DepthResult", depthResult);
        computeShader.SetTexture(reinterpolate, "Result", result);

        postProcessingMaterial = new Material(postProcessing);
        postProcessingMaterial.SetFloat("PI", Mathf.PI);
        postProcessingMaterial.SetFloat("PI2", Mathf.PI * 2);
        postProcessingMaterial.SetTexture("_MainTex", result);

        computeShader.Dispatch(calculateMotionVectors, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
    }

    private void Update()
    {
        postProcessingMaterial.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
        postProcessingMaterial.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);
        
        previousPosition = transform.position;

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = transformed;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = depth;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = depthResult;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        if (debug != previousDebug)
        {
            if (debug)
            {
                computeShader.EnableKeyword("WIREFRAME");
            }
            else
            {
                computeShader.DisableKeyword("WIREFRAME");
            }

            previousDebug = debug;
        }

        computeShader.SetBool("DEBUG", debug);
        computeShader.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.Dispatch(project, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
        computeShader.Dispatch(interpolate, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
        // computeShader.Dispatch(reproject, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
        // computeShader.Dispatch(reinterpolate, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, postProcessingMaterial);
    }
}
