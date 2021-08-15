using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraRotation : MonoBehaviour
{
    public Material material;
    public Texture texture;
    
    public float angleOfView;

    const float PI = Mathf.PI;
    const float PI2 = Mathf.PI * 2;

    int texProp;
    int angleProp;
    int phi1Prop;
    int lambda0Prop;
    int rotProp;

    void Start()
    {
        texProp = Shader.PropertyToID("_MainTex");
        angleProp = Shader.PropertyToID("_AngleOfView");
        phi1Prop = Shader.PropertyToID("_Phi1");
        lambda0Prop = Shader.PropertyToID("_Lambda0");
        rotProp = Shader.PropertyToID("_Theta");

        material.SetTexture(texProp, texture);
    }

    void Update()
    {
        /*
        phi1 = transform.rotation.eulerAngles.x / fac;
        lambda0 = transform.rotation.eulerAngles.y / fac;
        */
        // angleOfView = 180 / Camera.main.fieldOfView;
        material.SetFloat(angleProp, angleOfView);
        material.SetFloat(phi1Prop, transform.eulerAngles.x * PI2*2 / 360);
        material.SetFloat(lambda0Prop, transform.eulerAngles.y * PI2 / 360);
        material.SetFloat(rotProp, transform.eulerAngles.z * PI / 360);
    }
}
