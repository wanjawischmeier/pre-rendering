using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RenderTest : MonoBehaviour
{
    public Material material;

    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        cam.depthTextureMode = DepthTextureMode.Depth;
        material.SetMatrix("_CamToWorld", cam.worldToCameraMatrix.inverse);
        material.SetMatrix("_ViewToWorld", cam.cameraToWorldMatrix);
        material.SetMatrix("_WorldToCam", cam.worldToCameraMatrix);
        material.SetMatrix("_Translate", Matrix4x4.Translate(new Vector3(0, 2, 3)));
        Debug.Log(cam.cameraToWorldMatrix);
    }

    // Update is called once per frame
    void Update()
    {
        // cam.main.camToWorldMatrix
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
