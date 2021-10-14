using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PreRendering
{
    /// <summary>
    /// An abstract class intended to buffer data.
    /// </summary>
    /// <typeparam name="T1">The key under which objects can be stored inside the buffer.</typeparam>
    /// <typeparam name="T2">The object type to be stored inside the buffer.</typeparam>
    public abstract class Buffer<T1, T2> : IEnumerable<T1>
    {
        public Dictionary<T1, int> reserved;
        public Queue<int> available;

        public Buffer(int cacheSize)
        {
            reserved = new Dictionary<T1, int>(cacheSize);
            available = new Queue<int>();

            for (int i = 0; i < cacheSize; i++) available.Enqueue(i);
        }

        /// <summary>
        /// Acess an element inside the buffer.
        /// </summary>
        /// <param name="index">The key under which the value is stored inside the buffer.</param>
        /// <returns>The index under which the element can be accessed</returns>
        public int this[T1 index]
        {
            get
            {
                if (reserved.ContainsKey(index))
                    return reserved[index];
                else return -1;
            }
        }

        public IEnumerator GetEnumerator() { return reserved.Keys.GetEnumerator(); }

        IEnumerator<T1> IEnumerable<T1>.GetEnumerator() { return reserved.Keys.GetEnumerator(); }

        /// <summary>
        /// Add an element to the buffer
        /// </summary>
        /// <returns>
        /// Wether the element could be added to the buffer.
        /// Returns false if there is already an element stored under the specified key
        /// and returns true otherwise.
        /// </returns>
        public bool Add(T1 key, T2 value)
        {
            if (reserved.ContainsKey(key) || value == null) return false;

            if (available.Count == 0)
            {
                T1 anyKey = Enumerable.ToArray(reserved.Keys)[0];
                Release(anyKey);
            }

            int index = available.Dequeue();
            Add(index, value);
            reserved.Add(key, index);

            return true;
        }

        public abstract void Add(int index, T2 value);

        /// <summary>
        /// Release an element from the buffer. This won't immediately remove it though,
        /// the area in the buffer just won't be protected anymore.
        /// That means that the element may get overwritten when another element is added to the buffer.
        /// </summary>
        /// <returns>
        /// Wether the element could be released.
        /// Returns false if the buffer doesn't countain the specified key,
        /// returns true otherwise.
        /// </returns>
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
