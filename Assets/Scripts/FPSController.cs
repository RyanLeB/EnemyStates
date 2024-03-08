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
        
        characterController = GetComponent<CharacterController>();

    }


    void Update()
    {
        if (activeGrapple) return;

        Vector3 moveForward = transform.TransformDirection(Vector3.forward);
        Vector3 moveRight = transform.TransformDirection(Vector3.right);


        // isSprinting to make player run

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        
        float cursorSpeedX = canMove ? (isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float cursorSpeedY = canMove ? (isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (moveForward * cursorSpeedX) + (moveRight * cursorSpeedY);

    }

}
