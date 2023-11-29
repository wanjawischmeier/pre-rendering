using UnityEngine;

public class CubeMapConversion : MonoBehaviour
{
    Matrix4x4[] orientationMatricies = new Matrix4x4[]
    {
        // positive x face (Depth, ViewY, -ViewX)
        new Matrix4x4(
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 1,  0, 0),
            new Vector4(1, 0,  0, 0),
            new Vector4(0, 0,  0, 1)
        ),

        // negative x face (-Depth, ViewY, ViewX)
        new Matrix4x4(
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(-1, 0, 0, 0),
            new Vector4(0, 0, 0, 1)
        ),

        // positive y face (ViewX, Depth, -ViewY)
        new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0, 1)
        ),
        
        // negative y face (ViewX, -Depth, ViewY)
        new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, -1, 0, 0),
            new Vector4(0, 0, 0, 1)
        ),

        // positive z face (ViewX, ViewY, Depth)
        new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 1)
        ),

        // negative z face (-ViewX, ViewY, -Depth)
        new Matrix4x4(
            new Vector4(-1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 0, 0, 1)
        )
    };

    public Vector2 uv;
    public float depth;
    public int faceIndex;
    public Vector2 viewSpace;
    public Vector4 P;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        viewSpace = new Vector2(2 * uv.x - 1, 1 - 2 * uv.y) * depth;
        var pos = new Vector4(viewSpace.x, viewSpace.y, depth, 1);
        P = orientationMatricies[faceIndex] * pos;
    }
}
