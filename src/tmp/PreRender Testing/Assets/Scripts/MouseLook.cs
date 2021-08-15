using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensitivity;
    public float max;

    float yRotation = 0f;
    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation += mouseY;
        yRotation -= mouseX;
        xRotation = Mathf.Clamp(xRotation, -max, max);

        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
