using System.Collections.Generic;
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
        public Vector3[] vectorOffsets;
    }

    public class Map
    {
        public delegate void FrameData(Texture2D frame, Vector3 offset);
        public event FrameData FrameReady;

        public float fClip;
        public int textureWidth, textureHeight;
        public TextureFormat textureFormat;

        Vector3[] offsets;
        AssetBundle bundle;
        StandaloneMapConfig mapConfig;
        Dictionary<Vector3, AssetBundleRequest> requests;

        public Map(string path)
        {
            bundle = AssetBundle.LoadFromFile(path);
            requests = new Dictionary<Vector3, AssetBundleRequest>();

            TextAsset config = bundle.LoadAllAssets<TextAsset>()[0];
            mapConfig = JsonUtility.FromJson<StandaloneMapConfig>(config.text);
            fClip = mapConfig.fclip;
            offsets = mapConfig.vectorOffsets;
            
            Texture2D sample = bundle.LoadAllAssets<Texture2D>()[0];
            textureWidth = sample.width;
            textureHeight = sample.height;
            textureFormat = sample.format;
        }

        public Map(AssetBundle assetBundle)
        {
            bundle = assetBundle;
            requests = new Dictionary<Vector3, AssetBundleRequest>();
        }

        public void Request(Vector3 reqest)
        {
            AssetBundleRequest assetRequest = bundle.LoadAssetAsync<Texture2D>(VectorToFileName(reqest, mapConfig.mx_width));
            assetRequest.completed += AssetRequest_completed;
            requests.Add(reqest, assetRequest);
        }

        void AssetRequest_completed(AsyncOperation obj)
        {
            foreach (Vector3 key in requests.Keys)
            {
                if (requests[key].isDone)
                {
                    FrameReady.Invoke((Texture2D)requests[key].asset, key);
                    requests.Remove(key);
                }
            }
        }

        static string VectorToFileName(Vector3 vector, int mx_width)
        {
            return (vector.x + (vector.y + vector.z * mx_width) * mx_width).ToString();
        }
    }
}