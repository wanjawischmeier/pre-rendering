using UnityEngine;

public static class CubeMapConversion
{
    public static readonly Matrix4x4[] orientationMatricies = new Matrix4x4[]
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

    public static readonly Matrix4x4[] alternateOrientationMatricies = new Matrix4x4[]
    {
        // negative x face (-Depth, ViewY, ViewX)
        new Matrix4x4(
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(-1, 0, 0, 0),
            new Vector4(0, 0, 0, 1)
        ),

        // positive x face (Depth, ViewY, -ViewX)
        new Matrix4x4(
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 1,  0, 0),
            new Vector4(1, 0,  0, 0),
            new Vector4(0, 0,  0, 1)
        ),

        // negative y face (ViewX, -Depth, ViewY)
        new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, -1, 0, 0),
            new Vector4(0, 0, 0, 1)
        ),

        // positive y face (ViewX, Depth, -ViewY)
        new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 1, 0, 0),
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


    public static Matrix4x4[] inverseOrientationMatricies
    {
        get
        {
            var matricies = new Matrix4x4[orientationMatricies.Length];
            for (int faceIndex = 0; faceIndex < orientationMatricies.Length; faceIndex++)
            {
                matricies[faceIndex] = orientationMatricies[faceIndex].inverse;
            }

            return matricies;
        }
    }


    public static Matrix4x4[] alternateInverseOrientationMatricies
    {
        get
        {
            var matricies = new Matrix4x4[alternateOrientationMatricies.Length];
            for (int faceIndex = 0; faceIndex < alternateOrientationMatricies.Length; faceIndex++)
            {
                matricies[faceIndex] = alternateOrientationMatricies[faceIndex].inverse;
            }

            return matricies;
        }
    }
}
