using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour
{

    private bool canMove = true;

    [Header("Set Gravity")]
    // gravity 
    [SerializeField] private float gravity = 20.0f;

    [Header("Set Character Values")]
    // character speeds
    [SerializeField] private float walkSpeed = 7.0f;
    [SerializeField] private float sprintSpeed = 13.0f;
    [SerializeField] private float jumpSpeed = 8.5f;

    [Header("Set Camera Controls")]
    // camera controls
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSpeed = 2.5f;
    [SerializeField] private float lookLimitX = 45.0f;


    [Header("Set Crouch Values")]
    [SerializeField] private float crouchSpeed = 3.0f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2.0f;

    private bool isCrouchKeyPressed = false;


    // character controller
    CharacterController characterController;
    float rotationX = 0;
    Vector3 moveDirection = Vector3.zero;
    public bool activeGrapple;


    void Start()
    {
        // Locks mouse cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerCamera = Camera.main;
        characterController = GetComponent<CharacterController>();

    }


    void Update()
    {
        if (activeGrapple) return;

        Vector3 moveForward = transform.TransformDirection(Vector3.forward);
        Vector3 moveRight = transform.TransformDirection(Vector3.right);


        // isSprinting to make player run

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float speed = isCrouchKeyPressed ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        float cursorSpeedX = canMove ? (isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float cursorSpeedY = canMove ? (isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (moveForward * cursorSpeedX) + (moveRight * cursorSpeedY);

        // jump 

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (Input.GetKeyDown(KeyCode.C) && canMove)
        {
            isCrouchKeyPressed = true;
        }
        if (Input.GetKeyUp(KeyCode.C) && canMove)
        {
            isCrouchKeyPressed = false;
        }

        if (isCrouchKeyPressed)
        {
            // Check for obstacles above before crouching
            if (!CheckHeadObstacle())
            {
                characterController.height = crouchHeight;
            }
            else
            {
                isCrouchKeyPressed = true; // Keep crouching if obstacle above
            }
        }
        else
        {
            characterController.height = standingHeight;
        }

        // gravity 
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Moves the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Camera
        if (canMove)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);


            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookLimitX, lookLimitX);
        }
    }

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        moveDirection = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
    }


    void ToggleCrouch()
    {
        isCrouchKeyPressed = !isCrouchKeyPressed;

        if (isCrouchKeyPressed)
        {

            if (!CheckHeadObstacle())
            {
                characterController.height = crouchHeight;
            }
            else
            {
                isCrouchKeyPressed = true; // Keep crouching if obstacle above
            }
        }
        else
        {
            characterController.height = standingHeight;
        }
    }
    bool CheckHeadObstacle()
    {
        float radius = characterController.radius;
        float height = isCrouchKeyPressed ? crouchHeight : standingHeight;

        // Cast a sphere to check for obstacles above
        bool hasObstacle = Physics.SphereCast(
            transform.position + Vector3.up * (height - radius), // Start position
            radius, // Sphere radius
            Vector3.up, // Direction
            out RaycastHit hitInfo,
            height - radius, // Max distance
            ~LayerMask.GetMask("Player") // Ignore the player's own collider
        );

        return hasObstacle;
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

}
