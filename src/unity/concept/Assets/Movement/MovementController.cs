using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class MovementController : MonoBehaviour
{
    public float speed = 7.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    public Vector3 secondaryPosition;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0, rotationY = 0;

    void Start() =>
        characterController = GetComponent<CharacterController>();

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Vector3 back = transform.TransformDirection(Vector3.back);
        Vector3 left = transform.TransformDirection(Vector3.left);

        float curSpeedX = speed * -Input.GetAxis("Vertical");
        float curSpeedY = speed * -Input.GetAxis("Horizontal");
        moveDirection = (back * curSpeedX) + (left * curSpeedY);

        moveDirection.y = Input.GetKey(KeyCode.Q) ? speed * Time.deltaTime * 25 : Input.GetKey(KeyCode.E) ? -speed * Time.deltaTime * 25 : 0;
        Vector3 oldPos = transform.position;
        characterController.Move(moveDirection * Time.deltaTime);

        rotationX += Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY += Input.GetAxis("Mouse X") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);

        if (Input.GetMouseButton(0))
        {
            secondaryPosition += transform.position - oldPos;
        }
        if (Input.GetMouseButton(1))
        {
            secondaryPosition = Vector3.zero;
        }
    }
}