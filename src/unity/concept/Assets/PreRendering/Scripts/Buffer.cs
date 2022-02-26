using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PreRendering
{
    /// <summary>
    /// An abstract class intended to buffer data.
    /// </summary>
    /// <typeparam name="T">The index under which objects can be stored inside the buffer.</typeparam>
    public abstract class Buffer<T> : IEnumerable<T>
    {
        public Dictionary<T, int> reserved;
        public Queue<int> available;
        public Queue<int> copyQueue;

        public Buffer(int cacheSize)
        {
            reserved = new Dictionary<T, int>(cacheSize);
            available = new Queue<int>(cacheSize);
            copyQueue = new Queue<int>();

            for (int i = 0; i < cacheSize; i++) available.Enqueue(i);
        }

        /// <summary>
        /// Acess an element inside the buffer.
        /// </summary>
        /// <param name="index">The index under which the value is stored inside the buffer.</param>
        /// <returns>The native index under which the element can be accessed</returns>
        public int this[T index]
        {
            get
            {
                if (reserved.ContainsKey(index))
                    return reserved[index];
                else return -1;
            }
        }

        public T this[int nativeIndex]
        {
            get
            {
                if (reserved.ContainsValue(nativeIndex))
                    return reserved.First(x => x.Value == nativeIndex).Key;
                else return default;
            }
        }

        public IEnumerator GetEnumerator() { return reserved.Keys.GetEnumerator(); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return reserved.Keys.GetEnumerator(); }

        /// <summary>
        /// Add an element to the buffer
        /// </summary>
        /// <returns>
        /// Wether the element could be added to the buffer.
        /// Returns false if there is already an element stored under the specified index
        /// and returns true otherwise.
        /// </returns>
        public bool Add(T index, int bufferIdx)
        {
            if (reserved.ContainsKey(index)) return false;
            if (available.Count == 0) FreeOne();

            int nativeIndex = available.Dequeue();
            copyQueue.Enqueue(bufferIdx);
            reserved.Add(index, nativeIndex);

            return true;
        }

        /// <summary>
        /// Deallocate the first value of the buffer.
        /// </summary>
        private void FreeOne()
        {
            if (reserved.Count == 0) return;

            T index = this[0];
            reserved.Remove(index);
            available.Enqueue(0);
        }

        /// <summary>
        /// Release an element from the buffer. This won't immediately remove it though,
        /// the area in the buffer just won't be protected anymore.
        /// That means that the element may get overwritten when another element is added to the buffer.
        /// </summary>
        /// <returns>
        /// Wether the element could be released.
        /// Returns false if the buffer doesn't countain the specified index,
        /// returns true otherwise.
        /// </returns>
        public bool Release(T index)
        {
            if (!reserved.ContainsKey(index)) return false;

            int nativeIdx = reserved[index];
            reserved.Remove(index);
            available.Enqueue(nativeIdx);

            return true;
        }

        /// <summary>
        /// Pushes all new frames to the gpu
        /// (To be called in MonoBehaviour.Update)
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < copyQueue.Count; i++)
            {
                int bufferIdx = copyQueue.Dequeue();

                // TODO: Set start index and count
                SetData(bufferIdx, bufferIdx);
            }
        }

        public abstract void SetData(int nativeIdx, int bufferIdx);
    }
}
