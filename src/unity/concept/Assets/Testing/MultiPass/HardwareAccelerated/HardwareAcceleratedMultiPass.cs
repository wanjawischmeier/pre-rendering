using UnityEngine;
using PreRendering;
using System;
using static UnityEditor.ShaderData;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, motionVectors, rasterized
    }
    
    public Texture2D[] inputImages;
    public Vector3[] meshTranslations;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public GeometryLoader.Map map;
    public bool validateNeighbors, performanceLogging;
    public float maxCircumference, interpolationRange, depthOffset, maxDifference;
    public int fieldOfViewOffset = 10;
    public int[] dimensions;
    public Vector2Int projectionResolution, rasterizationResolution;
    public AnimationCurve resolutionCurve;

    [Header("Debugging")]
    public DebugChannel debugChannel;
    public DynamicRenderBuffer.DebugMode debugMode;
    public int debugPass;

    [Header("Debugging Values")]
    public RenderTexture motionVectors;
    public RenderTexture[] rasterized;
    public Mesh colliderMesh;

    private int passes;
    private Camera originalCamera;
    private Material postRasterizationMaterial;
    private GeometryLoader geometryLoader;
    private DynamicRenderBuffer[] renderBuffers;
    private Resolution[] projectionResolutions, rasterizationResolutions;


    private Resolution CalculatePassResolutionFromCurve(int pass, Vector2Int inputResolution, AnimationCurve curve)
    {
        // bypass dynamic resolution scaling for now
        new Resolution()
        {
            width = inputResolution.x,
            height = inputResolution.y
        };

        float relativePass = (pass + 1) / (float)passes;
        float relativeCurveMultiplier = curve.Evaluate(relativePass);

        return new Resolution()
        {
            width = Mathf.RoundToInt(inputResolution.x * relativeCurveMultiplier),
            height = Mathf.RoundToInt(inputResolution.y * relativeCurveMultiplier)
        };
    }

    private void Start()
    {
        Debug.Log(SystemInfo.supportedRenderTargetCount);
        originalCamera = GetComponent<Camera>();
        passes = dimensions.Length;
        
        // initialize resolution arrays
        projectionResolutions = new Resolution[passes];
        rasterizationResolutions = new Resolution[passes];

        for (int pass = 0; pass < passes; pass++)
        {
            projectionResolutions[pass] = CalculatePassResolutionFromCurve(pass, projectionResolution, resolutionCurve);
            rasterizationResolutions[pass] = CalculatePassResolutionFromCurve(pass, rasterizationResolution, resolutionCurve);
        }

        // create geometry loader and populate first mesh buffer, as that one will not change
        geometryLoader = new GeometryLoader(dimensions[0], map, computeShader, projectionResolutions, rasterizationResolutions);
        geometryLoader.computeShader.SetFloat("MAX_DIFFERENCE", maxDifference);
        geometryLoader.CalculateMotionVectors(inputImages);

        // initialize render buffers
        renderBuffers = new DynamicRenderBuffer[passes];
        
        for (int pass = 0; pass < passes; pass++)
        {
            Debug.Log($"Projection Resolution {pass}: {projectionResolutions[pass]}");
            Debug.Log($"Rasterization Resolution {pass}: {rasterizationResolutions[pass]}");

            renderBuffers[pass] = new DynamicRenderBuffer(
                pass, dimensions[pass], meshTranslations, transform, originalCamera, geometryLoader.motionVectors,
                projectionResolutions[pass], rasterizationResolutions[pass], rasterizationShader
            );
        }

        geometryLoader.validateNeighbors = validateNeighbors;
        geometryLoader.PopulateMeshBuffer(renderBuffers, 0);

        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetInt("NUM_SLICES", renderBuffers[passes - 1].slices);
        postRasterizationMaterial.SetVector("RESOLUTION", rasterizationResolution.ToVector2());

        for (int imageIndex = 0; imageIndex < inputImages.Length; imageIndex++)
        {
            postRasterizationMaterial.SetTexture($"_Input{imageIndex}", inputImages[imageIndex]);
        }

        var lastRenderBuffer = renderBuffers[passes - 1];
        for (int slice = 0; slice < lastRenderBuffer.slices; slice++)
        {
            postRasterizationMaterial.SetTexture($"_Coordinates{slice}", lastRenderBuffer.targetTextures[slice]);
            postRasterizationMaterial.SetTexture($"_Depth{slice}", lastRenderBuffer.depthTextures[slice]);
        }

        // debug values
        int totalPasses = 0;
        foreach (int dimension in dimensions)
        {
            totalPasses += dimension;
        }

        motionVectors = geometryLoader.motionVectors;
        rasterized = new RenderTexture[totalPasses * 2];
        for (int pass = 0; pass < passes; pass++)
        {
            var color = renderBuffers[pass].targetTextures;
            var depth = renderBuffers[pass].depthTextures;
            for (int slice = 0; slice < color.Length; slice++)
            {
                rasterized[pass * dimensions[0] * 2 + slice * 2] = color[slice];
                rasterized[pass * dimensions[0] * 2 + slice * 2 + 1] = depth[slice];
            }
        }

        // colliderMesh = renderBuffers[0].CreateColliderMesh(0);
    }

    private void Update()
    {
        double startTime;
        double populateTime = 0;
        double renderTime = 0;

        for (int pass = 0; pass < passes; pass++)
        {
            // populate mesh buffer based on previous pass if available
            if (pass != 0)
            {
                startTime = Time.realtimeSinceStartupAsDouble;
                geometryLoader.PopulateMeshBuffer(renderBuffers, pass);
                populateTime += Time.realtimeSinceStartupAsDouble - startTime;
            }

            startTime = Time.realtimeSinceStartupAsDouble;
            // renderBuffers[pass].meshTranslations = meshTranslations;
            renderBuffers[pass].UpdateParamsAndRenderToBuffer(debugMode, maxCircumference, pass == passes - 1 ? 0 : fieldOfViewOffset);
            renderTime += Time.realtimeSinceStartupAsDouble - startTime;
        }
        
        if (performanceLogging)
        {
            Debug.Log($"Populating mesh buffers took {populateTime}s");
            Debug.Log($"Rendering to buffer took {renderTime}s");
        }

        postRasterizationMaterial.SetInt("DEBUG_MODE", (int)debugMode);
        postRasterizationMaterial.SetFloat("INTERPOLATION_RANGE", interpolationRange);
        postRasterizationMaterial.SetFloat("MAX_CIRCUMFERENCE", maxCircumference);
        postRasterizationMaterial.SetFloat("DEPTH_OFFSET", depthOffset);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.motionVectors:
                Graphics.Blit(geometryLoader.motionVectors, destination);
                break;
            case DebugChannel.rasterized:
                Graphics.Blit(renderBuffers.GetTexture(debugPass), destination);
                break;
            default:
                Graphics.Blit(source, destination, postRasterizationMaterial);
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var translation in meshTranslations)
        {
            Gizmos.DrawSphere(translation + Vector3.down * 4, 0.1f);
        }
    }

    private void OnDestroy()
    {
        geometryLoader.Dispose();

        for (int pass = 0; pass < passes; pass++)
        {
            renderBuffers[pass].Dispose();
        }
    }
}
