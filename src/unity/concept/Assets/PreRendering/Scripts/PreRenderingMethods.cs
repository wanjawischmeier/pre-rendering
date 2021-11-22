using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PreRendering
{
    public partial class PreRenderer
    {
        private void SetShaderConstants()
        {
            ShaderManager.SetValues(
                new ShaderManager.Property()
                {
                    name = "MX_IDX",
                    value = cacheSize
                },
                new ShaderManager.Property()
                {
                    name = "PI",
                    value = Mathf.PI
                },
                new ShaderManager.Property()
                {
                    name = "PI2",
                    value = Mathf.PI * 2
                },
                new ShaderManager.Property()
                {
                    name = "NCLIP",
                    value = Map.nClip
                },
                new ShaderManager.Property()
                {
                    name = "FCLIP",
                    value = Map.fClip
                },
                new ShaderManager.Property()
                {
                    name = "InputBufferResolution",
                    value = Map.resolution.ToVector()
                },
                new ShaderManager.Property()
                {
                    name = "ProjectedResolution",
                    value = projectionResolution.ToVector()
                },
                new ShaderManager.Property()
                {
                    name = "InputBuffer",
                    value = buffer.computeBuffer
                });
        }

        private void SetShaderValues()
        {
            ShaderManager.SetValues(
                new ShaderManager.Property()
                {
                    name = "FOV",
                    value = mainCamera.fieldOfView * Mathf.Deg2Rad,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Debug",
                    value = shaderDebugSelection,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "DOF_INTENSITY",
                    value = depthOfField,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_FALLOFF",
                    value = mistFalloff,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_OFFSET",
                    value = mistOffset,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "MIST_COL",
                    value = (Vector4)mist,
                    material = shaderManager.postProcessingMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Position",
                    value = (Vector4)transform.position,
                    material = shaderManager.projectionMaterial
                },
                new ShaderManager.Property()
                {
                    name = "Rotation",
                    value = (Vector4)transform.eulerAngles * Mathf.Deg2Rad,
                    material = shaderManager.postProcessingMaterial
                });
        }
    }

    public static partial class PreRenderingUtility
    {
        /// <summary>
        /// Estimates the resolution a panorama projected using gnomonic projection will have.
        /// </summary>
        public static Resolution EstimateScreenResolution(this Resolution resolution, float fov)
        {
            return EstimateScreenResolution(resolution.width, resolution.height, fov);
        }

        public static Resolution EstimateScreenResolution(int width, int height, float fov)
        {
            var res = new Resolution
            {
                width = Mathf.RoundToInt(width * fov / 360),
                height = Mathf.RoundToInt(height * fov / 180)
            };
            return res;
        }

        /// <summary>
        /// Get the vectors that have the smallest distance to the specified target position.
        /// These vectors originate from the 'position' vector and are ordered in an outwards spiraling pattern.
        /// </summary>
        /// <param name="amount">The desired length of the returned array.</param>
        public static Vector3[] GetClosest(this Vector3[] vectors, Vector3 position, int amount)
        {
            return vectors
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(amount)
                .ToArray();
        }

        /// <summary>
        /// Get the vectors that have the smallest distance to the specified target position.
        /// These vectors originate from the 'position' vector and are ordered in an outwards spiraling pattern.
        /// </summary>
        /// <param name="amount">The desired length of the returned array.</param>
        public static Vector3[] PredictClosest(this Vector3[] vectors, Vector3 oldPosition, Vector3 newPosition, int amount, float blend = 0.5f, float predictionDistance = 2)
        {
            return vectors
                .OrderBy(x =>
                {
                    Vector3 P = oldPosition + predictionDistance * (newPosition - oldPosition);
                    return (1 - blend) * Vector3.Distance(newPosition, x) + blend * Vector3.Distance(P, x);
                })
                .Take(amount)
                .ToArray();
        }

        public static Resolution Multiply(this Resolution resolution, float value)
        {
            return new Resolution()
            {
                width = Mathf.RoundToInt(resolution.width * value),
                height = Mathf.RoundToInt(resolution.height * value)
            };
        }

        public static bool ContainsAny<T>(this IEnumerable<T> enumerable1, IEnumerable<T> enumerable2)
        {
            foreach (var item in enumerable1)
            {
                if (enumerable2.Contains(item)) return true;
            }

            return false;
        }

        public static Vector4 ToVector(this Resolution resolution)
        {
            return new Vector4(resolution.width, resolution.height);
        }
    }
}