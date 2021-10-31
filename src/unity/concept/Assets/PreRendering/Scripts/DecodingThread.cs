using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Threading;
using System.IO;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a <FrameBuffer>.
    /// </summary>
    public class DecodingThread
    {
        public int Pending { get { return pending.Count; } }
        public int Decoding { get { return decoding.Count; } }

        Dictionary<string, Vector3> pending;
        Dictionary<UnityWebRequestAsyncOperation, Vector3> decoding;

        readonly TextureBuffer buffer;
        readonly int decodingThreads, maxPending;

        public DecodingThread(TextureBuffer buffer, int decodingThreads, int maxPending)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;
            this.maxPending = maxPending;

            pending = new Dictionary<string, Vector3>();
            decoding = new Dictionary<UnityWebRequestAsyncOperation, Vector3>();
        }

        ~DecodingThread() => Release();

        public void Release()
        {
            foreach (var item in decoding)
                item.Key.webRequest.Abort();
            decoding.Clear();
        }

        public bool IsPending(string path)
        {
            return pending.Keys.Contains(path);
        }

        public bool IsDecoding(Vector3 vector)
        {
            return decoding.Values.Contains(vector);
        }

        public bool IsProcessing(string path, Vector3 value)
        {
            return IsPending(path) || IsDecoding(value);
        }

        public void ClearPending() => pending.Clear();

        public bool DecodeToBuffer(string path, Vector3 value)
        {
            if (!File.Exists(path)) return false;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            texture.LoadImage(bytes);

            buffer.Add(value, texture);

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
                // Based on: https://stackoverflow.com/a/53770838/13215204
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
                var asyncOp = www.SendWebRequest();
                decoding.Add(asyncOp, value);
                asyncOp.completed += OnImageDecoded;
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

        void OnImageDecoded(AsyncOperation obj)
        {
            UnityWebRequestAsyncOperation asyncOp = (UnityWebRequestAsyncOperation)obj;

            // Prevents crash due to memory acess violation
            // (if some stuff has already been deallocated)
            if (decoding.Count == 0) return;

            Vector3 vector = decoding[asyncOp];

            if (asyncOp.webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(asyncOp.webRequest);
                buffer.Add(vector, texture);
#if UNITY_EDITOR
                Object.DestroyImmediate(texture);
#else
                Object.Destroy(texture);
#endif
            }

            decoding.Remove(asyncOp);
            DecodePending();
        }
    }
}