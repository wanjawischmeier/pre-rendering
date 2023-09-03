using UnityEngine;
using PreRendering;
using System;

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
    public float maxCircumference, interpolationRange;
    public int[] dimensions;
    public Vector2Int projectionResolution, rasterizationResolution;
    public AnimationCurve projectionResolutionCurve, rasterizationResolutionCurve;

    [Header("Debugging")]
    public DebugChannel debugChannel;
    public DynamicRenderBuffer.DebugMode debugMode;
    public int debugPass;

    [Header("Debugging Values")]
    public RenderTexture motionVectors;
    public RenderTexture[] rasterized;

    private int passes;
    private Camera originalCamera;
    private Material postRasterizationMaterial;
    private GeometryLoader geometryLoader;
    private DynamicRenderBuffer[] renderBuffers;
    private Resolution[] projectionResolutions, rasterizationResolutions;


    private Resolution SamplePassResolutionFromCurve(int pass, Vector2Int inputResolution, AnimationCurve curve)
    {
        // bypass dynamic resolution scaling for now
        return new Resolution()
        {
            width = inputResolution.x,
            height = inputResolution.y
        };

        float relativePass = (passes == 1) ? 1 : (float)pass / (passes - 1);
        float relativeCurveMultiplier = projectionResolutionCurve.Evaluate(relativePass);

        return new Resolution()
        {
            width = Mathf.RoundToInt(inputResolution.x * relativeCurveMultiplier),
            height = Mathf.RoundToInt(inputResolution.y * relativeCurveMultiplier)
        };
    }

    private void Start()
    {
        originalCamera = GetComponent<Camera>();
        passes = dimensions.Length;
        
        // initialize arrays
        projectionResolutions = new Resolution[passes];
        rasterizationResolutions = new Resolution[passes];
        renderBuffers = new DynamicRenderBuffer[passes];

        for (int pass = 0; pass < passes; pass++)
        {
            projectionResolutions[pass] = SamplePassResolutionFromCurve(pass, projectionResolution, projectionResolutionCurve);
            rasterizationResolutions[pass] = SamplePassResolutionFromCurve(pass, rasterizationResolution, rasterizationResolutionCurve);

            Debug.Log($"Projection Resolution {pass}: {projectionResolutions[pass]}");
            Debug.Log($"Rasterization Resolution {pass}: {rasterizationResolutions[pass]}");

            renderBuffers[pass] = new DynamicRenderBuffer(
                pass, dimensions[pass], meshTranslations, transform, originalCamera,
                projectionResolutions[pass], rasterizationResolutions[pass], rasterizationShader
            );
        }

        // create geometry loader and populate first mesh buffer, as that one will not change
        geometryLoader = new GeometryLoader(dimensions[0], map, computeShader, projectionResolutions, rasterizationResolutions);
        geometryLoader.CalculateMotionVectors(inputImages);
        geometryLoader.PopulateMeshBuffer(renderBuffers, 0);
        
        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetInt("NUM_SLICES", renderBuffers[passes - 1].slices);
        postRasterizationMaterial.SetVector("RESOLUTION", rasterizationResolution.ToVector2());
        for (int slice = 0; slice < renderBuffers[passes - 1].slices; slice++)
        {
            postRasterizationMaterial.SetTexture($"_Input{slice}", inputImages[slice]);
            postRasterizationMaterial.SetTexture($"_Coordinates{slice}", renderBuffers[passes - 1].targetTextures[slice]);
        }

        // debug values
        motionVectors = geometryLoader.motionVectors;
        rasterized = new RenderTexture[passes * dimensions[0]];
        for (int pass = 0; pass < passes; pass++)
        {
            var source = renderBuffers[pass].targetTextures;
            Array.Copy(source, 0, rasterized, pass * dimensions[0], source.Length);
        }
    }

    private void Update()
    {
        for (int pass = 0; pass < passes; pass++)
        {
            // populate mesh buffer based on previous pass if available
            if (pass != 0)
            {
                geometryLoader.PopulateMeshBuffer(renderBuffers, pass);
            }

            renderBuffers[pass].meshTranslations = meshTranslations;
            renderBuffers[pass].UpdateParamsAndRenderToBuffer(debugMode, maxCircumference);
        }

        postRasterizationMaterial.SetInt("DEBUG_MODE", (int)debugMode);
        postRasterizationMaterial.SetFloat("INTERPOLATION_RANGE", interpolationRange);
        postRasterizationMaterial.SetFloat("MAX_CIRCUMFERENCE", maxCircumference);
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

    private void OnDestroy()
    {
        geometryLoader.Dispose();

        for (int pass = 0; pass < passes; pass++)
        {
            renderBuffers[pass].Dispose();
        }
    }
}
