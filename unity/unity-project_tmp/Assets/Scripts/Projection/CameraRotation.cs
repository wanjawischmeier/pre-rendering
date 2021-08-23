using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraRotation : MonoBehaviour
{
    public Material material;
    public Texture texture;
    [Range(1, 10)]
    public float angleOfView;
    // [Range(20, 90)]
    public float fac;
    public float x;
    public float x_r;

    int texProp;
    int angleProp;
    int phi1Prop;
    int lambda0Prop;
    float phi1;
    public float lambda0 = 0;

    void Start()
    {
        texProp = Shader.PropertyToID("_MainTex");
        angleProp = Shader.PropertyToID("_AngleOfView");
        phi1Prop = Shader.PropertyToID("_Phi1");
        lambda0Prop = Shader.PropertyToID("_Lambda0");

        material.SetTexture(texProp, texture);
    }

    void Update()
    {
        /*
        phi1 = transform.rotation.eulerAngles.x / fac;
        lambda0 = transform.rotation.eulerAngles.y / fac;
        */
        material.SetFloat(angleProp, angleOfView);
        x_r = transform.localEulerAngles.x;
        material.SetFloat(phi1Prop, x / -60);
        material.SetFloat(lambda0Prop, lambda0);
    }
}
