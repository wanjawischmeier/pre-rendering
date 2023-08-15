using UnityEngine;

public class FlyMovementController : MonoBehaviour
{
    public float flySpeed = 5f;
    public float rotationSpeed = 2f;

    private CharacterController characterController;
    private Transform cameraTransform;

    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor to the center of the screen
    }

    private void Update()
    {
        // Handle rotation (looking around)
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = -Input.GetAxis("Mouse Y") * rotationSpeed; // Invert Y-axis for natural camera movement

        // Apply vertical rotation to the camera
        horizontalRotation += mouseX;
        verticalRotation += mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);

        // Calculate movement based on user input
        float horizontalInput = 0f;
        float verticalInput = 0f;
        float upDownInput = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalInput = 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            verticalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            verticalInput = 1f;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            upDownInput = -1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            upDownInput = 1f;
        }

        Vector3 movement = transform.forward * verticalInput + transform.right * horizontalInput + transform.up * upDownInput;
        movement.Normalize();
        movement *= flySpeed * Time.deltaTime;

        characterController.Move(movement);
    }
}
