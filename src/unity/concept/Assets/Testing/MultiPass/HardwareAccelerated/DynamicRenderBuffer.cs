using System;
using UnityEngine;

namespace PreRendering
{
    public class DynamicRenderBuffer : IDisposable
    {
        public enum DebugMode
        {
            none, zSineFilled, showPerimeter
        }

        public Vector3[] meshTranslations
        {
            set
            {
                for (int slice = 0; slice < Mathf.Min(slices, value.Length); slice++)
                {
                    var matrix = Matrix4x4.Translate(value[slice]);
                    renderParams[slice].matProps.SetMatrix("_ObjectToWorld", matrix);
                }
            }
        }

        /// <summary>
        /// The cameras used for rendering the buffer. Each camera corresponds to a slice.
        /// </summary>
        public readonly Camera[] renderCameras;
        /// <summary>
        /// The textures the camera renders to.
        /// </summary>
        public readonly RenderTexture[] targetTextures, depthTextures;
        public readonly GraphicsBuffer[] triangles, positions, uvs;
        public readonly RenderParams[] renderParams;
        public readonly int pass, verticies, indicies, slices;

        private Camera originalCamera;
        private readonly int cullingMaskLayer;

        private const string CullingMaskLayerName = "Rasterized";
        private const int MaximumShaderSupportedSlices = 4;


        public DynamicRenderBuffer(int pass, int slices, Vector3[] meshTranslations, Transform parentTransform, Camera originalCamera, Resolution projectionResolution, Resolution rasterizationResolution, Shader rasterizationShader)
        {
            if (slices > MaximumShaderSupportedSlices)
            {
                Debug.LogError($"Requested creation of a buffer with {slices} slices, but the shader only supports {MaximumShaderSupportedSlices}.");
            }

            // initialize fields and arrays
            this.pass = pass;
            this.slices = slices;
            this.originalCamera = originalCamera;
            triangles = new GraphicsBuffer[slices];
            positions = new GraphicsBuffer[slices];
            uvs = new GraphicsBuffer[slices];
            targetTextures = new RenderTexture[slices];
            depthTextures = new RenderTexture[slices];
            renderCameras = new Camera[slices];
            renderParams = new RenderParams[slices];

            // calculate buffer constants
            cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);
            verticies = projectionResolution.width * projectionResolution.height;
            indicies = verticies * 6;

            for (int slice = 0; slice < slices; slice++)
            {
                // create graphics buffers
                triangles[slice] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies, sizeof(int));
                positions[slice] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));
                uvs[slice] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 2 * sizeof(float));

                // create target texture
                targetTextures[slice] = new RenderTexture(
                    rasterizationResolution.width, rasterizationResolution.height, 24,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear
                );
                depthTextures[slice] = new RenderTexture(
                    rasterizationResolution.width, rasterizationResolution.height, 24,
                    RenderTextureFormat.Depth, RenderTextureReadWrite.Linear
                );
                targetTextures[slice].Create();
                depthTextures[slice].Create();

                // create and set up the render camera
                GameObject cameraObject = new GameObject($"RenderCamera_PASS{pass}_SLICE{slice}");
                cameraObject.transform.parent = parentTransform;
                cameraObject.transform.position = Vector3.zero;
                renderCameras[slice] = cameraObject.AddComponent<Camera>();
                renderCameras[slice].clearFlags = CameraClearFlags.SolidColor;
                renderCameras[slice].backgroundColor = Color.clear;
                renderCameras[slice].cullingMask = 1 << cullingMaskLayer;
                renderCameras[slice].SetTargetBuffers(
                    targetTextures[slice].colorBuffer,
                    depthTextures[slice].depthBuffer
                );

                // copy some flags for comfort
                renderCameras[slice].useOcclusionCulling = originalCamera.useOcclusionCulling;
                renderCameras[slice].allowHDR = originalCamera.allowHDR;
                renderCameras[slice].allowMSAA = originalCamera.allowMSAA;
                renderCameras[slice].allowDynamicResolution = originalCamera.allowDynamicResolution;

                // create material props
                var matrix = Matrix4x4.Translate(meshTranslations[slice]);
                var renderMatProps = new MaterialPropertyBlock();
                renderMatProps.SetInt("RENDER_PASS", pass);
                renderMatProps.SetBuffer("_Triangles", triangles[slice]);
                renderMatProps.SetBuffer("_Positions", positions[slice]);
                renderMatProps.SetBuffer("_UVs", uvs[slice]);
                renderMatProps.SetMatrix("_ObjectToWorld", matrix);

                // set render params
                var rasterizationMaterial = new Material(rasterizationShader);
                renderParams[slice] = new RenderParams(rasterizationMaterial)
                {
                    worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // use tighter bounds
                    camera = renderCameras[slice],
                    matProps = renderMatProps,
                    layer = cullingMaskLayer
                };
            }
        }

        public void UpdateParamsAndRenderToBuffer(DebugMode debugMode, float maxCircumference)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                // update camera params
                renderCameras[slice].nearClipPlane = originalCamera.nearClipPlane;
                renderCameras[slice].farClipPlane = originalCamera.farClipPlane;
                renderCameras[slice].fieldOfView = originalCamera.fieldOfView;
                
                // update render params
                renderParams[slice].matProps.SetInt("DEBUG_MODE", (int)debugMode);
                renderParams[slice].matProps.SetInt("TEXTURE_INDEX", slice);
                renderParams[slice].matProps.SetFloat("TIMESTEP", Time.time);
                renderParams[slice].matProps.SetFloat("MAX_CIRCUMFERENCE", maxCircumference);
                renderParams[slice].matProps.SetBuffer("_Triangles", triangles[slice]);
                renderParams[slice].matProps.SetBuffer("_Positions", positions[slice]);
                renderParams[slice].matProps.SetBuffer("_UVs", uvs[slice]);

                Graphics.RenderPrimitives(renderParams[slice], MeshTopology.Triangles, indicies);
            }
        }

        public void Dispose()
        {
            for (int slice = 0; slice < slices; slice++)
            {
                triangles[slice].Dispose();
                positions[slice].Dispose();
                uvs[slice].Dispose();

                targetTextures[slice].Release();
            }
        }
    }
}