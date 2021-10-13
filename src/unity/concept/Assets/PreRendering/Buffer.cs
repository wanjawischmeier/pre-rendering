using System.Collections;
using System.Collections.Generic;

namespace PreRendering
{
    public abstract class Buffer<T1, T2> : IEnumerable<T1>
    {
        Dictionary<T1, int> reserved;
        Queue<int> available;

        public Buffer(int cacheSize)
        {
            reserved = new Dictionary<T1, int>(cacheSize);
            available = new Queue<int>();

            for (int i = 0; i < cacheSize; i++) available.Enqueue(i);
        }

        public int this[T1 index]
        {
            get { return reserved[index]; }
        }

        public IEnumerator GetEnumerator() { return GetEnumerator(); }

        IEnumerator<T1> IEnumerable<T1>.GetEnumerator() { return reserved.Keys.GetEnumerator(); }

        public bool Add(T1 key, T2 value)
        {
            if (reserved.ContainsKey(key) || available.Count < 1) return false;

            int index = available.Dequeue();
            Add(index, value);
            reserved.Add(key, index);

            return true;
        }

        public abstract void Add(int index, T2 value);

        public bool Release(T1 key)
        {
            if (!reserved.ContainsKey(key)) return false;

            int index = reserved[key];
            available.Enqueue(index);
            reserved.Remove(key);
            Release(index);

            return true;
        }

        public virtual void Release(int index) { }
    }
}
