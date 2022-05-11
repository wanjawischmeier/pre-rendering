using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using UnityEngine;

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

        public readonly DecodingBuffer buffer;

        private readonly Decoder[] decoders;
        private readonly List<long> lowPriority, highPriority, decoding;
        private readonly Dictionary<long, int> decoded;
        private readonly int decodingThreads;
        private ChunkIndexing.ChunkIndex chunkIndex;
        private ChunkIndexing.GlobalIndex globalIndex;
        private bool correctChunkIndex;

        #endregion

        #region Properties

        public int Pending { get { return lowPriority.Count + highPriority.Count; } }
        public int Decoding { get { return decoding.Count; } }

        public bool IsPending(long frameIdx) => lowPriority.Contains(frameIdx);

        public bool IsDecoding(long frameIdx) => decoding.Contains(frameIdx);

        public bool IsDecoded(long frameIdx) => false; // buffer.Contains(index);

        public bool IsProcessing(long frameIdx) => IsPending(frameIdx) || IsDecoding(frameIdx);

        public Vector3 Position
        {
            set => correctChunkIndex = ChunkIndexing.CorrectChunkIndex(globalIndex, value, out chunkIndex, out globalIndex);
        }

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

        public bool Decode(long frameIdx, int bufferIdx)
        {
            return decoders[bufferIdx].Decode(frameIdx);
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
            buffer.Refresh();
            // var currentlyDecoded = decoded.ToArray();
            /*
            if (!cancelRequest && decoding.Count < decodingThreads && lowPriority.Count > 0 && lowPriority.Count < maxPending)
                Debug.Log($"{cancelRequest} {decoding.Count} {lowPriority.Count}");
                // DecodePending();
            */
        }

        public void Release()
        {
            Decoder.FrameReady -= OnFrameReady;

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

            var globalIndex = frameIdx.GetGlobalIndex(out int channelBlock);
            int localIndex = globalIndex.Local;

            var decoder = Decoder.decoders[threadIdx];

            // Graphics.CopyTexture(source.texture, 0, chunk, localIndex);
            ChunkIndexing.chunkIndicies[localIndex] = chunkIndex;
            // buffer.SetData(ChunkIndexing.chunkIndicies, localIndex, localIndex, 1);

            // Finished decoding channel block of chunk
            if ((frameIdx + 1) % ChunkIndexing.chunkSize == 0 && (frameIdx + 1) - channelBlock * ChunkIndexing.totalSize != 0)
            {
                ChunkIndexing.chunkIndicies[localIndex] = chunkIndex;
                // buffer.SetData(ChunkIndexing.chunkIndicies, localIndex, localIndex, 1);

                Debug.LogFormat("Finished decoding channel block {0} of chunk {1}.", channelBlock, (int)chunkIndex);
                decoder.Pause();
                return;
            }

            // Player is loading the wrong chunk
            if (!correctChunkIndex)
                decoder.Frame = globalIndex;
        }
    }
}