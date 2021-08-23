using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }
    void OnBecameVisible()
    {
        //Debug.Log("Visible");
    }
    void OnBecameInvisible()
    {
        //Debug.Log("Invisible");
        if (cam.position.y < 100)
        {
            cam.position = new Vector3(cam.position.x, cam.position.y + 0.1f, cam.position.z);
        }
    }
}
