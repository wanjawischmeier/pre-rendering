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
            RGBA64,
            RGBA32
        }

        public class NativeBuffer : Buffer<Vector3>
        {
            public NativeArray<uint> nativeBuffer;
            public ComputeBuffer computeBuffer;
            private readonly List<int> toCopy;

            public NativeBuffer(IntPtr bufferPointer, int width, int height, int depth, Format format) : base(depth)
            {
                int size = width * height * depth * 2;

                unsafe
                {
                    nativeBuffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(
                        bufferPointer.ToPointer(),
                        size,
                        Allocator.None);
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeBuffer, AtomicSafetyHandle.Create());
#endif

                computeBuffer = new ComputeBuffer(size, sizeof(uint));
                toCopy = new List<int>();
            }

            /// <summary>
            /// Pushes all new frames to the gpu
            /// (To be called in MonoBehaviour.Update)
            /// </summary>
            public void Refresh()
            {
                for (int i = 0; i < toCopy.Count; i++)
                {
                    computeBuffer.SetData(nativeBuffer);
                    toCopy.RemoveAt(i);
                }
            }

            public override void Add(int index)
            {
                if (nativeBuffer == null) return;
                toCopy.Add(index);
            }

            public void Release() => computeBuffer.Release();
        }
    }
}