using System;
using System.Collections.Generic;
using UnityEngine;

namespace PreRendering
{
    public class ShaderManager
    {
        public struct Property
        {
            public string name;
            public object value;
            public Material material;
        }

        // Based on https://stackoverflow.com/a/4478535/13215204
        private static readonly Dictionary<Type, Func<Material, string, object>> getValue = new Dictionary<Type, Func<Material, string, object>>()
            {
                {
                    typeof(int), new Func<Material, string, object>(
                        (material, name) =>
                        {
                            if (material == null) return Shader.GetGlobalInt(name);
                            else return material.GetInt(name);
                        })
                },
                {
                    typeof(float), new Func<Material, string, object>(
                        (material, name) =>
                        {
                            if (material == null) return Shader.GetGlobalFloat(name);
                            else return material.GetFloat(name);
                        })
                },
                {
                    typeof(Vector4), new Func<Material, string, object>(
                        (material, name) =>
                        {
                            if (material == null) return Shader.GetGlobalVector(name);
                            else return material.GetVector(name);
                        })
                }
            };
        private static readonly Dictionary<Type, Action<Material, string, object>> setValue = new Dictionary<Type, Action<Material, string, object>>()
            {
                {
                    typeof(int), new Action<Material, string, object>(
                        (material, name, value) =>
                        {
                            if (material == null) Shader.SetGlobalInt(name, (int)value);
                            else material.SetInt(name, (int)value);
                        })
                },
                {
                    typeof(float), new Action<Material, string, object>(
                        (material, name, value) =>
                        {
                            if (material == null) Shader.SetGlobalFloat(name, (float)value);
                            else material.SetFloat(name, (float)value);
                        })
                },
                {
                    typeof(Vector4), new Action<Material, string, object>(
                        (material, name, value) =>
                        {
                            if (material == null) Shader.SetGlobalVector(name, (Vector4)value);
                            else material.SetVector(name, (Vector4)value);
                        })
                },
                {
                    typeof(ComputeBuffer), new Action<Material, string, object>(
                        (material, name, value) =>
                        {
                            if (material == null) Shader.SetGlobalBuffer(name, (ComputeBuffer)value);
                            else material.SetBuffer(name, (ComputeBuffer)value);
                        })
                }
            };

        /// <summary>
        /// Set an arbitrary amount of values on the gpu.
        /// Each pair has to have a name, a material and the actual value.
        /// If the material is set to null, the variable will be set globally.
        /// </summary>
        public static void SetValues(params Property[] values)
        {
            foreach (var item in values)
                setValue[item.value.GetType()](item.material, item.name, item.value);
        }

        public object this[string name, Type type, Material material]
        {
            get
            {
                return getValue[type](material, name);
            }
            set
            {
                setValue[type](material, name, value);
            }
        }

        private readonly Shader projectionShader;
        private readonly Shader postProcessingShader;
        public readonly Material projectionMaterial;
        public readonly Material postProcessingMaterial;
        private readonly RenderTexture projection;

        public enum ShaderDebugMode
        {
            Disabled,
            TextureCoordinates,
            ProjectedCoordinates,
            Normals,
            DepthOfField,
            DepthBuffer
        }

        public ShaderManager(Resolution projectedResolution)
        {
            projectionShader = Shader.Find("PreRendering/Projection");
            postProcessingShader = Shader.Find("PreRendering/PostProcessing");

            projectionMaterial = new Material(projectionShader);
            postProcessingMaterial = new Material(postProcessingShader);

            projection = new RenderTexture(
                Mathf.RoundToInt(projectedResolution.width),
                Mathf.RoundToInt(projectedResolution.height),
                1, RenderTextureFormat.ARGB64)
            { enableRandomWrite = true };
            projection.Create();

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