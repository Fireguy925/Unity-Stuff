using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HorseController : MonoBehaviour
{
    public Transform saddlePoint;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float acceleration = 2f;
    public float deceleration = 2f;

    private float currentSpeed = 0f;
    private bool isMounted = false;
    public bool IsMounted => isMounted;

    private float gravity = -9.81f;
    private Vector3 verticalVelocity = Vector3.zero;

    private Animator animator;
    private Transform rider;
    private CharacterController controller;

    [Header("Audio")]
    public AudioSource footstepSource;
    public AudioClip walkClip;
    public AudioClip runClip;

    private float stepTimer = 0f;
    private float stepInterval = 0.5f;

    private const float walkStepInterval = 1f;
    private const float runStepInterval = 0.5f;
    private const float idleToWalkThreshold = 3f;
    private const float walkToRunThreshold = 4f;

    [Header("Camera")]
    public Transform cameraTarget;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction dismountAction;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        isMounted = false;
    }

    private void Update()
    {
        if (!isMounted || rider == null)
            return;

        HandleMovement();
        UpdateAnimations();

        if (dismountAction != null && dismountAction.WasPressedThisFrame())
            Dismount();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        float vertical = moveInput.y;

        float targetSpeed = 0f;
        if (vertical > 0.1f)
        {
            bool isSprinting = sprintAction != null && sprintAction.IsPressed();
            targetSpeed = isSprinting ? runSpeed : walkSpeed;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
            (currentSpeed < targetSpeed ? acceleration : deceleration) * Time.deltaTime);

        float horizontal = moveInput.x;
        if (Mathf.Abs(horizontal) > 0.1f)
            transform.Rotate(Vector3.up * horizontal * 60f * Time.deltaTime);

        if (controller.isGrounded)
            verticalVelocity.y = -1f;
        else
            verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 move = transform.forward * currentSpeed + verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    private void UpdateAnimations()
    {
        animator.SetFloat("Speed", currentSpeed);
        PlayHorseSounds();
    }

    private void PlayHorseSounds()
    {
        if (controller.isGrounded && currentSpeed > idleToWalkThreshold)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                stepInterval = currentSpeed > walkSpeed + walkToRunThreshold
                    ? runStepInterval : walkStepInterval;

                AudioClip clipToPlay = currentSpeed > walkSpeed + walkToRunThreshold
                    ? runClip : walkClip;

                if (footstepSource && clipToPlay)
                    footstepSource.PlayOneShot(clipToPlay);

                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    public void Mount(Transform player)
    {
        if (isMounted)
            return;

        isMounted = true;
        rider = player;

        var riderRigidbody = rider.GetComponent<Rigidbody>();
        if (riderRigidbody != null)
        {
            riderRigidbody.linearVelocity = Vector3.zero;
            riderRigidbody.angularVelocity = Vector3.zero;
            riderRigidbody.isKinematic = true;
        }

        var riderController = rider.GetComponent<CharacterController>();
        if (riderController != null) riderController.enabled = false;

        var riderCollider = rider.GetComponent<Collider>();
        if (riderCollider != null) riderCollider.enabled = false;

        originalCameraParent = Camera.main.transform.parent;
        originalCameraLocalPosition = Camera.main.transform.localPosition;
        originalCameraLocalRotation = Camera.main.transform.localRotation;

        StartCoroutine(MountNextFrame(player));
    }

    private IEnumerator MountNextFrame(Transform player)
    {
        yield return null;

        // Create input actions directly in code
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        sprintAction = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        dismountAction = new InputAction("Dismount", binding: "<Keyboard>/f");
        dismountAction.AddBinding("<Gamepad>/buttonEast");

        moveAction.Enable();
        sprintAction.Enable();
        dismountAction.Enable();

        rider.SetParent(saddlePoint);
        rider.localPosition = Vector3.zero;
        rider.localRotation = Quaternion.identity;

        Transform camParent = cameraTarget != null ? cameraTarget : saddlePoint;
        Camera.main.transform.SetParent(camParent);
        Camera.main.transform.localPosition = new Vector3(0, 1.5f, -3.5f);
        Camera.main.transform.localRotation = Quaternion.Euler(10, 0, 0);

        Debug.Log($"Mounted successfully — player pos: {rider.position}");
    }

    public void Dismount()
    {
        if (!isMounted || rider == null)
            return;

        rider.SetParent(null);
        rider.position = transform.position - transform.right * 3f + Vector3.up * 1f;

        var riderRigidbody = rider.GetComponent<Rigidbody>();
        if (riderRigidbody != null) riderRigidbody.isKinematic = false;

        var riderController = rider.GetComponent<CharacterController>();
        if (riderController != null) riderController.enabled = true;

        var riderCollider = rider.GetComponent<Collider>();
        if (riderCollider != null) riderCollider.enabled = true;

        Camera.main.transform.SetParent(originalCameraParent);
        Camera.main.transform.localPosition = originalCameraLocalPosition;
        Camera.main.transform.localRotation = originalCameraLocalRotation;

        moveAction?.Disable();
        moveAction?.Dispose();
        sprintAction?.Disable();
        sprintAction?.Dispose();
        dismountAction?.Disable();
        dismountAction?.Dispose();

        moveAction     = null;
        sprintAction   = null;
        dismountAction = null;

        isMounted = false;
        rider = null;
        currentSpeed = 0f;
    }
}