using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a <FrameBuffer>.
    /// </summary>
    public class Decoder
    {
        public Dictionary<string, Vector3> pending;
        public Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>> decoding;

        readonly FrameBuffer buffer;
        readonly int decodingThreads;

        public Decoder(FrameBuffer buffer, int decodingThreads)
        {
            this.buffer = buffer;
            this.decodingThreads = decodingThreads;

            pending = new Dictionary<string, Vector3>();
            decoding = new Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>>();
        }

        [Obsolete]
        public Texture2D Decode(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            Texture2D reader = new Texture2D(0, 0);
            reader.LoadImage(rawTexture);
            return reader;
        }

        [Obsolete]
        public void DecodeToBuffer(string path, Vector3 key)
        {
            Texture2D texture = Decode(path);
            buffer.Add(key, texture);
        }

        public bool IsPending(Vector3 vector)
        {
            return pending.Values.Contains(vector);
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

        public bool IsProcessing(Vector3 value)
        {
            return IsPending(value) || IsDecoding(value);
        }

        public bool DecodeToBufferAsync(string path, Vector3 value)
        {
            if (IsProcessing(value)) return false;
            if (decoding.Count >= decodingThreads) pending.Add(path, value);
            else
            {
                // Based on: https://stackoverflow.com/a/53770838/13215204
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
                var asyncOp = www.SendWebRequest();
                Debug.Log("Decoding at " + path);
                decoding.Add(asyncOp, new Tuple<Vector3, UnityWebRequest>(value, www));
                asyncOp.completed += OnImageDecoded;
            }

            return true;
        }

        void DecodePending()
        {
            Dictionary<string, Vector3> temp = new Dictionary<string, Vector3>(pending);
            
            foreach (KeyValuePair<string, Vector3> item in temp)
            {
                if (decoding.Count >= decodingThreads) break;
                DecodeToBufferAsync(item.Key, item.Value);
                pending.Remove(item.Key);
            }
        }

        void OnImageDecoded(AsyncOperation obj)
        {
            Tuple<Vector3, UnityWebRequest> data = decoding[obj];

            if (data.Item2.result == UnityWebRequest.Result.Success)
                buffer.Add(data.Item1, DownloadHandlerTexture.GetContent(data.Item2));
            Debug.Log(string.Format("Decoded texture at ({0}, {1}, {2}) with result {3}", data.Item1.x, data.Item1.y, data.Item1.z, data.Item2.result.ToString()));
            decoding.Remove(obj);
            DecodePending();
        }
    }
}