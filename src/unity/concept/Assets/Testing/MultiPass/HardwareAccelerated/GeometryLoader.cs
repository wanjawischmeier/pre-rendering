using System;
using UnityEngine;

namespace PreRendering
{
    public class GeometryLoader
    {
        [Serializable]
        public struct Map
        {
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

        public ComputeShader computeShader;
        private Texture2DArray cubemapFaceImages;
        private Resolution[] projectionResolutions, rasterizationResolutions;
        private UnityEngine.Rendering.LocalKeyword usePreviousPassComputeShaderKeyword;
        private int[] loadTexelsToBufferGroupSizesX, loadTexelsToBufferGroupSizesY;
        private int loadTexelsToBufferKernelId, bufferSize;
        private bool _validateNeighbors = false;

        public GeometryLoader(int bufferSize, Map map, ComputeShader computeShader, Texture2DArray cubemapFaceImages, Resolution[] projectionResolutions, Resolution[] rasterizationResolutions, Vector4[] cubePositions)
        {
            this.bufferSize = bufferSize;
            this.computeShader = computeShader;
            this.projectionResolutions = projectionResolutions;
            this.rasterizationResolutions = rasterizationResolutions;

            loadTexelsToBufferKernelId = computeShader.FindKernel("LoadTexelsToBuffer");
            usePreviousPassComputeShaderKeyword = new UnityEngine.Rendering.LocalKeyword(computeShader, "USE_PREVIOUS_PASS");

            computeShader.GetKernelThreadGroupSizes(loadTexelsToBufferKernelId, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            loadTexelsToBufferGroupSizesX = new int[projectionResolutions.Length];
            loadTexelsToBufferGroupSizesY = new int[projectionResolutions.Length];
            for (int i = 0; i < projectionResolutions.Length; i++)
            {
                loadTexelsToBufferGroupSizesX[i] = projectionResolutions[i].width / (int)threadGroupSizeX;
                loadTexelsToBufferGroupSizesY[i] = projectionResolutions[i].height / (int)threadGroupSizeY;
            }

            computeShader.SetTexture(loadTexelsToBufferKernelId, "_CubemapFaces", cubemapFaceImages);

            computeShader.SetBool("VALIDATE_NEIGHBORS", false);
            computeShader.SetFloat("PI", Mathf.PI);
            computeShader.SetFloat("PI2", Mathf.PI * 2);
            computeShader.SetFloat("NCLIP", map.nClip);
            computeShader.SetFloat("FCLIP", map.fClip);
            computeShader.SetVector("INPUT_RESOLUTION", new Vector2(cubemapFaceImages.width, cubemapFaceImages.height));
            computeShader.SetVectorArray("CUBE_POSITIONS", cubePositions);
            computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
            computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        }

        public void PopulateMeshBuffer(DynamicRenderBuffer[] renderBuffers, int pass, Vector4[] cubePositions)
        {
            var renderBuffer = renderBuffers[pass];
            int slices = renderBuffer.slices;
            if (slices > bufferSize)
            {
                Debug.LogError($"Invalid dimensions\nDynamicRenderBuffer: {slices} slices\nGeometryLoader: {bufferSize}  slices");
            }
            computeShader.SetVectorArray("CUBE_POSITIONS", cubePositions);
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

                // TODO: only upon startup?
                computeShader.SetTexture(loadTexelsToBufferKernelId, $"_PreviousPass", previousBuffer.targetTexture);
                computeShader.SetTexture(loadTexelsToBufferKernelId, $"_PreviousDepth", previousBuffer.depthTexture);
            }

            computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Triangles", renderBuffer.triangles);
            computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Positions", renderBuffer.positions);
            computeShader.SetBuffer(loadTexelsToBufferKernelId, "_UVs", renderBuffer.uvs);
            computeShader.SetBuffer(loadTexelsToBufferKernelId, "_Color", renderBuffer.color);

            computeShader.Dispatch(
                loadTexelsToBufferKernelId,
                loadTexelsToBufferGroupSizesX[pass],
                loadTexelsToBufferGroupSizesY[pass],
                slices
            );
        }
    }
}