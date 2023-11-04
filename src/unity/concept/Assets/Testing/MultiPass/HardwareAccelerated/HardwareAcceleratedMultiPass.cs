using UnityEngine;
using PreRendering;
using TMPro;
using System;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    public Texture2D[] inputImages;
    public Vector3[] meshTranslations;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public GeometryLoader.Map map;
    public bool validateNeighbors, performanceLogging, uiDebuggerState;
    public float interpolationRange, depthOffset, maxDifference;
    public float[] maxCircumferences;
    public int fieldOfViewOffset = 10;
    public int[] dimensions;
    public Vector2Int motionVectorResolution, projectionResolution, rasterizationResolution;
    public AnimationCurve projectionResolutionCurve, rasterizationResolutionCurve;
    public Camera uiCamera;
    public TextMeshProUGUI uiDebugger;

    [Header("Debugging")]
    public DynamicRenderBuffer.DebugMode debugMode;
    public int debugPass, debugSlice;

    [Header("Debugging Values")]
    public RenderTexture motionVectors;
    public RenderTexture[] rasterized;
    public Mesh colliderMesh;

    private bool previousUIDebuggerState = false;
    private int passes, totalTriangles;
    private Camera originalCamera;
    private RenderTexture uiTexture;
    private Material postRasterizationMaterial;
    private GeometryLoader geometryLoader;
    private DynamicRenderBuffer[] renderBuffers;
    private Resolution[] projectionResolutions, rasterizationResolutions;
    private int debugModeCount = Enum.GetValues(typeof(DynamicRenderBuffer.DebugMode)).Length;


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
        var inputResolution = new Resolution()
        {
            width = inputImages[0].width,
            height = inputImages[0].height
        };
        var _motionVectorResolution = new Resolution()
        {
            width = motionVectorResolution.x,
            height = motionVectorResolution.y
        };

        for (int pass = 0; pass < passes; pass++)
        {
            projectionResolutions[pass] = CalculatePassResolutionFromCurve(pass, projectionResolution, projectionResolutionCurve);
            rasterizationResolutions[pass] = CalculatePassResolutionFromCurve(pass, rasterizationResolution, rasterizationResolutionCurve);
            totalTriangles += projectionResolutions[pass].width * projectionResolutions[pass].height * dimensions[pass] * 2;
        }

        // create geometry loader and populate first mesh buffer, as that one will not change
        geometryLoader = new GeometryLoader(dimensions[0], map, computeShader, inputResolution, _motionVectorResolution, projectionResolutions, rasterizationResolutions);
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
                projectionResolutions[pass], rasterizationResolutions[pass], rasterizationShader, inputImages
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
        uiTexture = new RenderTexture(Screen.width, Screen.height, 0);
        uiCamera.targetTexture = uiTexture;
        postRasterizationMaterial.SetTexture("_UI", uiTexture);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            debugMode = (DynamicRenderBuffer.DebugMode)((int)(debugMode + 1) % debugModeCount);
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            uiDebuggerState = !uiDebuggerState;
        }
        if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.mouseScrollDelta.y < 0) && debugSlice > 0)
        {
            debugSlice -= 1;
        }
        if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.mouseScrollDelta.y > 0) && debugSlice < dimensions[debugPass] - 1)
        {
            debugSlice += 1;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && debugPass > 0)
        {
            debugPass -= 1;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && debugPass < passes - 1)
        {
            debugPass += 1;
        }
        if (debugSlice > dimensions[debugPass] - 1)
        {
            debugSlice = dimensions[debugPass] - 1;
        }
        if (uiDebuggerState != previousUIDebuggerState)
        {
            uiCamera.enabled = uiDebuggerState;
            previousUIDebuggerState = uiDebuggerState;
            postRasterizationMaterial.SetInteger("UI_DEBUGGER", uiDebuggerState ? 1 : 0);
        }
        if (uiDebuggerState)
        {
            uiDebugger.text = $"3D Position:\t\t\t\t{transform.position}\r\n" +
                $"Screen Resolution:\t\t{Screen.width}x{Screen.height}\r\n" +
                $"Input Resolution:\t\t\t{inputImages[0].width}x{inputImages[0].height}\r\n" +
                $"Motion Vector Resolution:\t{motionVectorResolution.x}x{motionVectorResolution.y}\r\n" +
                $"Projection Resolutions:\t\t[{string.Join(", ", projectionResolutions.Select(res => $"{res.width}x{res.height}"))}]\r\n" +
                $"Rasterization Resolutions:\t[{string.Join(", ", rasterizationResolutions.Select(res => $"{res.width}x{res.height}"))}]\r\n" +
                $"Estimated Triangle Count:\t{totalTriangles}\r\n" +
                $"Debugging Mode:\t\t\t{debugMode}\r\n" +
                $"Debugging Pass:\t\t\t{debugPass + 1} / {passes}\r\n" +
                $"Debugging Slice:\t\t\t{debugSlice + 1} / {dimensions[debugPass]}";
        }

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
            renderBuffers[pass].meshTranslations = meshTranslations;    // only for debugging purposes
            renderBuffers[pass].UpdateParamsAndRenderToBuffer(debugMode, maxCircumferences[pass], interpolationRange, pass == passes - 1 ? 0 : fieldOfViewOffset);
            renderTime += Time.realtimeSinceStartupAsDouble - startTime;
        }

        if (performanceLogging)
        {
            Debug.Log($"Populating mesh buffers took {populateTime}s");
            Debug.Log($"Rendering to buffer took {renderTime}s");
        }

        postRasterizationMaterial.SetInt("DEBUG_MODE", (int)debugMode);
        postRasterizationMaterial.SetFloat("MAX_CIRCUMFERENCE", maxCircumferences[passes - 1]);
        postRasterizationMaterial.SetFloat("DEPTH_OFFSET", depthOffset);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugMode)
        {
            case DynamicRenderBuffer.DebugMode.inputImages:
                Graphics.Blit(inputImages[debugSlice], destination, postRasterizationMaterial);
                break;
            case DynamicRenderBuffer.DebugMode.motionVectors:
                Graphics.Blit(geometryLoader.motionVectors, source, debugSlice, 0); // a bit suboptimal, but oh well...
                Graphics.Blit(source, destination, postRasterizationMaterial);
                break;
            case DynamicRenderBuffer.DebugMode.rasterized:
                Graphics.Blit(renderBuffers.GetTexture(debugPass, debugSlice), destination, postRasterizationMaterial);
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