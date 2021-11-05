using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PreRendering
{
    /// <summary>
    /// Contains all static helper functions used inside the 'PreRendering' namespace.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Estimates a panorama resolution that should result in no interpolation
        /// (When cropping into a texture by the specified fov using gnomonic projection).
        /// </summary>
        public static Resolution EstimatePanoramaResolution(this Resolution resolution, float fov)
        {
            return EstimatePanoramaResolution(resolution.width, resolution.height, fov);
        }

        public static Resolution EstimatePanoramaResolution(int width, int height, float fov)
        {
            Resolution res = new Resolution
            {
                width = Mathf.RoundToInt(width * 360 / fov),
                height = Mathf.RoundToInt(height * 180 / fov)
            };
            return res;
        }

        /// <summary>
        /// Estimates the resolution a panorama projected using gnomonic projection will have.
        /// </summary>
        public static Resolution EstimateScreenResolution(this Resolution resolution, float fov)
        {
            return EstimateScreenResolution(resolution.width, resolution.height, fov);
        }

        public static Resolution EstimateScreenResolution(int width, int height, float fov)
        {
            Resolution res = new Resolution
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

        /// <summary>
        /// Get a file name for a vector, based on a root directory.
        /// The vector has to be contained inside the vector array this method extends from.
        /// </summary>
        public static string GetFileName(this Vector3[] vectors, string path, Vector3 vector)
        {
            int index = Array.IndexOf(vectors, vector);
            return GetFileName(path, index);
        }

        public static string GetFileName(this List<Vector3> vectors, string path, Vector3 vector)
        {
            int index = vectors.IndexOf(vector);
            return GetFileName(path, index);
        }

        public static string GetFileName(string path, int index)
        {
            return Path.Combine(path, index.ToString().PadLeft(4, '0') + ".png");
        }


        /// <summary>
        /// Loads an image into a texture.
        /// !IMPORTANT! The returned texture will always be in the RGBA32 format.
        /// </summary>
        /// <param name="path">The path of the image file</param>
        public static Texture2D LoadTexture(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            Texture2D reader = new Texture2D(0, 0);
            reader.LoadImage(rawTexture);
            return reader;
        }

        public static int GetSpiralLength(Vector3Int start, Vector3Int end, int step_size = 1)
        {
            return
                (end.x + step_size - start.x) *
                (end.y + step_size - start.y) *
                (end.z + step_size - start.z);
        }

        public static int GetSpiralLength(int range, int step_size = 1)
        {
            float length = Mathf.Pow(2 * range + step_size, 3);
            return Mathf.CeilToInt(length);
        }

        public static int GetSpiralRange(int length, int step_size = 1)
        {
            float range = (Mathf.Pow(length, 1 / 3f) - step_size) / 2f;
            return Mathf.CeilToInt(range);
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

        public static float Normalize(this ushort value)
        {
            return value / (float)0xFFFF;
        }
    }
}