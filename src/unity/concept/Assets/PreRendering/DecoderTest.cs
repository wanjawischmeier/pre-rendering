using PreRendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class DecoderTest : MonoBehaviour
{
    public Shader shader;
    Material material;
    public string image_path;
    public Vector2Int res;
    public int threads;

    void Start()
    {
        material = new Material(shader);
        material.SetVector("res", new Vector2(res.x, res.y));

        Decoder.Initialize();
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        List<Task<ushort[]>> tasks = new List<Task<ushort[]>>();
        for (int i = 0; i < threads; i++)
        {
            Debug.Log(string.Format("Starting thread {0}", i));
            tasks.Add(Task.Run(() =>
            {
                return Decoder.Decode(image_path, res.x, res.y, i);
            }));
        }
    }
    private static void Decoder_ImageDecoded(string path, ushort[] data, int t)
    {
        string bytestr = t.ToString() + ":";

        foreach (ushort _byte in data)
        {
            bytestr += _byte.ToString() + "-";
        }
        bytestr += data.Length.ToString();

        Debug.Log(bytestr);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }

    void OnDestroy() =>
        Decoder.Deinitialize();
}
