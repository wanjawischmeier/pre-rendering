using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Vector3 vector;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        vector = transform.localEulerAngles;
    }
}
