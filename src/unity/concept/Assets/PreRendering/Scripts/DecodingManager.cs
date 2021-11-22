using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
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

        public delegate void ImageDecodedEvent(string path, Vector3 index, long decodingTime);
        public event ImageDecodedEvent ImageDecoded;

        public ThreadPriority priority;
        private bool cancelRequest;
        private readonly Dictionary<string, Vector3> pending;
        private readonly List<Vector3> decoding;
        private readonly List<Tuple<string, Vector3, long>> decoded;
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
            decoded = new List<Tuple<string, Vector3, long>>();
        }

        public bool DecodeToBuffer(string path, Vector3 index)
        {
            if (!File.Exists(path)) return false;

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            texture.LoadImage(bytes);

            buffer.Add(index);

#if UNITY_EDITOR
            Object.DestroyImmediate(texture);
#else
            Object.Destroy(texture);
#endif
            return true;
        }

        public bool DecodeToBufferAsync(string path, Vector3 index, bool allowPending = true)
        {
            if (IsDecoding(index) || IsDecoded(index)) return false;
            if (decoding.Count < decodingThreads)
            {
                decoding.Add(index);
                buffer.Add(index);

                Task.Run(() =>
                {

                    Debug.Log($"Decoding {path} with index {index} and nativeIndex {buffer[index]}");
                    Thread.CurrentThread.Priority = priority;
                    Decoder.Decode(path, buffer[index], out long decodingTime);

                    Debug.Log(
                        $"Decoded {Path.GetFileName(path)} " +
                        $"in {decodingTime}ms " +
                        $"to position {index}");

                    decoding.Remove(index);
                    decoded.Add(new Tuple<string, Vector3, long>(path, index, decodingTime));
                });
            }
            else
            {
                if (allowPending && !IsPending(index) && pending.Count < maxPending)
                {
                    Debug.Log($"Pending {path}");
                    pending.Add(path, index);
                }
            }

            return true;
        }

        public void Refresh()
        {
            var currentlyDecoded = decoded.ToArray();

            foreach (var item in currentlyDecoded)
            {
                ImageDecodedEvent temp = ImageDecoded;
                if (temp != null)
                {
                    temp.Invoke(item.Item1, item.Item2, item.Item3);
                }
            }

            if (!cancelRequest && decoding.Count < decodingThreads && pending.Count > 0 && pending.Count < maxPending)
                Debug.Log($"{cancelRequest} {decoding.Count} {pending.Count}");
                // DecodePending();
        }

        private void DecodePending()
        {
            var item = pending.ElementAt(0);
            Debug.Log($"Decoding Pending with length {pending.Count}");

            Debug.Log(
                $"Preparing {item.Key} with count {decoding.Count} and " +
                $"{decodingThreads} threads and " +
                $"{(cancelRequest ? "a" : "no")} cancel request");

            Debug.Log($"Dequeuing {item.Key}");
            DecodeToBufferAsync(item.Key, item.Value, false);
            pending.Remove(item.Key);
        }

        public bool IsPending(Vector3 index)
        {
            return pending.Values.Contains(index);
        }

        public bool IsDecoding(Vector3 index)
        {
            return decoding.Contains(index);
        }

        public bool IsDecoded(Vector3 index)
        {
            return buffer.Contains(index);
        }

        public bool IsProcessing(Vector3 index)
        {
            return IsPending(index) || IsDecoding(index);
        }

        public void ClearPending() => pending.Clear();

        public void Release()
        {
            cancelRequest = true;

            Stopwatch timeWaiting = Stopwatch.StartNew();
            for (int i = 0; i < 20; i++)
            {
                if (decoding.Count == 0) break;
                Thread.Sleep(100);
            }
            timeWaiting.Stop();

            if (decoding.Count != 0)
                Debug.LogWarning(
                    $"Some decoding threads are not responding (waited for {timeWaiting.ElapsedMilliseconds}ms).\n" +
                    "Deinitializing the decoder anyways, this may lead to a crash due to a memory acess violation.\n" +
                    $"Waited for threads <{string.Join(",", decoding)}>.");
            Decoder.Deinitialize();
        }
    }
}