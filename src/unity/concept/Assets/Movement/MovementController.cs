using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    public float speed = 7.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0, rotationY = 0;

    private void Start() =>
        characterController = GetComponent<CharacterController>();

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.back);
        Vector3 right = transform.TransformDirection(Vector3.left);
        
        float curSpeedX = speed * -Input.GetAxis("Vertical");
        float curSpeedY = speed * -Input.GetAxis("Horizontal");
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        moveDirection.y = (Input.GetKey(KeyCode.Q) ? -speed : Input.GetKey(KeyCode.E) ? speed : 0) * Time.deltaTime * 100;
        characterController.Move(moveDirection * Time.deltaTime);

        rotationX += Input.GetAxis("Mouse Y") * lookSpeed;
        rotationY += Input.GetAxis("Mouse X") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }
}