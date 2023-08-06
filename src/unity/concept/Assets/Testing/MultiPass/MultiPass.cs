using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

public class MultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, transformed, depth, result, depthResult
    }

    public Texture2D input;
    public ComputeShader computeShader;
    public Shader postProcessing;
    public Vector2Int groupSize, projectionResolution, rasterizationResolution;
    public int searchRadius = 10;
    public int debugInt;
    public bool debug = false;
    public DebugChannel debugChannel;

    public RenderTexture motionVectors, transformed, transformedResult, invTransformed, depth, depthReprojected, depthResult, result;
    private Material postProcessingMaterial;
    private Vector3 previousPosition = Vector3.one;
    private Camera mainCamera;
    private int threadGroupSizeX, threadGroupSizeY;
    private int calculateMotionVectors, project, interpolate, reproject, reinterpolate, calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, projectGroupsX, projectGroupsY, interpolateGroupsX, interpolateGroupsY;
    private bool previousDebug = false;

    private void Start()
    {
        mainCamera = Camera.main;

        calculateMotionVectors = computeShader.FindKernel("CalculateMotionVectors");
        project = computeShader.FindKernel("Project");
        interpolate = computeShader.FindKernel("Interpolate");
        // reproject = computeShader.FindKernel("Reproject");
        reinterpolate = computeShader.FindKernel("Reinterpolate");

        computeShader.GetKernelThreadGroupSizes(project, out uint tmpX, out uint tmpY, out _);
        threadGroupSizeX = (int)tmpX;
        threadGroupSizeY = (int)tmpY;
        calculateMotionVectorGroupsX = input.width / threadGroupSizeY;
        calculateMotionVectorGroupsY = input.height / threadGroupSizeY;
        projectGroupsX = projectionResolution.x / threadGroupSizeY;
        projectGroupsY = projectionResolution.y / threadGroupSizeY;
        interpolateGroupsX = rasterizationResolution.x / threadGroupSizeY;
        interpolateGroupsY = rasterizationResolution.y / threadGroupSizeY;

        // input dimensions
        motionVectors = new RenderTexture(input.width, input.height, 0);
        motionVectors.enableRandomWrite = true;
        motionVectors.format = RenderTextureFormat.ARGBFloat;

        // projection dimensions
        depth = new RenderTexture(projectionResolution.x, projectionResolution.y, 0);
        depth.enableRandomWrite = true;
        depth.format = RenderTextureFormat.RFloat;
        transformed = new RenderTexture(depth);
        transformed.format = RenderTextureFormat.RGFloat;

        // result/rasterization dimensions
        result = new RenderTexture(rasterizationResolution.x, rasterizationResolution.y, 0);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Bilinear;
        depthResult = new RenderTexture(depth);

        // depth = new RenderTexture(input.width / groupSize.x, input.height / groupSize.y, 0);
        depthReprojected = new RenderTexture(depthResult);
        transformedResult = new RenderTexture(transformed);

        invTransformed = new RenderTexture(result);
        invTransformed.format = RenderTextureFormat.ARGBFloat;

        // computeShader.SetInt("SEARCH_RADIUS", searchRadius);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolution.x, projectionResolution.y));
        computeShader.SetVector("RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolution.x, rasterizationResolution.y));
        // computeShader.SetVector("GROUP_SIZE", new Vector2(groupSize.x, groupSize.y));
        // computeShader.SetTexture(calculateMotionVectors, "Input", input);
        // computeShader.SetTexture(calculateMotionVectors, "MotionVectors", motionVectors);
        computeShader.SetTexture(project, "Input", input);
        computeShader.SetTexture(project, "Transformed", transformed);
        // computeShader.SetTexture(project, "MotionVectors", motionVectors);
        // computeShader.SetTexture(project, "Depth", depth);
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
        computeShader.SetTexture(reinterpolate, "Input", input);
        computeShader.SetTexture(reinterpolate, "InvTransformed", invTransformed);
        computeShader.SetTexture(reinterpolate, "DepthReprojected", depthReprojected);
        computeShader.SetTexture(reinterpolate, "DepthResult", depthResult);
        computeShader.SetTexture(reinterpolate, "Result", result);
        
        postProcessingMaterial = new Material(postProcessing);
        postProcessingMaterial.SetFloat("PI", Mathf.PI);
        postProcessingMaterial.SetFloat("PI2", Mathf.PI * 2);
        postProcessingMaterial.SetTexture("_MainTex", result);

        computeShader.Dispatch(calculateMotionVectors, input.width / threadGroupSizeY, input.height / threadGroupSizeY, 1);
        */
    }

    private void Update()
    {
        /*
        postProcessingMaterial.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
        postProcessingMaterial.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);
        
        previousPosition = transform.position;
        */
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
        /*
        bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
        Matrix4x4 M = transform.localToWorldMatrix;
        Matrix4x4 V = mainCamera.worldToCameraMatrix;
        Matrix4x4 P = mainCamera.projectionMatrix;
        if (d3d)
        {
            // Invert Y for rendering to a render texture
            for (int i = 0; i < 4; i++)
            {
                P[1, i] = -P[1, i];
            }
            // Scale and bias from OpenGL -> D3D depth range
            for (int i = 0; i < 4; i++)
            {
                P[2, i] = P[2, i] * 0.5f + P[3, i] * 0.5f;
            }
        }
        */
        // Matrix4x4 MVP = P*V*M;
        // MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix * transform.localToWorldMatrix;
        Matrix4x4 MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;
        // Matrix4x4 MVP = mainCamera.nonJitteredProjectionMatrix * transform.worldToLocalMatrix;

        // computeShader.SetBool("DEBUG", debug);
        // computeShader.SetInt("DEBUG_INT", debugInt);
        // computeShader.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        // computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetMatrix("MVP", MVP);
        computeShader.Dispatch(project, projectGroupsX, projectGroupsY, 1);
        computeShader.Dispatch(interpolate, projectGroupsX, projectGroupsY, 1);
        // computeShader.Dispatch(reproject, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
        // computeShader.Dispatch(reinterpolate, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.transformed:
                Graphics.Blit(transformed, destination);
                break;
            case DebugChannel.depth:
                Graphics.Blit(depth, destination);
                break;
            case DebugChannel.result:
                Graphics.Blit(result, destination);
                break;
            case DebugChannel.depthResult:
                Graphics.Blit(depthResult, destination);
                break;
            default:
                Graphics.Blit(null, destination, postProcessingMaterial);
                break;
        }
    }
}
