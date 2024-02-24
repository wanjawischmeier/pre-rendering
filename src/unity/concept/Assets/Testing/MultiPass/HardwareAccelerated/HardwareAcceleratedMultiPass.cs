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
    public Vector2Int[] rawProjectionResolutions, rawRasterizationResolutions;
    public AnimationCurve projectionResolutionCurve, rasterizationResolutionCurve;
    public Camera uiCamera, backgroundCamera;
    public TextMeshProUGUI uiDebugger;
    public Texture2D[] rawCubemapFaceImages;

    [Header("Debugging")]
    public DynamicRenderBuffer.DebugMode debugMode;
    public int debugPass, debugSlice;

    [Header("Debugging Values")]
    public RenderTexture[] rasterized;
    public Mesh colliderMesh;
    public Texture2DArray cubemapFaceImages;

    private bool previousUIDebuggerState = false;
    private int passes;
    private string compressionInfo;
    private Camera originalCamera;
    private RenderTexture uiTexture, backgroundTexture;
    private Material postRasterizationMaterial;
    private GeometryLoader geometryLoader;
    private DynamicRenderBuffer[] renderBuffers;
    private Resolution[] projectionResolutions, rasterizationResolutions;
    private Matrix4x4 projMat, viewMat, viewProjInvMat;
    private int debugModeCount = Enum.GetValues(typeof(DynamicRenderBuffer.DebugMode)).Length;
    
    public Vector4[] cubePositions;


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

    public static string FormatLargeInteger(int number)
    {
        if (number < 1000)
        {
            return number.ToString();
        }
        else if (number < 1000000)
        {
            double formattedNumber = Math.Round((double)number / 1000, 1);
            return $"{formattedNumber}k";
        }
        else
        {
            double formattedNumber = Math.Round((double)number / 1000000, 1);
            return $"{formattedNumber}M";
        }
    }

    private void Start()
    {
        Debug.Log(SystemInfo.supportedRenderTargetCount);
        originalCamera = GetComponent<Camera>();
        passes = dimensions.Length;

        projMat = GL.GetGPUProjectionMatrix(backgroundCamera.projectionMatrix, false);

        // initialize resolution arrays
        projectionResolutions = new Resolution[passes];
        rasterizationResolutions = new Resolution[passes];
        var inputResolution = new Resolution()
        {
            width = inputImages[0].width,
            height = inputImages[0].height
        };

        int compressedTriangles = 0;
        for (int pass = 0; pass < passes; pass++)
        {
            // projectionResolutions[pass] = CalculatePassResolutionFromCurve(pass, projectionResolution, projectionResolutionCurve);
            // rasterizationResolutions[pass] = CalculatePassResolutionFromCurve(pass, rasterizationResolution, rasterizationResolutionCurve);
            projectionResolutions[pass] = new Resolution
            {
                width = rawProjectionResolutions[pass].x,
                height = rawProjectionResolutions[pass].y,
            };
            rasterizationResolutions[pass] = new Resolution
            {
                width = rawRasterizationResolutions[pass].x,
                height = rawRasterizationResolutions[pass].y,
            };
            compressedTriangles += projectionResolutions[pass].width * projectionResolutions[pass].height * dimensions[pass] * 2;
        }

        int compressedFinalTriangles = projectionResolutions[passes - 1].width * projectionResolutions[passes - 1].height;
        int uncompressedTriangles = rawCubemapFaceImages[0].width * rawCubemapFaceImages[0].height * rawCubemapFaceImages.Length;
        compressionInfo = $"{FormatLargeInteger(uncompressedTriangles)} -> " +
            $"{FormatLargeInteger(compressedTriangles)} ({FormatLargeInteger(compressedFinalTriangles)})\t" +
            $"{100 - Math.Round(compressedTriangles / (float)uncompressedTriangles, 2) * 100}%";

        var sampleTexture = rawCubemapFaceImages[0];
        cubemapFaceImages = new Texture2DArray(sampleTexture.width, sampleTexture.height, rawCubemapFaceImages.Length, sampleTexture.format, false);
        for (int faceIndex = 0; faceIndex < rawCubemapFaceImages.Length; faceIndex++)
        {
            Graphics.CopyTexture(rawCubemapFaceImages[faceIndex], 0, cubemapFaceImages, faceIndex);
            Debug.Log($"Loading Texture: {faceIndex + 1}/{rawCubemapFaceImages.Length}");
        }

        // create geometry loader and populate first mesh buffer, as that one will not change
        geometryLoader = new GeometryLoader(
            dimensions[0], map, computeShader,
            cubemapFaceImages, projectionResolutions,
            rasterizationResolutions, cubePositions
        );
        geometryLoader.computeShader.SetFloat("MAX_DIFFERENCE", maxDifference);

        // initialize render buffers
        renderBuffers = new DynamicRenderBuffer[passes];

        for (int pass = 0; pass < passes; pass++)
        {
            Debug.Log($"Projection Resolution {pass}: {projectionResolutions[pass]}");
            Debug.Log($"Rasterization Resolution {pass}: {rasterizationResolutions[pass]}");

            renderBuffers[pass] = new DynamicRenderBuffer(
                pass, passes, dimensions[pass], meshTranslations, transform, originalCamera,
                projectionResolutions[pass], rasterizationResolutions[pass], rasterizationShader, cubemapFaceImages
            );
        }

        geometryLoader.validateNeighbors = validateNeighbors;
        geometryLoader.PopulateMeshBuffer(renderBuffers, 0, cubePositions);

        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetInt("NUM_SLICES", renderBuffers[passes - 1].slices);
        postRasterizationMaterial.SetVector("RESOLUTION", rasterizationResolutions[passes - 1].ToVector2());

        uiTexture = new RenderTexture(Screen.width, Screen.height, 0);
        uiCamera.targetTexture = uiTexture;
        postRasterizationMaterial.SetTexture("_UI", uiTexture);
        backgroundTexture = new RenderTexture(uiTexture);
        backgroundCamera.targetTexture = backgroundTexture;
        postRasterizationMaterial.SetTexture("_CameraTex", backgroundTexture);

        for (int imageIndex = 0; imageIndex < inputImages.Length; imageIndex++)
        {
            postRasterizationMaterial.SetTexture($"_Input{imageIndex}", inputImages[imageIndex]);
        }

        var lastRenderBuffer = renderBuffers[passes - 1];
        postRasterizationMaterial.SetTexture($"_Coordinates", lastRenderBuffer.targetTexture);
        postRasterizationMaterial.SetTexture($"_Depth", lastRenderBuffer.depthTexture);

        rasterized = new RenderTexture[dimensions.Length * 3];
        for (int pass = 0; pass < passes; pass++)
        {
            var color = renderBuffers[pass].targetTexture;
            var background = renderBuffers[pass].backgroundTexture;
            var depth = renderBuffers[pass].depthTexture;

            rasterized[pass * 2] = color;
            rasterized[pass * 2 + 1] = background;
            rasterized[pass * 2 + 2] = depth;
        }

        Shader.SetGlobalMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.alternateInverseOrientationMatricies);
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
                $"Projection Resolutions:\t\t[{string.Join(", ", projectionResolutions.Select(res => $"{res.width}x{res.height}"))}]\r\n" +
                $"Rasterization Resolutions:\t[{string.Join(", ", rasterizationResolutions.Select(res => $"{res.width}x{res.height}"))}]\r\n" +
                $"Estimated Triangle Count:\t{compressionInfo}\r\n" +
                $"Debugging Mode:\t\t\t{debugMode}\r\n" +
                $"Debugging Pass:\t\t\t{debugPass + 1} / {passes}\r\n" +
                $"Debugging Slice:\t\t\t{debugSlice + 1} / {dimensions[debugPass]}";
        }

        viewMat = backgroundCamera.worldToCameraMatrix;
        viewProjInvMat = (projMat * viewMat).inverse;
        Shader.SetGlobalMatrix("VP_I", viewProjInvMat); // 1st pass load texel kernels also need the inverse vp matrix
        Shader.SetGlobalVector("P_CAM", new Vector4(transform.position.x, transform.position.y, transform.position.z, 1));  // TODO: set this locally!
        
        double startTime;
        double populateTime = 0;
        double renderTime = 0;

        for (int pass = 0; pass < passes; pass++)
        {
            // populate mesh buffer based on previous pass if available
            if (pass != 0)
            {
                startTime = Time.realtimeSinceStartupAsDouble;
                geometryLoader.PopulateMeshBuffer(renderBuffers, pass, cubePositions);
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
        postRasterizationMaterial.SetVector("CAMERA_POSITION", new Vector4(transform.position.x, transform.position.y, transform.position.z, 1));
        postRasterizationMaterial.SetVectorArray("CUBE_POSITIONS", cubePositions);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugMode)
        {
            case DynamicRenderBuffer.DebugMode.inputImages:
                Graphics.Blit(rawCubemapFaceImages[debugSlice], destination, postRasterizationMaterial);
                break;
            case DynamicRenderBuffer.DebugMode.motionVectors:
                // Graphics.Blit(geometryLoader.motionVectors, source, debugSlice, 0); // a bit suboptimal, but oh well...
                Graphics.Blit(source, destination);
                break;
            case DynamicRenderBuffer.DebugMode.rasterized:
                Graphics.Blit(renderBuffers.GetTexture(debugPass), destination, postRasterizationMaterial);
                break;
            default:
                Graphics.Blit(renderBuffers.GetTexture(passes - 1), destination, postRasterizationMaterial);
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
        for (int pass = 0; pass < passes; pass++)
        {
            renderBuffers[pass].Dispose();
        }
    }
}