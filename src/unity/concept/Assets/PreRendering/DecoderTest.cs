using PreRendering;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class DecoderTest : MonoBehaviour
{
    public Shader shader;
    Material material;
    ComputeBuffer buffer;
    public Vector2Int res;
    public int threads;
    string imagePath;
    uint[] data;

    void Start()
    {
        string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/room_simple_v2_270p/0000.png");

        int size = res.x * res.y * threads * 2;
        data = new uint[size];
        buffer = new ComputeBuffer(size, sizeof(uint));

        material = new Material(shader);
        material.SetVector("res", new Vector2(res.x, res.y));
        material.SetBuffer("Tex", buffer);

        Decoder.Initialize(imagePath, res.x, res.y);
        
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        for (int i = 0; i < threads; i++)
        {
            Debug.Log(string.Format("Starting thread {0}", i));
            int j = i;
            Task.Run(() => Decoder.Decode(imagePath, ref data, j));
        }
        
    }

    void Update()
    {
        if (data != null) buffer.SetData(data);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(source, destination, material);

    void OnDestroy()
    {
        Decoder.Deinitialize();
        buffer.Release();
    }

    private void Decoder_ImageDecoded(string path)
    {
        string bytestr = "";
        int i = 0;

        foreach (uint pixel in data)
        {
            string sPixel = pixel.ToString();
            sPixel = sPixel.PadLeft(10, '0');

            Decoder.Unpack(pixel, out ushort a, out ushort b);
            bytestr += string.Format("{0}\t- (P: {1},\t\tC: [{2},{3}])\n", i++, sPixel, a, b);
        }
        bytestr += string.Format("{0} pixels total\n", data.Length);

        Debug.Log(bytestr);
    }
}
