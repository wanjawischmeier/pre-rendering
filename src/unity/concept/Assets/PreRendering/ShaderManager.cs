using UnityEngine;

namespace PreRendering
{
    public class ShaderManager
    {
        ComputeShader computeShader;
        Shader postProcessingShader;
        Material postProcessingMaterial;
        ComputeBuffer positionBuffer;
        ComputeBuffer debugPositionBuffer;
        RenderTexture projected;
        MapConfig mapConfig;
        Vector3[] positions;

        public Vector3 position
        {
            set { computeShader.SetVector("Position", value); }
        }

        public Vector3 rotation
        {
            get { return postProcessingMaterial.GetVector("Rotation"); }
            set { postProcessingMaterial.SetVector("Rotation", value * Mathf.Deg2Rad); }
        }

        public float fov
        {
            get { return postProcessingMaterial.GetFloat("FOV"); }
            set { postProcessingMaterial.SetFloat("FOV", value); }
        }

        public bool debug
        {
            get { return postProcessingMaterial.GetInt("Debug") == 1 ? true : false; }
            set { postProcessingMaterial.SetInt("Debug", value ? 1 : 0); }
        }

        public Vector3[] positionArray
        {
            get { return positions; }
            set
            {
                positions = value;
                positionBuffer.SetData(value);
            }
        }

        readonly Vector2Int geometryResolution;
        readonly int layerDepth, cacheSize, projectKernel, combineKernel;
        readonly uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;

        public ShaderManager(
            ComputeShader computeShader, Shader postProcessingShader,
            Texture2DArray textures, Resolution resolution,
            Vector2Int geometryResolution, MapConfig mapConfig,
            int layerDepth, int cacheSize)
        {
            this.computeShader = computeShader;
            this.postProcessingShader = postProcessingShader;
            this.geometryResolution = geometryResolution;
            this.mapConfig = mapConfig;
            this.layerDepth = layerDepth;
            this.cacheSize = cacheSize;

            postProcessingMaterial = new Material(postProcessingShader);
            projectKernel = computeShader.FindKernel("Project");
            combineKernel = computeShader.FindKernel("Combine");

            computeShader.GetKernelThreadGroupSizes(projectKernel, out projectThreadsX, out projectThreadsY, out uint _);
            computeShader.GetKernelThreadGroupSizes(combineKernel, out combineThreadsX, out combineThreadsY, out uint _);

            Resolution panoramaResolution = Utility.EstimatePanoramaResolution(
                resolution.width, resolution.height, Camera.main.fieldOfView);
            projected = new RenderTexture(
                panoramaResolution.width, panoramaResolution.height, 0, RenderTextureFormat.ARGB64)
            {
                enableRandomWrite = true
            };
            projected.Create();

            // debugPositionArray = new Vector3[cacheSize];
            positionBuffer = new ComputeBuffer(cacheSize, sizeof(float) * 3);
            debugPositionBuffer = new ComputeBuffer(cacheSize, sizeof(float) * 3);

            Shader.SetGlobalFloat("PI", Mathf.PI);
            Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
            Shader.SetGlobalFloat("FCLIP", mapConfig.fclip);
            Shader.SetGlobalInt("MX_IDX", layerDepth);
            Shader.SetGlobalVector("InputArrayRes", new Vector2(mapConfig.textureWidth, mapConfig.textureHeight));
            Shader.SetGlobalVector("ProjectedRes", new Vector2(projected.width, projected.height));
            Shader.SetGlobalTexture("_InputArray", textures);
            Shader.SetGlobalTexture("_Projected", projected);

            computeShader.SetBuffer(projectKernel, "PositionBuffer", positionBuffer);
            computeShader.SetBuffer(projectKernel, "DebugPositionBuffer", debugPositionBuffer);
        }

        ~ShaderManager()
        {
            if (positionBuffer != null) positionBuffer.Release();
            if (debugPositionBuffer != null) debugPositionBuffer.Release();
            if (projected != null) projected.Release();
        }

        public void Project(int width, int height, int index)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB64);
            rt.enableRandomWrite = true;

            Shader.SetGlobalVector("ProjectionRes", new Vector2(width, height));
           computeShader.SetInt("IMG_IDX", index);
           computeShader.SetTexture(projectKernel, "_Result", rt);
           computeShader.SetTexture(combineKernel, "_Input", rt);
           
           computeShader.Dispatch(projectKernel, width / (int)projectThreadsX, height / (int)projectThreadsY, 1);
           computeShader.Dispatch(combineKernel, projected.width / (int)combineThreadsX, projected.height / (int)combineThreadsY, 1);

            RenderTexture.ReleaseTemporary(rt);
        }

        public void Render(ref RenderTexture destination)
        {
            Graphics.Blit(projected, destination, postProcessingMaterial);

            RenderTexture tmp = RenderTexture.active;
            RenderTexture.active = projected;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = tmp;
        }
    }

}