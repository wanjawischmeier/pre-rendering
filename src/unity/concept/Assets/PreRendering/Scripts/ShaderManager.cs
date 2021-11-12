using UnityEngine;

namespace PreRendering
{
    public class ShaderManager
    {
        /// <summary>
        /// The position inside the compute shader.
        /// </summary>
        public Vector3 Position
        {
            get { return projectionMaterial.GetVector("Position"); }
            set { projectionMaterial.SetVector("Position", value); }
        }

        /// <summary>
        /// Sets the position compute buffer.
        /// </summary>
        public Vector3 PositionOffset
        {
            get { return projectionMaterial.GetVector("PositionOffset"); }
            set { projectionMaterial.SetVector("PositionOffset", value); }
        }

        /// <summary>
        /// The rotation inside the post processing shader.
        /// The value will be converted to radians before passed to the gpu.
        /// </summary>
        public Vector3 Rotation
        {
            get { return postProcessingMaterial.GetVector("Rotation"); }
            set { postProcessingMaterial.SetVector("Rotation", value * Mathf.Deg2Rad); }
        }

        /// <summary>
        /// The field of view used for the gnomonic projection inside the post processing shader.
        /// </summary>
        public float Fov
        {
            get { return postProcessingMaterial.GetFloat("FOV"); }
            set { postProcessingMaterial.SetFloat("FOV", value * Mathf.Deg2Rad); }
        }

        public float DOFIntensity
        {
            get { return postProcessingMaterial.GetFloat("DOF_INTENSITY"); }
            set { postProcessingMaterial.SetFloat("DOF_INTENSITY", value); }
        }

        public ShaderDebugMode ShaderDebug
        {
            get { return (ShaderDebugMode)postProcessingMaterial.GetInt("Debug"); }
            set { postProcessingMaterial.SetInt("Debug", (int)value); }
        }

        public Color Mist
        {
            get { return postProcessingMaterial.GetColor("MIST_COL"); }
            set { postProcessingMaterial.SetColor("MIST_COL", value); }
        }

        public float MistFalloff
        {
            get { return postProcessingMaterial.GetFloat("MIST_FALLOFF"); }
            set { postProcessingMaterial.SetFloat("MIST_FALLOFF", value); }
        }

        public float MistOffset
        {
            get { return postProcessingMaterial.GetFloat("MIST_OFFSET"); }
            set { postProcessingMaterial.SetFloat("MIST_OFFSET", value); }
        }

        private readonly Shader projectionShader;
        private readonly Shader postProcessingShader;
        private readonly Material projectionMaterial;
        private readonly Material postProcessingMaterial;
        private readonly RenderTexture projection;
        private readonly Map map;

        public enum ShaderDebugMode
        {
            Disabled,
            TextureCoordinates,
            ProjectedCoordinates,
            Normals,
            DepthOfField,
            DepthBuffer
        }

        public ShaderManager(
            ComputeBuffer buffer, Resolution projectionResolution,
            Map map, int layerDepth)
        {
            this.map = map;

            projectionShader = Shader.Find("PreRendering/Projection");
            postProcessingShader = Shader.Find("PreRendering/PostProcessing");

            projectionMaterial = new Material(projectionShader);
            postProcessingMaterial = new Material(postProcessingShader);

            projection = new RenderTexture(
                projectionResolution.width, projectionResolution.height, 1, RenderTextureFormat.ARGB64)
            { enableRandomWrite = true };
            projection.Create();

            Shader.SetGlobalFloat("PI", Mathf.PI);
            Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
            Shader.SetGlobalFloat("NCLIP", map.nClip);
            Shader.SetGlobalFloat("FCLIP", map.fClip);
            Shader.SetGlobalInt("MX_IDX", layerDepth);
            Shader.SetGlobalVector("InputBufferResolution", new Vector2(map.resolution.width, map.resolution.height));
            Shader.SetGlobalVector("ProjectedRes", new Vector2(projection.width, projection.height));
            Shader.SetGlobalBuffer("InputBuffer", buffer);
            Shader.SetGlobalTexture("_Projection", projection);
        }

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
            Shader.SetGlobalVector("ProjectionRes", new Vector2(width, height));
            projectionMaterial.SetInt("IMG_IDX", index);

            Graphics.Blit(null, projection, projectionMaterial);
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
            RenderTexture.active = tmp;
        }
    }

}