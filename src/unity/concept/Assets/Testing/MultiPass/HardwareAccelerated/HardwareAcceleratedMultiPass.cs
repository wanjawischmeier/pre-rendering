using UnityEngine;
using PreRendering;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, motionVectors, rasterized
    }
    
    public Texture2D input;
    public Texture2D[] inputImages;
    public Vector3[] meshTranslations;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public GeometryLoader.Map map;
    public float maxCircumference;
    public float fClipCutoff = 1;
    public int[] dimensions;
    public Vector2Int projectionResolution, rasterizationResolution;
    public AnimationCurve projectionResolutionCurve, rasterizationResolutionCurve;

    [Header("Debugging")]
    public DebugChannel debugChannel;
    public DynamicRenderBuffer.DebugMode debugMode;
    public int debugPass;

    [Header("Debugging Values")]
    public Camera originalCamera;
    public RenderTexture motionVectors;
    public RenderTexture[] rasterized;

    public Material postRasterizationMaterial;
    private GeometryLoader geometryLoader;
    private DynamicRenderBuffer[] renderBuffers;
    private Resolution[] projectionResolutions, rasterizationResolutions;
    private int passes;


    private Resolution SamplePassResolutionFromCurve(int pass, Vector2Int inputResolution, AnimationCurve curve)
    {
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
        Debug.Log(SystemInfo.supportsRenderTargetArrayIndexFromVertexShader);
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
        geometryLoader = new GeometryLoader(dimensions[0], map, computeShader, projectionResolutions);
        geometryLoader.CalculateMotionVectors(inputImages);
        geometryLoader.PopulateMeshBuffer(renderBuffers[0]);

        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetVector("RESOLUTION", rasterizationResolution.ToVector2());
        postRasterizationMaterial.SetTexture("_Input", input);
        postRasterizationMaterial.SetTexture("_Coordinates", renderBuffers.GetTexture(passes - 1, 1));

        // debug values
        motionVectors = geometryLoader.motionVectors;
        rasterized = renderBuffers[passes - 1].targetTextures;
    }

    private void Update()
    {
        renderBuffers[0].UpdateParamsAndRenderToBuffer(debugMode, maxCircumference);
        /*
        for (int pass = 0; pass < passes; pass++)
        {
            if (pass != 0)
            {
                computeShader.SetInt("RENDER_PASS", pass);
                // computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass].x, projectionResolutions[pass].y));

                if (pass == 1)
                {
                    computeShader.EnableKeyword("USE_PREVIOUS_PASS");
                }
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", rasterizationResolution.ToVector2());
                // computeShader.SetTexture(loadTexelsToQuadBufferKernel, "_PreviousPass", rasterized[pass - 1]);

                // computeShader.Dispatch(loadTexelsToQuadBufferKernel, projectionResolutions[pass].x / (int)threadGroupSizeX, projectionResolutions[pass].y / (int)threadGroupSizeY, 1);
            }

            // renderBuffers[pass].UpdateAndRenderToBuffer(debugMode, fClip, maxCircumference);
        }

        if (passes != 1)
        {
            computeShader.DisableKeyword("USE_PREVIOUS_PASS");
        }
        */
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
