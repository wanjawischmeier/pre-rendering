using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a <FrameBuffer>.
    /// </summary>
    public class DecodingThread
    {
        public Dictionary<string, Vector3> pending;
        public Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>> decoding;

        readonly TextureBuffer buffer;
        readonly int decodingThreads;

        public DecodingThread(TextureBuffer buffer, int decodingThreads)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;

            pending = new Dictionary<string, Vector3>();
            decoding = new Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>>();
        }

        ~DecodingThread() => Release();

        public void Release()
        {
            foreach (var item in decoding)
                item.Value.Item2.Abort();
            decoding.Clear();
        }

        public bool IsPending(string path)
        {
            return pending.Keys.Contains(path);
        }

        public bool IsDecoding(Vector3 vector)
        {
            return decoding.Values.Select(
                (Tuple<Vector3, UnityWebRequest> value) =>
                {
                    return value.Item1;
                })
                .Contains(vector);
        }

        public bool IsProcessing(string path, Vector3 value)
        {
            return IsPending(path) || IsDecoding(value);
        }

        public bool DecodeToBuffer(string path, Vector3 value)
        {
            if (IsDecoding(value)) return false;
            if (decoding.Count >= decodingThreads && !IsPending(path))
                pending.Add(path, value);
            else
            {
                // Based on: https://stackoverflow.com/a/53770838/13215204
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
                var asyncOp = www.SendWebRequest();
                decoding.Add(asyncOp, new Tuple<Vector3, UnityWebRequest>(value, www));
                asyncOp.completed += OnImageDecoded;
            }

            return true;
        }

        void DecodePending()
        {
            Dictionary<string, Vector3> temp = new Dictionary<string, Vector3>(pending);

            foreach (var item in temp)
            {
                if (decoding.Count >= decodingThreads) break;
                DecodeToBuffer(item.Key, item.Value);
                pending.Remove(item.Key);
            }
        }

        void OnImageDecoded(AsyncOperation obj)
        {
            // Prevents crash due to memory acess violation
            // (if some stuff has already been deallocated)
            if (decoding.Count == 0) return;

            Tuple<Vector3, UnityWebRequest> data = decoding[obj];

            if (data.Item2.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(data.Item2);
                buffer.Add(data.Item1, texture);
#if UNITY_EDITOR
                Object.DestroyImmediate(texture);
#else
                Object.Destroy(texture);
#endif
            }

            decoding.Remove(obj);
            DecodePending();
        }
    }
}