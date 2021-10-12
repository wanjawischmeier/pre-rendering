using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreRendering
{
    public abstract class Buffer<T1, T2> : IEnumerable<T1>
    {
        Dictionary<T1, int> lookup;
        Queue<int> queue;

        public Buffer(int cacheSize)
        {
            lookup = new Dictionary<T1, int>(cacheSize);
            queue = new Queue<int>();
        }

        public int this[T1 index]
        {
            get
            {
                return lookup[index];
            }
        }

        public IEnumerator GetEnumerator() { return GetEnumerator(); }

        IEnumerator<T1> IEnumerable<T1>.GetEnumerator()
        {
            return new List<T1>().GetEnumerator();
        }

        public bool Add(T1 key, T2 value)
        {
            if (lookup.ContainsKey(key)) return false;

            int index = queue.Dequeue();
            AddAtIndex(index, value);
            queue.Enqueue(index);
            return true;
        }
        
        public bool Remove(T1 key)
        {
            return false;
        }

        public abstract void AddAtIndex(int index, T2 value);
    }

    public class FrameBuffer : Buffer<Vector3, Texture2D>
    {
        public Texture2DArray textures;

        public FrameBuffer(int width, int height, int cacheSize, TextureFormat format = TextureFormat.RGBA32) : base(cacheSize)
        {
            textures = new Texture2DArray(width, height, cacheSize, format, 1, false);
        }

        ~FrameBuffer()
        {
            Object.Destroy(textures);
        }

        public override void AddAtIndex(int index, Texture2D value)
        {
            Graphics.CopyTexture(textures, index, value, 0);
        }
    }
}
