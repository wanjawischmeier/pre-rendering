using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helper
{
    public static class TextureHelper
    {
        // Credit: https://github.com/ababilinski/unity-gpu-texture-resize/
        public static void CopyToArray(this Texture2D texture, Texture2DArray array, int index)
        {
            RenderTexture rt = RenderTexture.GetTemporary(array.width, array.height);
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Graphics.CopyTexture(rt, 0, array, index);
        }
    }
}