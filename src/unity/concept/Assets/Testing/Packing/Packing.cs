using UnityEngine;
using PreRendering;

[ExecuteInEditMode]
public class Packing : MonoBehaviour
{
    public Shader shader;
    public ushort[] values;
    public int index;

    Material material;
    ComputeBuffer buffer;

    void OnValidate()
    {
        material = new Material(shader);
        buffer = new ComputeBuffer(3, sizeof(ushort) * 4);
        material.SetBuffer("Buff", buffer);
    }

    void Update()
    {
        buffer.SetData(values);
        material.SetInt("Idx", index);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }

    Vector2 Unpack(uint v)
    {
        return new Vector2(v >> 16, v & 0xFFFF) / 0xFFFF;
    }

    Vector4 Unpack(Vector2Int v)
    {
        Vector2 v1 = Unpack((uint)v.x);
        Vector2 v2 = Unpack((uint)v.y);

        return new Vector4(v1.y, v1.x, v2.y, v2.x);
    }
}
