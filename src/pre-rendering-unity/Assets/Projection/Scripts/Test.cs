using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Test : MonoBehaviour
{
    public Vector3 vector;

    void Start()
    {
        
    }

    void Update()
    {

        transform.eulerAngles = Vector3.right * 125;
        vector = transform.eulerAngles;
    }
}
