using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GetMatricies : MonoBehaviour
{
    public Material[] materials;

    public Vector3 Translate;
    public Vector3 Rotate;
    public Vector3 Scale;

    [Range(10, 180)]
    public float fov;
    public Matrix4x4 TRS;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Material material in materials)
        {
            TRS = Matrix4x4.Perspective(fov, (float)Screen.height / (float)Screen.width, 1, 100) * Matrix4x4.TRS(Translate, Quaternion.Euler(Rotate), Scale);
            material.SetMatrix("_TRS", TRS);
        }
    }
}
