using UnityEngine;

[ExecuteInEditMode]
public class FlipNormals : MonoBehaviour
{
    void Awake()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int t = triangles[i];
            triangles[i] = triangles[i + 2];
            triangles[i + 2] = t;
        }

        mesh.triangles = triangles;
    }
}
