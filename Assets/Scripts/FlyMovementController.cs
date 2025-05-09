using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FlyMovementController : MonoBehaviour
{
    public float flySpeed = 5f;
    public float rotationSpeed = 2f;
    public GameObject pauseMenuCanvas;
    public bool lockCursor = true;

    private CharacterController characterController;

    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        TryApplyMapSpeedMultiplier();

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void Update()
    {
        if (pauseMenuCanvas != null)
        {
            if (!pauseMenuCanvas.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                pauseMenuCanvas.SetActive(true);
                if (lockCursor)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                return;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                pauseMenuCanvas.SetActive(false);
                if (lockCursor)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            else if (pauseMenuCanvas.activeSelf)
            {
                return;
            }
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? flySpeed / 4 : flySpeed;

        // Handle rotation (looking around)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        // Apply vertical rotation to the camera
        if (rotationSpeed != 0)
        {
            horizontalRotation += mouseX * rotationSpeed;
            verticalRotation += -mouseY * rotationSpeed;    // Invert Y-axis for natural camera movement
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
        }

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
        movement *= speed * Time.deltaTime;

        characterController.Move(movement);
    }

    public void Quit() => Application.Quit();

    public void TryApplyMapSpeedMultiplier()
    {
        if (TryGetComponent(out RenderManager renderManager))
        {
            flySpeed *= renderManager.map.flySpeedMultiplier;
        }
    }
}
