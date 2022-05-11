using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    /// <summary>
    /// A class that takes care of decoding images.
    /// This can be done asynchronously, (on multiple threads)
    /// and directly into a buffer.
    /// </summary>
    public class DecodingManager
    {
        #region Variables

        public ThreadPriority priority;
        public readonly DecodingBuffer buffer;

        private readonly Decoder[] decoders;
        private readonly List<long> lowPriority, highPriority, decoding;
        private readonly Dictionary<long, int> decoded;
        private readonly int decodingThreads;

        #endregion

        #region Properties

        public int Pending { get { return lowPriority.Count + highPriority.Count; } }
        public int Decoding { get { return decoding.Count; } }

        public bool IsPending(long frameIdx) => lowPriority.Contains(frameIdx);

        public bool IsDecoding(long frameIdx) => decoding.Contains(frameIdx);

        public bool IsDecoded(long frameIdx) => false; // buffer.Contains(index);

        public bool IsProcessing(long frameIdx) => IsPending(frameIdx) || IsDecoding(frameIdx);

        #endregion

        public DecodingManager(string relativeVideoPath, int decodingThreads)
        {
            this.decodingThreads = decodingThreads;

            lowPriority = new List<long>();
            highPriority = new List<long>();
            decoding = new List<long>();
            decoded = new Dictionary<long, int>();

            decoders = Decoder.Initialize(relativeVideoPath, decodingThreads, out IntPtr[] dataPointers);
            buffer = new DecodingBuffer(dataPointers, Decoder.info, DecodingBuffer.BufferFormat.RGB24);

            Decoder.FrameReady += OnFrameReady;
            Decoder.invokeFrameReadyEvents = true;
        }

        public bool Decode(long frameIdx)
        {
            return false;
        }

        /*
        public bool DecodeToBufferAsync(long frameIdx, bool allowPending = true)
        {
            if (IsDecoding(frameIdx) || IsDecoded(frameIdx)) return false;
            if (decoding.Count < decodingThreads)
            {
                decoding.Add(frameIdx);
                int threadIdx = decoding.IndexOf(frameIdx);

                Task.Run(() =>
                {

                    // Debug.Log($"Decoding {path} with index {index} and nativeIndex {buffer[index]}");
                    Thread.CurrentThread.Priority = priority;
                    // Decoder.Decode(path, buffer[index], out long decodingTime);
                    // Decoder.Decode(frameIdx, threadIdx);
                    long decodingTime = -1;

                    Debug.Log(
                        $"Decoded {frameIdx} " +
                        $"in {decodingTime}ms " +
                        $"to position {frameIdx}");

                    decoding.Remove(frameIdx);
                    decoded.Add(frameIdx, threadIdx);
                });
            }
            else
            {
                if (allowPending && !IsPending(frameIdx) && lowPriority.Count < maxPending)
                {
                    Debug.Log($"Pending {frameIdx}");
                    lowPriority.Add(frameIdx);
                }
            }

            return true;
        }
        */
        public void Refresh()
        {
            var currentlyDecoded = decoded.ToArray();
            /*
            if (!cancelRequest && decoding.Count < decodingThreads && lowPriority.Count > 0 && lowPriority.Count < maxPending)
                Debug.Log($"{cancelRequest} {decoding.Count} {lowPriority.Count}");
                // DecodePending();
            */
        }

        public void Release()
        {
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

            buffer.Release();
            Decoder.Deinitialize();
        }

        private void OnFrameReady(long frameIdx, int threadIdx)
        {
            Debug.Log($"FrameReady callback for frame {frameIdx} from thread {threadIdx} invoked");
            buffer.Add(frameIdx, threadIdx);
        }
    }
}