using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    Transform camera;

    void Start()
    {
        camera = Camera.main.transform;
    }
    void OnBecameVisible()
    {
        //Debug.Log("Visible");
    }
    void OnBecameInvisible()
    {
        //Debug.Log("Invisible");
        if (camera.position.y < 100)
        {
            camera.position = new Vector3(camera.position.x, camera.position.y + 0.1f, camera.position.z);
        }
    }
}
