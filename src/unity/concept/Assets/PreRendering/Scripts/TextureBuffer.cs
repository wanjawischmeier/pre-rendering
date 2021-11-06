using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace PreRendering
{
    public class TextureBuffer : Buffer<Vector3, Texture2D>
    {
        public NativeArray<uint> textures;

        public TextureBuffer(IntPtr pointer, int width, int height, int cacheSize, TextureFormat format = TextureFormat.RGBA32) : base(cacheSize)
        {
            unsafe
            {
                textures = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(
                    pointer.ToPointer(),
                    width * height * cacheSize * 2,
                    Allocator.None);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref textures, AtomicSafetyHandle.Create());
#endif
        }


        public override void Add(int index, Texture2D value)
        {
            if (textures == null) return;
            Graphics.CopyTexture(value, 0, textures, index);
        }
    }
}