using PreRendering;
using System.Threading.Tasks;
using UnityEngine;

public class DecoderTest : MonoBehaviour
{
    public Shader shader;
    Material material;
    ComputeBuffer buffer;
    public string image_path;
    public Vector2Int res;
    public int threads;
    int[] data;

    void Start()
    {
        data = new int[res.x * res.y * 4 * threads];
        buffer = new ComputeBuffer(res.x * res.y * 4 * threads, sizeof(int));
        material = new Material(shader);
        material.SetVector("res", new Vector2(res.x, res.y));
        material.SetBuffer("Tex", buffer);

        Decoder.Initialize();
        /*
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        for (int i = 0; i < threads; i++)
        {
            Debug.Log(string.Format("Starting thread {0}", i));
            int j = i;
            Task.Run(() =>
            {
                Decoder.Decode(ref data, image_path, res.x, res.y, j);
            });
        }
        */
    }

    void Update()
    {
        buffer.SetData(data);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }

    void OnDestroy()
    {
        Decoder.Deinitialize();
        buffer.Release();
    }

    void Decoder_ImageDecoded(string path, int[] data, int t)
    {
        string bytestr = "";

        foreach (uint _byte in data)
        {
            bytestr += _byte.ToString() + "-";
        }
        Debug.Log(bytestr + data.Length.ToString());
    }
}
