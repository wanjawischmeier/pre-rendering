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
            RGBA64
        }

        public class Buffer : Buffer<Vector3>
        {
            public NativeArray<uint> nativeBuffer;
            public ComputeBuffer computeBuffer;
            private readonly List<int> toCopy;

            public Buffer(IntPtr bufferPointer, int width, int height, int depth, Format format = Format.RGBA64) : base(depth)
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