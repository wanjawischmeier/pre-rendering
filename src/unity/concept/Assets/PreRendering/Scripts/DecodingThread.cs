using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.IO;
using System.Threading;

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

        public bool isCanceled = false;

        Dictionary<string, Vector3> pending;
        List<Vector3> decoding;

        readonly RawTexture.Buffer buffer;
        readonly CancellationToken cancellationToken;
        readonly int decodingThreads, maxPending;

        public DecodingThread(RawTexture.Buffer buffer, CancellationToken cancellationToken, int decodingThreads, int maxPending)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;
            this.maxPending = maxPending;
            this.cancellationToken = cancellationToken;

            pending = new Dictionary<string, Vector3>();
            decoding = new List<Vector3>();

            Decoder.ImageDecoded += OnImageDecoded;
        }

        public bool DecodeToBuffer(string path, Vector3 value)
        {
            if (!File.Exists(path)) return false;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            texture.LoadImage(bytes);

            buffer.Add(value);

#if UNITY_EDITOR
            Object.DestroyImmediate(texture);
#else
            Object.Destroy(texture);
#endif
            return true;
        }
        
        public bool DecodeToBufferAsync(string path, Vector3 value, bool allowPending = true)
        {
            if (IsDecoding(value)) return false;
            if (decoding.Count < decodingThreads)
            {
                Decoder.Decode(path, 0);
                decoding.Add(value);
            }
            else
            {
                if (allowPending && !IsPending(path) && pending.Count < maxPending)
                {
                    pending.Add(path, value);
                }
            }

            return true;
        }

        public void DecodePending()
        {
            Dictionary<string, Vector3> temp = new Dictionary<string, Vector3>(pending);

            foreach (var item in temp)
            {
                if (decoding.Count >= decodingThreads) break;
                DecodeToBufferAsync(item.Key, item.Value, false);
                pending.Remove(item.Key);
            }
        }

        private void OnImageDecoded(string path, int index, int threadId, long decodingTime)
        {
            // Prevents crash due to memory acess violation
            // (if some stuff has already been deallocated)
            if (decoding.Count == 0) return;

            Vector3 vector = Vector3.zero;
            buffer.Add(index);

            decoding.Remove(vector);

            if (cancellationToken.IsCancellationRequested)
            {
                isCanceled = true;
                return;
            }

            DecodePending();
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
    }
}