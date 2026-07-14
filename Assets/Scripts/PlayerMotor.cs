using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private float Speed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float lookSpeed = 200f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 1f;
    [Header("Sprint")]
    [Tooltip("If true, sprint toggles on press; if false, sprint is active while held")]
    public bool sprintToggleMode = false;
    [SerializeField] private float sprintDuration = 5f;
    [SerializeField] private float sprintCooldown = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;

    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Rigidbody rb;
    private float verticalRotation = 0f;
    private bool isSprinting = false;
    private float sprintTimer = 0f;
    private float cooldownTimer = 0f;
    private float baseSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 999f;
        rb.freezeRotation = true;
        baseSpeed = Speed;
    }

    public void SetMoveInput(Vector2 input) => moveInput = input;

    // Toggle sprint (for toggle mode)
    public void ToggleSprint()
    {
        if (isSprinting)
            StopSprint();
        else
            TryStartSprint();
    }

    // Hold sprint input (for hold mode)
    public void SetSprintHold(bool hold)
    {
        if (sprintToggleMode) return;

        if (hold)
            TryStartSprint();
        else
            StopSprint();
    }

    public void SetLookInput(Vector2 input) => lookInput = input;

    public void RequestJump()
    {
        jumpBufferTimer = jumpBufferTime;
    }

    private void TryStartSprint()
    {
        if (cooldownTimer > 0f) return; // still cooling down
        if (isSprinting) return;

        isSprinting = true;
        sprintTimer = sprintDuration;
    }

    private void StopSprint()
    {
        if (!isSprinting) return;
        isSprinting = false;
        cooldownTimer = sprintCooldown;
    }

    private void Update()
    {
        // Timers for sprint duration and cooldown
        if (isSprinting)
        {
            sprintTimer -= Time.deltaTime;
            if (sprintTimer <= 0f)
            {
                StopSprint();
            }
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer < 0f) cooldownTimer = 0f;
        }

        // Yaw (horizontal) rotation of the body. Done in Update (render framerate),
        // NOT FixedUpdate, otherwise mouse deltas captured per-frame get applied at the
        // fixed timestep rate and you get stutter/jitter on the look.
        float yawDegrees = lookInput.x * lookSpeed * Time.deltaTime * mouseSensitivity;
        transform.Rotate(0f, yawDegrees, 0f);
    }

    private void LateUpdate()
    {
        // Pitch (vertical) for the camera. LateUpdate so it applies after any camera-follow
        // logic and right before rendering -> smoothest possible result.
        if (cameraTransform != null)
        {
            verticalRotation -= lookInput.y * lookSpeed * Time.deltaTime * mouseSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -89f, 89f);
            cameraTransform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
    }

    private void FixedUpdate()
    {
        // Ground check (fiable, indépendant des collisions physiques)
        isGrounded = groundCheck != null &&
            Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.fixedDeltaTime;
        jumpBufferTimer -= Time.fixedDeltaTime;

        // Movement (horizontal)
        float currentSpeed = isSprinting ? sprintSpeed : baseSpeed;
        Vector3 desiredLocal = new Vector3(moveInput.x, 0f, moveInput.y) * currentSpeed;
        Vector3 desiredWorld = transform.TransformDirection(desiredLocal);

        float verticalVelocity = rb.linearVelocity.y;

        // Jump : buffered + coyote time -> réactif comme dans un FPS AAA
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            float gravityMagnitude = Mathf.Abs(Physics.gravity.y);
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravityMagnitude);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f; // empêche le double saut
        }

        // Chute renforcée -> plus "punchy", moins flottant
        if (verticalVelocity < 0f)
            verticalVelocity += Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector3(desiredWorld.x, verticalVelocity, desiredWorld.z);
    }
}