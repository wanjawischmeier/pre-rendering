using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FlyMovementController : MonoBehaviour
{
    public float flySpeed = 5f;
    public float rotationSpeed = 2f;
    public DynamicJoystick moveJoystick, viewJoystick;

    private CharacterController characterController;

    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            moveJoystick.gameObject.SetActive(true);
            viewJoystick.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            return;
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? flySpeed / 10 : flySpeed;

        // Handle rotation (looking around)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");
        if (viewJoystick.Horizontal != 0 || viewJoystick.Vertical != 0)
        {
            mouseX = viewJoystick.Horizontal;
            mouseY = viewJoystick.Vertical;
        }
        else if (moveJoystick.Horizontal != 0 || moveJoystick.Vertical != 0)
        {
            mouseX = mouseY = 0;
        }

        // Apply vertical rotation to the camera
        horizontalRotation += mouseX * rotationSpeed;
        verticalRotation += -mouseY * rotationSpeed;    // Invert Y-axis for natural camera movement
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);

        // Calculate movement based on user input
        float horizontalInput = 0f;
        float verticalInput = 0f;
        float upDownInput = 0f;

        if (moveJoystick.Horizontal != 0)
        {
            horizontalInput = moveJoystick.Horizontal;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalInput = 1f;
        }

        if (moveJoystick.Vertical != 0)
        {
            verticalInput = moveJoystick.Vertical;
        }
        else if (Input.GetKey(KeyCode.S))
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
        movement *= speed * Time.deltaTime;

        characterController.Move(movement);
    }
}
