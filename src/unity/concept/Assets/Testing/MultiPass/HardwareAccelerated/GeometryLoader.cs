using System;
using UnityEngine;
using static UnityEditor.ShaderData;

namespace PreRendering
{
    public class GeometryLoader : IDisposable
    {
        [Serializable]
        public struct Map
        {
            public int imageWidth, imageHeight;
            public float nClip, fClip;
        }

        public readonly RenderTexture motionVectors;
        private ComputeShader computeShader;
        private Resolution[] projectionResolutions, rasterizationResolutions;
        private Texture2D[] inputImages;
        private UnityEngine.Rendering.LocalKeyword usePreviousPassComputeShaderKeyword;
        private int calculateMotionVectorsGroupSizeX, calculateMotionVectorsGroupSizeY;
        private int[] loadTexelsToBufferGroupSizesX, loadTexelsToBufferGroupSizesY;
        private int calculateMotionVectorsKernelId, loadTexelsToBufferKernelId, bufferSize;
        private bool isInputInitialized;

        public GeometryLoader(int bufferSize, Map map, ComputeShader computeShader, Resolution[] projectionResolutions, Resolution[] rasterizationResolutions)
        {
            this.bufferSize = bufferSize;
            this.computeShader = computeShader;
            this.projectionResolutions = projectionResolutions;
            this.rasterizationResolutions = rasterizationResolutions;

            // input dimensions
            // skip color conversions here (RenderTextureReadWrite.Linear)?
            motionVectors = new RenderTexture(map.imageWidth, map.imageHeight, 0);
            motionVectors.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            motionVectors.enableRandomWrite = true;
            motionVectors.volumeDepth = bufferSize;
            motionVectors.format = RenderTextureFormat.ARGBFloat;
            motionVectors.Create();

            calculateMotionVectorsKernelId = computeShader.FindKernel("CalculateMotionVectors");
            loadTexelsToBufferKernelId = computeShader.FindKernel("LoadTexelsToBuffer");
            usePreviousPassComputeShaderKeyword = new UnityEngine.Rendering.LocalKeyword(computeShader, "USE_PREVIOUS_PASS");

            computeShader.GetKernelThreadGroupSizes(calculateMotionVectorsKernelId, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            calculateMotionVectorsGroupSizeX = map.imageWidth / (int)threadGroupSizeX;
            calculateMotionVectorsGroupSizeY = map.imageHeight / (int)threadGroupSizeY;

            computeShader.GetKernelThreadGroupSizes(loadTexelsToBufferKernelId, out threadGroupSizeX, out threadGroupSizeY, out _);
            loadTexelsToBufferGroupSizesX = new int[projectionResolutions.Length];
            loadTexelsToBufferGroupSizesY = new int[projectionResolutions.Length];
            for (int i = 0; i < projectionResolutions.Length; i++)
            {
                loadTexelsToBufferGroupSizesX[i] = projectionResolutions[i].width / (int)threadGroupSizeX;
                loadTexelsToBufferGroupSizesY[i] = projectionResolutions[i].height / (int)threadGroupSizeY;
            }

            computeShader.SetFloat("PI", Mathf.PI);
            computeShader.SetFloat("PI2", Mathf.PI * 2);
            computeShader.SetFloat("NCLIP", map.nClip);
            computeShader.SetFloat("FCLIP", map.fClip);
            computeShader.SetVector("INPUT_RESOLUTION", new Vector2(map.imageWidth, map.imageHeight));
            computeShader.SetTexture(calculateMotionVectorsKernelId, "_MotionVectorsWrite", motionVectors);
            computeShader.SetTexture(loadTexelsToBufferKernelId, "_MotionVectors", motionVectors);
        }

        public void CalculateMotionVectors(Texture2D[] images)
        {
            // check texture compatability with buffer
            if (images.Length != bufferSize)
            {
                Debug.LogError($"Incorrect number of input images (buffer has size {bufferSize}, provided {images.Length} images).");
                return;
            }
            if (images[0].width != motionVectors.width || images[0].height != motionVectors.height)
            {
                Debug.LogError("Incorrect input image dimensions.");
                return;
            }

            for (int slice = 0; slice < bufferSize; slice++)
            {
                // calculate motion vectors
                computeShader.SetInt("TEXTURE_INDEX", slice);
                computeShader.SetTexture(calculateMotionVectorsKernelId, "_Input", images[slice]);
                computeShader.Dispatch(calculateMotionVectorsKernelId, calculateMotionVectorsGroupSizeX, calculateMotionVectorsGroupSizeY, 1);
            }

            inputImages = images;
            isInputInitialized = true;
        }

        public void PopulateMeshBuffer(DynamicRenderBuffer renderBuffer)
        {
            if (!isInputInitialized)
            {
                Debug.LogError("Input images not initialized, unable to populate mesh buffer.");
                return;
            }
            if (renderBuffer.slices > bufferSize)
            {
                Debug.LogError($"Invalid dimensions\nDynamicRenderBuffer: {renderBuffer.slices} slices\nGeometryLoader: {bufferSize}  slices");
            }

            computeShader.SetInt("RENDER_PASS", renderBuffer.pass);
            computeShader.SetVector("PROJECTION_RESOLUTION", projectionResolutions[renderBuffer.pass].ToVector2());
            computeShader.SetKeyword(usePreviousPassComputeShaderKeyword, renderBuffer.pass != 0);
            if (renderBuffer.pass != 0)
            {
                Vector3 rasterizationResolution = rasterizationResolutions[renderBuffer.pass].ToVector2();
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", rasterizationResolution);
            }

            // TODO: all slices in single dispatch call
            for (int slice = 0; slice < renderBuffer.slices; slice++)
            {
                computeShader.SetInt("TEXTURE_INDEX", slice);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Triangles", renderBuffer.triangles[slice]);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Positions", renderBuffer.positions[slice]);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_UVs", renderBuffer.uvs[slice]);
                computeShader.SetTexture(loadTexelsToBufferKernelId, "_Input", inputImages[slice]);
                computeShader.SetTexture(loadTexelsToBufferKernelId, "_PreviousPass", renderBuffer.targetTextures[slice]);

                computeShader.Dispatch(
                    loadTexelsToBufferKernelId,
                    loadTexelsToBufferGroupSizesX[renderBuffer.pass],
                    loadTexelsToBufferGroupSizesY[renderBuffer.pass], 1
                );
            }
        }

        public void Dispose()
        {
            motionVectors.Release();
        }
    }
}