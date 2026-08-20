using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private float Speed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float lookSpeed = 200f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 1f;
    [Tooltip("Replicates the crouch/slide body shape to all clients (not just the owner).")]
    [SerializeField] private PlayerBodyVisual bodyVisual;
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

    [Header("Crouch")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Camera height as a fraction of the current capsule height, measured from its bottom. Kept well under 1 so the camera never pokes through the top of the (possibly crouched) body mesh.")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float eyeHeightRatio = 0.75f;

    [Header("Slide")]
    [SerializeField] private float slideSpeedMultiplier = 1.8f;
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideFriction = 5f;
    [SerializeField] private float minSlideSpeed = 2f;
    [Tooltip("Minimum horizontal speed required to trigger a slide when crouch is pressed.")]
    [SerializeField] private float minSpeedToSlide = 5.5f;

    [Header("Aim")]
    [SerializeField] private float aimSpeedMultiplier = 0.6f;

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

    private enum MotionState { Normal, Crouching, Sliding }
    private MotionState motionState = MotionState.Normal;
    private bool crouchHeld = false;
    private Vector3 slideDirection;
    private float slideTimer;
    private Vector3 cameraStandLocalPos;
    private bool isAiming = false;

    private CapsuleCollider capsule;

    public bool IsSliding => motionState == MotionState.Sliding;
    public bool IsCrouching => motionState == MotionState.Crouching;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = 999f;
        rb.freezeRotation = true;
        baseSpeed = Speed;
        if (cameraTransform != null)
            cameraStandLocalPos = cameraTransform.localPosition;

        capsule = GetComponent<CapsuleCollider>();
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

        // Jump-cancel: bail out of a slide early into a normal jump/airborne state, and
        // jumping out of a regular crouch stands you back up (like most FPS games).
        if (motionState == MotionState.Sliding || motionState == MotionState.Crouching)
            motionState = MotionState.Normal;
    }

    // Crouch is held-to-crouch. A single press while moving fast enough (sprinting) triggers a
    // slide that plays out on its own (duration/friction) - no need to hold the key for it.
    // Releasing crouch only stands back up from a regular (non-slide) crouch; jumping is what
    // cancels a slide early (slide cancel).
    public void SetCrouchHeld(bool held)
    {
        if (held && !crouchHeld)
        {
            // Speed-based trigger (not the isSprinting flag directly): more forgiving and avoids
            // missing the slide window right as sprint toggles/times out.
            if (motionState == MotionState.Normal && isGrounded && HorizontalSpeed() >= minSpeedToSlide)
                StartSlide();
            else if (motionState == MotionState.Normal)
                motionState = MotionState.Crouching;
        }
        else if (!held && crouchHeld)
        {
            if (motionState == MotionState.Crouching)
                motionState = MotionState.Normal;
        }

        crouchHeld = held;
    }

    public void SetAiming(bool aiming) => isAiming = aiming;

    // Called after a respawn so the local camera doesn't stay pitched/crouched from before death.
    public void ResetLook()
    {
        verticalRotation = 0f;
        if (cameraTransform != null)
        {
            cameraTransform.localEulerAngles = Vector3.zero;
            cameraTransform.localPosition = cameraStandLocalPos;
        }
        motionState = MotionState.Normal;
        crouchHeld = false;
        bodyVisual?.SetPose(BodyPose.Standing);
    }

    private float HorizontalSpeed() => new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

    private void StartSlide()
    {
        motionState = MotionState.Sliding;
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        slideDirection = horizontalVel.sqrMagnitude > 0.01f ? horizontalVel.normalized : transform.forward;

        float boostedSpeed = Mathf.Max(horizontalVel.magnitude, sprintSpeed) * slideSpeedMultiplier;
        rb.linearVelocity = new Vector3(slideDirection.x * boostedSpeed, rb.linearVelocity.y, slideDirection.z * boostedSpeed);
        slideTimer = slideDuration;
    }

    private void TryStartSprint()
    {
        if (motionState != MotionState.Normal) return; // can't sprint while crouched/sliding
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

        // Push the current pose to PlayerBodyVisual so it replicates to every client (this
        // component only runs for the local player - PlayerBodyVisual runs for everyone).
        if (bodyVisual != null)
        {
            BodyPose pose = motionState switch
            {
                MotionState.Sliding => BodyPose.Sliding,
                MotionState.Crouching => BodyPose.Crouching,
                _ => BodyPose.Standing
            };
            bodyVisual.SetPose(pose);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // Pitch only rotates the camera's own transform, not the player body: the body
            // (and its movement direction) stays upright and only yaws, like a normal FPS.
            verticalRotation -= lookInput.y * lookSpeed * Time.deltaTime * mouseSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -79f, 79f);
            cameraTransform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);

            // Derive the camera's eye height directly from the capsule's current shape (which
            // PlayerBodyVisual animates every frame), at a fixed ratio up from its bottom. This
            // keeps the camera safely inside the body mesh at all times.
            if (capsule != null)
            {
                float bottom = capsule.center.y - capsule.height * 0.5f;
                float eyeY = bottom + capsule.height * eyeHeightRatio;
                cameraTransform.localPosition = new Vector3(cameraStandLocalPos.x, eyeY, cameraStandLocalPos.z);
            }
        }
    }

    private void FixedUpdate()
    {
        // Ground check (fiable, indépendant des collisions physiques)
        isGrounded = groundCheck != null &&
            Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Time.fixedDeltaTime;
        jumpBufferTimer -= Time.fixedDeltaTime;

        Vector3 desiredWorld;

        if (motionState == MotionState.Sliding)
        {
            // Slide: no steering, just decelerate along the slide direction via friction.
            float slideSpeed = Mathf.Max(0f, HorizontalSpeed() - slideFriction * Time.fixedDeltaTime);
            desiredWorld = slideDirection * slideSpeed;

            slideTimer -= Time.fixedDeltaTime;
            if (slideTimer <= 0f || slideSpeed <= minSlideSpeed)
                motionState = crouchHeld ? MotionState.Crouching : MotionState.Normal;
        }
        else
        {
            float currentSpeed = motionState == MotionState.Crouching
                ? baseSpeed * crouchSpeedMultiplier
                : (isSprinting ? sprintSpeed : baseSpeed);

            if (isAiming)
                currentSpeed *= aimSpeedMultiplier;

            Vector3 desiredLocal = new Vector3(moveInput.x, 0f, moveInput.y) * currentSpeed;
            desiredWorld = transform.TransformDirection(desiredLocal);
        }

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
    public void ResetVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
