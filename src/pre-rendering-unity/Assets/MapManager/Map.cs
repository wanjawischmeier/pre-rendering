using System.Linq;
using UnityEngine;

namespace MapManagement
{
    public struct RawMapConfig
    {
        public int resolution;
        public int fclip;
        public int mx_width;
        public float[] offsets;
    }

    public struct StandaloneMapConfig
    {
        public int fclip;
        public int mx_width;
        public int textureWidth;
        public int textureHeight;
        public Vector3[] vectorOffsets;
    }

    public class Map
    {
        public StandaloneMapConfig config;
        AssetBundle bundle;

        public Map(string path)
        {
            bundle = AssetBundle.LoadFromFile(path);

            TextAsset rawConfig = bundle.LoadAllAssets<TextAsset>()[0];
            config = JsonUtility.FromJson<StandaloneMapConfig>(rawConfig.text);
        }

        public void SetTexturesAtPositions(Vector3[] requests, ref Texture2DArray textures, ref Texture2D[] texture2s)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                Texture2D texture = bundle.LoadAsset<Texture2D>(VectorToFileName(requests[i], config.mx_width));
                Graphics.CopyTexture(texture, 0, textures, i);
                Debug.Log(texture.mipmapCount);
                Debug.Log(texture.format);
                Debug.Log(texture.graphicsFormat);
                Debug.Log(textures.mipmapCount);
                Debug.Log(textures.format);
                Debug.Log(textures.graphicsFormat);
                
                texture2s[i] = texture;
            }
        }

        public Vector3[] GetClosest(Vector3 position, int length)
        {
            return config.vectorOffsets
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(length)
                .ToArray();
        }

        static string VectorToFileName(Vector3 vector, int mx_width)
        {
            return "first_render_landscape" + (vector.x + (vector.y + vector.z * mx_width) * mx_width).ToString().PadLeft(4, '0');
        }
    }
}