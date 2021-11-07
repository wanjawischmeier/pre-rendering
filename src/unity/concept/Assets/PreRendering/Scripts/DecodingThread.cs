using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ThreadPriority = System.Threading.ThreadPriority;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a buffer.
    /// </summary>
    public class DecodingThread
    {
        public int Pending { get { return pending.Count; } }
        public int Decoding { get { return decoding.Count; } }

        bool cancelRequest;

        Dictionary<string, Vector3> pending;
        List<Vector3> decoding;

        readonly RawTexture.Buffer buffer;
        readonly ThreadPriority priority;
        readonly int decodingThreads, maxPending;

        public DecodingThread(RawTexture.Buffer buffer, int decodingThreads, int maxPending, ThreadPriority priority = ThreadPriority.Lowest)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;
            this.maxPending = maxPending;
            this.priority = priority;

            pending = new Dictionary<string, Vector3>();
            decoding = new List<Vector3>();

            Decoder.ImageDecoded += OnImageDecoded;
        }

        ~DecodingThread() => Release();

        public bool DecodeToBuffer(string path, Vector3 key)
        {
            if (!File.Exists(path)) return false;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            texture.LoadImage(bytes);

            buffer.Add(key);

#if UNITY_EDITOR
            Object.DestroyImmediate(texture);
#else
            Object.Destroy(texture);
#endif
            return true;
        }
        
        public bool DecodeToBufferAsync(string path, Vector3 key, bool allowPending = true)
        {
            if (IsDecoding(key)) return false;
            if (decoding.Count < decodingThreads)
            {
                Task.Run(() =>
                {
                    Thread.CurrentThread.Priority = priority;
                    Decoder.Decode(path, 0);
                });
                decoding.Add(key);
            }
            else
            {
                if (allowPending && !IsPending(path) && pending.Count < maxPending)
                {
                    pending.Add(path, key);
                }
            }

            return true;
        }

        public void DecodePending()
        {
            Dictionary<string, Vector3> temp = new Dictionary<string, Vector3>(pending);

            foreach (var item in temp)
            {
                if (decoding.Count >= decodingThreads || cancelRequest) break;
                DecodeToBufferAsync(item.Key, item.Value, false);
                pending.Remove(item.Key);
            }
        }

        private void OnImageDecoded(string path, int index, int threadId, long decodingTime)
        {
            // Prevents crash due to memory acess violation
            // (if some stuff has already been deallocated)
            if (decoding.Count == 0) return;
            buffer.Add(index);

            Debug.Log($"Decoded {Path.GetFileName(path)} in {decodingTime}ms to position {index}\t\t(ThreadID:{threadId})");

            decoding.Remove(buffer.ElementAt(index));
            Debug.Log(decoding.Count);
            if (!cancelRequest) DecodePending();
        }

        public bool IsPending(string path)
        {
            return pending.Keys.Contains(path);
        }

        public bool IsDecoding(Vector3 vector)
        {
            return decoding.Contains(vector);
        }

        public bool IsProcessing(string path, Vector3 value)
        {
            return IsPending(path) || IsDecoding(value);
        }

        public void ClearPending() => pending.Clear();

        public void Release()
        {
            cancelRequest = true;
            while (true)
            {
                if (decoding.Count == 0) break;
                else
                {
                    foreach (Vector3 key in decoding)
                        Debug.Log($"Waiting for key {key}");
                }
                Thread.Sleep(10);
            }
            Decoder.Deinitialize();
        }
    }
}