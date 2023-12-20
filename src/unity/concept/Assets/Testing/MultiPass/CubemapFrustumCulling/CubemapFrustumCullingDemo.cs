using UnityEngine;

[ExecuteInEditMode]
public class CubemapFrustumCullingDemo : MonoBehaviour
{
    [System.Serializable]
    public struct Frustum
    {
        public float nearClipPlane, farClipPlane, fieldOfView, aspectRatio;
        public Vector3 position, rotation;

        public Matrix4x4 matrix
        {
            get
            {
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), Vector3.one);
                Debug.Log(matrix);
                matrix = Matrix4x4.Perspective(fieldOfView, aspectRatio, nearClipPlane, farClipPlane) * matrix;
                Debug.Log(matrix);
                return matrix;
            }
        }
    }

    private Vector3[] cubemapFaceAllignment = new Vector3[6]
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    public float cubemapNearClipPlane, cubemapFarClipPlane;
    public Vector3[] cubemapPositions;
    private Frustum cameraFrustum, testFrustum;
    private Frustum[] cubemapFrustums;

    private void Start()
    {
        Camera currentCamera = GetComponent<Camera>();
        cameraFrustum = new Frustum
        {
            nearClipPlane = currentCamera.nearClipPlane,
            farClipPlane = currentCamera.farClipPlane,
            fieldOfView = currentCamera.fieldOfView,
            aspectRatio = currentCamera.aspect,
            position = transform.position,
            rotation = transform.rotation.eulerAngles
        };
        
        testFrustum = new Frustum
        {
            nearClipPlane = cubemapNearClipPlane,
            farClipPlane = cubemapFarClipPlane,
            fieldOfView = 90,
            aspectRatio = 1,
            position = Vector3.zero,
            rotation = Vector3.zero
        };

        cubemapFrustums = new Frustum[cubemapPositions.Length];
        for (int cubemapIndex = 0; cubemapIndex < cubemapPositions.Length; cubemapIndex++)
        {
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                cubemapFrustums[cubemapIndex] = new Frustum
                {
                    nearClipPlane = cubemapNearClipPlane,
                    farClipPlane = cubemapFarClipPlane,
                    fieldOfView = 90, aspectRatio = 1,
                    position = cubemapPositions[cubemapIndex],
                    rotation = cubemapFaceAllignment[faceIndex]
                };
            }
        }
    }

    private void OnDrawGizmos()
    {
        Debug.Log(testFrustum.matrix);
        /// bool frustumsIntersect = AreFrustumsIntersecting(cameraFrustum, testFrustum);

        // Gizmos.color = frustumsIntersect ? Color.red : Color.green;
        Gizmos.DrawFrustum(Vector3.zero, 90, cubemapFarClipPlane, cubemapNearClipPlane, 1);
    }

    // Function to check frustum intersection using matrices
    public static bool AreFrustumsIntersecting(Frustum frustum0, Frustum frustum1)
    {
        // Use matrix operations to check for intersection
        Matrix4x4 relativeMatrix = frustum1.matrix.inverse * frustum0.matrix;

        // Check if the relative matrix has any scaling, which indicates intersection
        if (HasScaling(relativeMatrix))
        {
            return true; // Frustums are intersecting
        }

        return false; // Frustums are not intersecting
    }

    // Function to check if a matrix has scaling
    private static bool HasScaling(Matrix4x4 matrix)
    {
        Vector3 scale = matrix.lossyScale;
        return Mathf.Abs(scale.x - 1) > float.Epsilon || Mathf.Abs(scale.y - 1) > float.Epsilon || Mathf.Abs(scale.z - 1) > float.Epsilon;
    }
}
