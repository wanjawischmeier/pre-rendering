using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using ThreadPriority = System.Threading.ThreadPriority;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a buffer.
    /// </summary>
    public class DecodingManager
    {
        public int Pending { get { return pending.Count; } }
        public int Decoding { get { return decoding.Count; } }

        public ThreadPriority priority;
        private bool cancelRequest;
        private readonly Dictionary<string, Vector3> pending;
        private readonly List<Vector3> decoding;
        private readonly RawTexture.Buffer buffer;
        private readonly int decodingThreads, maxPending;

        public DecodingManager(RawTexture.Buffer buffer, int decodingThreads, int maxPending)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;
            this.maxPending = maxPending;

            priority = ThreadPriority.Lowest;
            pending = new Dictionary<string, Vector3>();
            decoding = new List<Vector3>();

            Decoder.ImageDecoded += OnImageDecoded;
        }

        public bool DecodeToBuffer(string path, Vector3 key)
        {
            if (!File.Exists(path)) return false;

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
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
                decoding.Add(key);
                buffer.Add(key);
                int index = buffer[key];

                Task.Run(() =>
                {
                    Debug.Log($"Decoding {path}");
                    Thread.CurrentThread.Priority = priority;
                    Decoder.Decode(path, index);
                });
            }
            else
            {
                if (allowPending && !IsPending(path) && pending.Count < maxPending)
                {
                    Debug.Log($"Pending {path}");
                    pending.Add(path, key);
                }
            }

            return true;
        }

        public void DecodePending()
        {
            var item = pending.ElementAt(0);
            Debug.Log($"Decoding Pending with length {pending.Count}");

            Debug.Log($"Checking {item.Key} with count {decoding.Count} and {decodingThreads} threads and {(cancelRequest ? "a" : "no")} cancel request");
            if (decoding.Count >= decodingThreads || cancelRequest) return;

            Debug.Log($"Dequeueing {item.Key}");
            DecodeToBufferAsync(item.Key, item.Value, false);
            pending.Remove(item.Key);
        }

        private void OnImageDecoded(string path, int index, int threadId, long decodingTime)
        {
            Debug.Log(
                $"Decoded {Path.GetFileName(path)} " +
                $"in {decodingTime}ms " +
                $"to position {index}\t\t" +
                $"(ThreadID:{threadId})");

            decoding.Remove(buffer.ElementAt(index));
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
            for (int i = 0; i < 20; i++)
            {
                if (decoding.Count == 0) break;
                else
                    foreach (Vector3 key in decoding)
                        Debug.Log($"Waiting for key {key}");

                Thread.Sleep(100);
            }
            Decoder.Deinitialize();
        }
    }
}