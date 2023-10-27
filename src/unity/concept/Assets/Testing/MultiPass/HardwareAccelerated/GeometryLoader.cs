using System;
using UnityEngine;

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

        /// <summary>
        /// Wether the gpu should search neighboring pixels to filter overlapping edges.
        /// Can only be set upon startup.
        /// </summary>
        public bool validateNeighbors
        {
            get => _validateNeighbors;
            set
            {
                _validateNeighbors = value;
                computeShader.SetBool("VALIDATE_NEIGHBORS", value);
            }
        }

        public readonly RenderTexture motionVectors;
        public ComputeShader computeShader;
        private Resolution[] projectionResolutions, rasterizationResolutions;
        private Texture2D[] inputImages;
        private UnityEngine.Rendering.LocalKeyword usePreviousPassComputeShaderKeyword;
        private int calculateMotionVectorsGroupSizeX, calculateMotionVectorsGroupSizeY;
        private int[] loadTexelsToBufferGroupSizesX, loadTexelsToBufferGroupSizesY;
        private int calculateMotionVectorsKernelId, loadTexelsToBufferKernelId, bufferSize;
        private bool isInputInitialized;
        private bool _validateNeighbors = false;

        public GeometryLoader(int bufferSize, Map map, ComputeShader computeShader, Resolution[] projectionResolutions, Resolution[] rasterizationResolutions)
        {
            this.bufferSize = bufferSize;
            this.computeShader = computeShader;
            this.projectionResolutions = projectionResolutions;
            this.rasterizationResolutions = rasterizationResolutions;

            // input dimensions
            motionVectors = new RenderTexture(
                map.imageWidth, map.imageHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear
            );
            motionVectors.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            motionVectors.enableRandomWrite = true;
            motionVectors.volumeDepth = bufferSize;
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

            computeShader.SetBool("VALIDATE_NEIGHBORS", false);
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
            if (images.Length < bufferSize)
            {
                Debug.LogError($"Not enough images provided (buffer has size {bufferSize}, provided {images.Length} images).");
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

        public void PopulateMeshBuffer(DynamicRenderBuffer[] renderBuffers, int pass)
        {
            if (!isInputInitialized)
            {
                Debug.LogError("Input images not initialized, unable to populate mesh buffer.");
                return;
            }

            var renderBuffer = renderBuffers[pass];
            int slices = renderBuffer.slices;
            if (slices > bufferSize)
            {
                Debug.LogError($"Invalid dimensions\nDynamicRenderBuffer: {slices} slices\nGeometryLoader: {bufferSize}  slices");
            }

            computeShader.SetBool("VALIDATE_NEIGHBORS", validateNeighbors);
            computeShader.SetInt("RENDER_PASS", pass);
            computeShader.SetVector("PROJECTION_RESOLUTION", projectionResolutions[pass].ToVector2());
            computeShader.SetKeyword(usePreviousPassComputeShaderKeyword, pass != 0);
            if (pass != 0)
            {
                Vector3 previousRasterizationResolution = rasterizationResolutions[pass - 1].ToVector2();
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", previousRasterizationResolution);

                DynamicRenderBuffer previousBuffer = renderBuffers[pass - 1];
                computeShader.SetInt("NUM_PREVIOUS_SLICES", previousBuffer.slices);
                for (int slice = 0; slice < previousBuffer.slices; slice++)
                {
                    computeShader.SetTexture(loadTexelsToBufferKernelId, $"_PreviousPass{slice}", previousBuffer.targetTextures[slice]);
                }
            }

            // TODO: all slices in single dispatch call
            for (int slice = 0; slice < slices; slice++)
            {
                computeShader.SetInt("TEXTURE_INDEX", slice);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Triangles", renderBuffer.triangles[slice]);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Positions", renderBuffer.positions[slice]);
                computeShader.SetBuffer(loadTexelsToBufferKernelId, "_UVs", renderBuffer.uvs[slice]);

                computeShader.Dispatch(
                    loadTexelsToBufferKernelId,
                    loadTexelsToBufferGroupSizesX[pass],
                    loadTexelsToBufferGroupSizesY[pass], 1
                );
            }
        }

        public void Dispose()
        {
            motionVectors.Release();
        }
    }
}