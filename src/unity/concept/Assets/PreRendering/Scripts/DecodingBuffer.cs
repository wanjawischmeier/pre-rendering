using System;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace PreRendering
{
    public class DecodingBuffer : Buffer<long>
    {
        public enum BufferFormat
        {
            RGB24 = 3,  // 3 channels * 1 byte per channel
            RGBA32 = 4, // 4 channels * 1 byte per channel
            RGB48 = 6,  // 3 channels * 2 bytes per channel
            RGBA64 = 8  // 4 channels * 2 bytes per channel
        }

        public NativeArray<byte>[] native;
        public ComputeBuffer compute;
        private readonly int imageSize;

        public DecodingBuffer(IntPtr[] bufferPointers, Decoder.VideoInfo info, int depth, BufferFormat format) : base(depth)
        {
            imageSize = info.width * info.height * (int)format;
            native = new NativeArray<byte>[depth];

            for (int i = 0; i < bufferPointers.Length; i++)
            {
                if (bufferPointers[i] == IntPtr.Zero)
                {
                    Debug.LogError("Passed a nullptr, buffer cannot be initialized");
                    return;
                }

                unsafe
                {
                    native[i] = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                        bufferPointers[i].ToPointer(),
                        imageSize,
                        Allocator.None);
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref native[i], AtomicSafetyHandle.Create());
#endif
            }

            compute = new ComputeBuffer(imageSize * depth / sizeof(uint), sizeof(uint));
        }

        public void Release() => compute.Release();

        public override void SetData(int nativeIdx, int bufferIdx)
        {
            compute.SetData(native[nativeIdx], 0, imageSize * bufferIdx, imageSize);
        }
    }
}