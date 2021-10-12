using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace PreRendering
{
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

        public void DecodeToBufferAsync(string path, Vector3 key)
        {
            if (decoding.Count > decodingThreads)
            {
                pending.Add(path, key);
                return;
            }

            // Based on: https://stackoverflow.com/a/53770838/13215204
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(path);
            var asyncOp = www.SendWebRequest();

            decoding.Add(asyncOp, new Tuple<Vector3, UnityWebRequest>(key, www));
            asyncOp.completed += OnImageDecoded;
        }

        void OnImageDecoded(AsyncOperation obj)
        {
            Tuple<Vector3, UnityWebRequest> data = decoding[obj];

            if (data.Item2.result == UnityWebRequest.Result.Success)
                buffer.Add(data.Item1, DownloadHandlerTexture.GetContent(data.Item2));

            decoding.Remove(obj);
        }
    }
}