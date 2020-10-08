using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PreRendering
{
    public static class FrameBuffer
    {
        static Dictionary<long, Texture> buffer = new Dictionary<long, Texture>();

        /// <summary>
        /// Get a texture from the buffer by a given index
        /// </summary>
        public static Texture Get(long frameIdx)
        {
            if (buffer.ContainsKey(frameIdx)) return buffer[frameIdx];  // Check if buffer contains the key and if so, return it
            else return null;
        }

        /// <summary>
        /// Push a texture to the buffer at a given index
        /// </summary>
        public static bool Push(long frameIdx, Texture texture)
        {
            if (buffer.ContainsKey(frameIdx)) return false;             // Check if buffer already contains key

            buffer.Add(frameIdx, texture);                              // Add new texture
            buffer.Remove(                                              // Remove old texture
                buffer.ElementAt(0).Key                                 // (At index 0) / Remove first element
            );

            return true;
        }
    }
}