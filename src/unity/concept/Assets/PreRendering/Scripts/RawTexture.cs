using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace PreRendering
{
    public class RawTexture
    {
        public enum Format
        {
            RGB24 = 3,  // 3 channels * 1 byte per channel
            RGBA32 = 4, // 4 channels * 1 byte per channel
            RGB48 = 6,  // 3 channels * 2 bytes per channel
            RGBA64 = 8  // 4 channels * 2 bytes per channel
        }

        public class NativeBuffer : Buffer<long>
        {
            public NativeArray<byte>[] nativeBuffer;
            public ComputeBuffer computeBuffer;
            private readonly Queue<int> toCopy;

            public NativeBuffer(IntPtr[] bufferPointers, int width, int height, int depth, Format format) : base(depth)
            {
                int size = width * height * (int)format * depth;
                nativeBuffer = new NativeArray<byte>[depth];

                for (int i = 0; i < depth; i++)
                {
                    if (bufferPointers[i] == IntPtr.Zero)
                    {
                        Debug.LogError("Passed a nullptr, buffer cannot be initialized");
                        return;
                    }

                    unsafe
                    {
                        nativeBuffer[i] = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                            bufferPointers[i].ToPointer(),
                            size,
                            Allocator.None);
                    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeBuffer[i], AtomicSafetyHandle.Create());
#endif
                }

                computeBuffer = new ComputeBuffer(size / sizeof(uint), sizeof(uint));
                toCopy = new Queue<int>();
            }

            /// <summary>
            /// Pushes all new frames to the gpu
            /// (To be called in MonoBehaviour.Update)
            /// </summary>
            public void Refresh()
            {
                for (int i = 0; i < toCopy.Count; i++)
                {
                    int idx = toCopy.Dequeue();

                    // TODO: Set start index and count
                    computeBuffer.SetData(nativeBuffer[0]);
                }
            }

            public override void Add(int index)
            {
                if (nativeBuffer == null) return;
                toCopy.Enqueue(index);
            }

            public void Release() => computeBuffer.Release();
        }
    }
}