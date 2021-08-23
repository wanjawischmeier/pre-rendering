using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Contains the Buffer<T> base class and variations of it. Used for storing data in a buffer.
/// </summary>
namespace Buffering
{
    /// <summary>
    /// A class for storing data in a buffer. 
    /// Removes the oldest entry when pushing a new one
    /// </summary>
    /// <typeparam name="Object-type"></typeparam>
    class Buffer<T>
    {
        Dictionary<ulong, T> frameBuffer;
        T nullval;
        ulong bufferSize;
        List<ulong> lastUsed;

        public Buffer(ulong _bufferSize, T _nullval)
        {
            bufferSize = _bufferSize;
            nullval = _nullval;
            frameBuffer = new Dictionary<ulong, T>();
            lastUsed = new List<ulong>();
            
            for (ulong i = 0; i < bufferSize; i++)
            {
                frameBuffer.Add(i, nullval);
                lastUsed.Add(i);
            }
        }

        public T Get(ulong key)
        {
            T value;

            if (frameBuffer.TryGetValue(key, out value)) return value;
            else return nullval;
        }

        public void Push(ulong key, T value)
        {
            if (frameBuffer.ContainsKey(key))
            {
                Debug.Log(string.Format("Key {0} already exists", key.ToString()));
                return;
            }

            ulong old = lastUsed[0];        // Get oldest value

            lastUsed.RemoveAt(0);           // Remove oldest value
            lastUsed.Add(key);              // Add new key to the end
            // Debug.Log(value);
            frameBuffer.Remove(old);        // Remove old value

            Debug.Log(
                string.Format(
                    "Pushing key {0}\n" +
                    "with value {1}\n" +
                    "\n" +
                    "Operation:\n" +
                    "Old value key: {2}\n" +
                    "Removing key {2} from dictionary\n" +
                    "Appending key {0}", 
                    key, value, old
                )
            );
            try
            {
                frameBuffer.Add(key, value);// Append new value
            }
            catch (System.ArgumentException) 
            {
                Debug.Log(string.Format("Key {0} already exists", key.ToString()));
            }
        }

        public void Log()
        {
            foreach (int index in lastUsed.ToArray())
            {
                ulong key = lastUsed[index -1];
                Debug.Log(string.Format("Key {0} at pos {1}: {2}", key.ToString(), index.ToString(), frameBuffer[key].ToString()));
            }
        }
    }


    /// <summary>
    /// Based on the <Buffer> class, specialized to buffer textures
    /// </summary>
    [System.Serializable]
    public class FrameBufferV1
    {
        Buffer<Texture> buffer;
        Texture empty;
        Func<ulong, Texture> decode;

        public FrameBufferV1(ulong _bufferSize, int width, int height, Func<ulong, Texture> decodeFrame)
        {
            empty = new Texture2D(width, height);
            buffer = new Buffer<Texture>(_bufferSize, empty);
            decode = decodeFrame;
        }

        public Texture Get(ulong frameIdx)
        {
            Texture texture = buffer.Get(frameIdx);
            
            if (texture == empty)
            {
                texture = decode(frameIdx);
                buffer.Push(frameIdx, texture);
            }
            
            return texture;
        }

        public void Push(ulong frameIdx, Texture texture)
        {
            buffer.Push(frameIdx, texture);
        }
    }

    public class Test
    {
        void Main()
        {
            FrameBufferV1 frameBuffer = new FrameBufferV1(10, 100, 100, null);
        }
    }
}