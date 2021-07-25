using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public int axis = 4;
    public float speed = 10;
    CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        float angle = 360 / axis;
        

    }
}
