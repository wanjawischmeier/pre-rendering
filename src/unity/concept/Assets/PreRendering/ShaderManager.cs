using UnityEngine;

namespace PreRendering
{
    public class ShaderManager
    {
        ComputeShader computeShader;
        Shader postProcessingShader;
        Material postProcessingMaterial;
        RenderTexture projection;
        RenderTexture unitProjection;
        Map map;

        /// <summary>
        /// The position inside the compute shader.
        /// </summary>
        public Vector3 position
        {
            set { computeShader.SetVector("Position", value); }
        }

        /// <summary>
        /// Sets the position compute buffer.
        /// </summary>
        public Vector3 positionOffset
        {
            set { computeShader.SetVector("PositionOffset", value); }
        }

        /// <summary>
        /// The rotation inside the post processing shader.
        /// The value will be converted to radians before passed to the gpu.
        /// </summary>
        public Vector3 rotation
        {
            get { return postProcessingMaterial.GetVector("Rotation"); }
            set { postProcessingMaterial.SetVector("Rotation", value * Mathf.Deg2Rad); }
        }

        /// <summary>
        /// The field of view used for the gnomonic projection inside the post processing shader.
        /// </summary>
        public float fov
        {
            get { return postProcessingMaterial.GetFloat("FOV"); }
            set { postProcessingMaterial.SetFloat("FOV", value * Mathf.Deg2Rad); }
        }

        /// <summary>
        /// If enabled, the post processing shader will just pass through the projected texture coordinates.
        /// </summary>
        public bool shaderDebug
        {
            get { return postProcessingMaterial.GetInt("Debug") == 1 ? true : false; }
            set { postProcessingMaterial.SetInt("Debug", value ? 1 : 0); }
        }

        /// <summary>
        /// At which point the shader should start overlaying the non-projected image to fill gaps.
        /// </summary>
        public float cutoff
        {
            get { return postProcessingMaterial.GetFloat("CUTOFF"); }
            set { postProcessingMaterial.SetFloat("CUTOFF", value); }
        }

        readonly int projectKernel, combineKernel;
        readonly uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;

        public ShaderManager(
            ComputeShader computeShader, Shader postProcessingShader,
            Texture2DArray textures, Resolution projectionResolution,
            Map map, int layerDepth)
        {
            this.computeShader = computeShader;
            this.postProcessingShader = postProcessingShader;
            this.map = map;

            postProcessingMaterial = new Material(postProcessingShader);
            projectKernel = computeShader.FindKernel("Project");
            combineKernel = computeShader.FindKernel("Combine");

            computeShader.GetKernelThreadGroupSizes(projectKernel, out projectThreadsX, out projectThreadsY, out uint _);
            computeShader.GetKernelThreadGroupSizes(combineKernel, out combineThreadsX, out combineThreadsY, out uint _);

            projection = new RenderTexture(
                projectionResolution.width, projectionResolution.height, 1, RenderTextureFormat.ARGB64)
            { enableRandomWrite = true };
            unitProjection = new RenderTexture(projection);
            projection.Create();
            unitProjection.Create();

            Shader.SetGlobalFloat("PI", Mathf.PI);
            Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
            Shader.SetGlobalFloat("NCLIP", map.nClip);
            Shader.SetGlobalFloat("FCLIP", map.fClip);
            Shader.SetGlobalInt("MX_IDX", layerDepth);
            Shader.SetGlobalVector("InputArrayRes", new Vector2(map.resolution.width, map.resolution.height));
            Shader.SetGlobalVector("ProjectedRes", new Vector2(projection.width, projection.height));
            Shader.SetGlobalTexture("_InputArray", textures);
            Shader.SetGlobalTexture("_Projection", projection);
            Shader.SetGlobalTexture("_UnitProjection", unitProjection);
        }

        ~ShaderManager() => Release();

        /// <summary>
        /// Releases all compute buffers and textures used at runtime.
        /// </summary>
        public void Release()
        {
            if (projection != null) projection.Release();
        }

        /// <summary>
        /// Use the project kernel to get a projection and then add it
        /// to the output texture using the combine kernel.
        /// </summary>
        /// <param name="index">The index at which the buffer should be acessed.</param>
        public void Project(int width, int height, int index)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB64);
            rt.enableRandomWrite = true;

            Shader.SetGlobalVector("ProjectionRes", new Vector2(width, height));
            computeShader.SetInt("IMG_IDX", index);
            computeShader.SetTexture(projectKernel, "_Result", rt);
            computeShader.SetTexture(combineKernel, "_Input", rt);
            
            computeShader.Dispatch(projectKernel, width / (int)projectThreadsX, height / (int)projectThreadsY, 1);
            computeShader.Dispatch(combineKernel, projection.width / (int)combineThreadsX, projection.height / (int)combineThreadsY, 1);

            RenderTexture.ReleaseTemporary(rt);
        }

        /// <summary>
        /// Blit the projected texture to the destination
        /// (should be called in OnRenderImage) and apply post processing.
        /// </summary>
        /// <param name="destination"></param>
        public void Render(ref RenderTexture destination)
        {
            Graphics.Blit(null, destination, postProcessingMaterial);

            RenderTexture tmp = RenderTexture.active;
            RenderTexture.active = projection;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = unitProjection;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = tmp;
        }
    }

}