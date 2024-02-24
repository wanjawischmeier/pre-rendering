using System;
using UnityEngine;

namespace PreRendering
{
    public class DynamicRenderBuffer : IDisposable
    {
        public enum DebugMode
        {
            none, zSineFilled, showPerimeter, inputImages, motionVectors, rasterized
        }

        public Vector3[] meshTranslations
        {
            set
            {
                var matricies = new Matrix4x4[value.Length];
                for (int slice = 0; slice < value.Length; slice++)
                {
                    matricies[slice] = Matrix4x4.Translate(value[slice]);
                }

                renderParams.matProps.SetMatrixArray("_ObjectToWorldMatricies", matricies);
            }
        }

        /// <summary>
        /// The cameras used for rendering the buffer. Each camera corresponds to a slice.
        /// </summary>
        public readonly Camera renderCamera;
        /// <summary>
        /// The textures the camera renders to.
        /// </summary>
        public readonly RenderTexture targetTexture, backgroundTexture, depthTexture;
        public readonly GraphicsBuffer triangles, positions, uvs, color;
        public readonly RenderParams renderParams;
        public readonly int pass, verticies, indicies, slices;

        private Camera originalCamera;
        private readonly int cullingMaskLayer;

        private const string CullingMaskLayerName = "Rasterized";
        private const int MaximumShaderSupportedSlices = 8;
        private const int TriangulationVertexRatio = 6;


        public DynamicRenderBuffer(int pass, int passes, int slices, Vector3[] meshTranslations, Transform parentTransform, Camera originalCamera, Resolution projectionResolution, Resolution rasterizationResolution, Shader rasterizationShader, Texture2DArray cubemapFaceImages)
        {
            if (slices > MaximumShaderSupportedSlices)
            {
                Debug.LogError($"Requested creation of a buffer with {slices} slices, but the shader only supports {MaximumShaderSupportedSlices}.");
            }

            // initialize fields and arrays
            this.pass = pass;
            this.slices = slices;
            this.originalCamera = originalCamera;

            // initialize transformation matricies
            var matricies = new Matrix4x4[meshTranslations.Length];
            for (int slice = 0; slice < meshTranslations.Length; slice++)
            {
                matricies[slice] = Matrix4x4.Translate(meshTranslations[slice]);
            }

            // set non-slice specific material properties
            var rasterizationMaterial = new Material(rasterizationShader);
            rasterizationMaterial.SetInt("RENDER_PASS", pass);
            rasterizationMaterial.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolution.width, projectionResolution.height));
            rasterizationMaterial.SetMatrixArray("_ObjectToWorldMatricies", matricies);
            /*
            if (pass != 0)
            {
                for (int slice = 0; slice < inputImages.Length; slice++)
                {
                    rasterizationMaterial.SetTexture($"_Input{slice}", inputImages[slice]);
                }
            }
            */

            // calculate buffer constants
            cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);
            verticies = projectionResolution.width * projectionResolution.height * slices;
            indicies = verticies * TriangulationVertexRatio;

            // create graphics buffers
            triangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies, sizeof(int));
            positions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));
            uvs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 4 * sizeof(float));
            color = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));

            // create target textures
            targetTexture = new RenderTexture(
                rasterizationResolution.width, rasterizationResolution.height, 24,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear
            );
            backgroundTexture = new RenderTexture(
                rasterizationResolution.width, rasterizationResolution.height, 24,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear
            );
            depthTexture = new RenderTexture(
                rasterizationResolution.width, rasterizationResolution.height, 24,
                RenderTextureFormat.Depth, RenderTextureReadWrite.Linear
            );
            targetTexture.Create();
            backgroundTexture.Create();
            depthTexture.Create();

            // create and set up the render camera
            GameObject cameraObject = new GameObject($"RenderCamera_PASS{pass}");
            cameraObject.transform.parent = parentTransform;
            cameraObject.transform.position = Vector3.zero;
            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << cullingMaskLayer;
            renderCamera.SetTargetBuffers(
                new RenderBuffer[] { targetTexture.colorBuffer, backgroundTexture.colorBuffer },
                depthTexture.depthBuffer
            );

            if (pass != passes - 1)
            {
                renderCamera.allowMSAA = false;
            }
            else
            {
                renderCamera.allowMSAA = originalCamera.allowMSAA;
            }

            // copy some flags for comfort
            renderCamera.allowHDR = originalCamera.allowHDR;
            renderCamera.useOcclusionCulling = originalCamera.useOcclusionCulling;
            renderCamera.allowDynamicResolution = originalCamera.allowDynamicResolution;

            // set render material properties
            var renderMatProps = new MaterialPropertyBlock();
            renderMatProps.SetBuffer("_Triangles", triangles);
            renderMatProps.SetBuffer("_Positions", positions);
            renderMatProps.SetBuffer("_UVs", uvs);
            renderMatProps.SetBuffer("_Color", color);
            renderMatProps.SetTexture("_Foreground", targetTexture);
            renderMatProps.SetTexture("_Background", backgroundTexture);
            renderMatProps.SetTexture("_Depth", depthTexture);
            renderMatProps.SetTexture("_CubemapFaces", cubemapFaceImages);

            // set render params
            renderParams = new RenderParams(rasterizationMaterial)
            {
                worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // use tighter bounds
                camera = renderCamera,
                matProps = renderMatProps,
                layer = cullingMaskLayer
            };
        }

        public void UpdateParamsAndRenderToBuffer(DebugMode debugMode, float maxCircumference, float interpolationRange, float fieldOfViewOffset = 0)
        {
            // update camera params
            renderCamera.nearClipPlane = originalCamera.nearClipPlane;
            renderCamera.farClipPlane = originalCamera.farClipPlane;
            renderCamera.fieldOfView = originalCamera.fieldOfView + fieldOfViewOffset;

            // update render params
            renderParams.matProps.SetInteger("DEBUG_MODE", (int)debugMode);
            renderParams.matProps.SetFloat("TIMESTEP", Time.time);
            renderParams.matProps.SetFloat("MAX_CIRCUMFERENCE", maxCircumference);
            renderParams.matProps.SetFloat("INTERPOLATION_RANGE", interpolationRange);

            Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
        }

        public void Dispose()
        {
            triangles.Dispose();
            positions.Dispose();
            uvs.Dispose();

            UnityEngine.Object.Destroy(renderCamera.gameObject);
            targetTexture.Release();
        }
    }
}