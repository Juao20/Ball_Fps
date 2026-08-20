using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerShoot shooter;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private Transform playerCamera;

    private InputSystem_Actions inputActions;
    private bool cursorLocked = true;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void OnDestroy()
    {
        inputActions.Dispose();
    }

    private void Start()
    {
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyCursorState();
        }
    }

    private void OnEnable()
    {
        inputActions.Player.AddCallbacks(this);
        inputActions.Player.Enable();
        ApplyCursorState();
    }

    private void OnDisable()
    {
        inputActions.Player.RemoveCallbacks(this);
        inputActions.Player.Disable();
        ShowCursor();
    }

    private void ApplyCursorState()
    {
        NetworkIdentity identity = GetComponent<NetworkIdentity>();
        if (identity == null || !identity.isLocalPlayer)
        {
            ShowCursor();
            return;
        }

        LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    private void ToggleCursor()
    {
        if (cursorLocked)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (cursorLocked == false) return;
        if (motor == null) return;
        motor.SetMoveInput(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (cursorLocked == false) return;
        if (motor == null) return;
        motor.SetLookInput(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (motor == null) return;

        if (context.performed)
            motor.RequestJump();
    }

    // Unused player callbacks (required by interface)
    public void OnFire(InputAction.CallbackContext context)
    {
        if (shooter == null) return;

        if (context.performed)
            shooter.Shoot();
    }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (motor == null) return;

        if (context.performed)
            motor.SetCrouchHeld(true);
        else if (context.canceled)
            motor.SetCrouchHeld(false);
    }
    public void OnToogleCursor(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ToggleCursor();
    }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnAim(InputAction.CallbackContext context)
    {
        if (aim == null || motor == null) return;

        if (context.performed)
        {
            aim.SetAiming(true);
            motor.SetAiming(true);
        }
        else if (context.canceled)
        {
            aim.SetAiming(false);
            motor.SetAiming(false);
        }
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (motor == null) return;

        // Toggle mode: activate on performed (press) only
        if (motor.sprintToggleMode)
        {
            if (context.performed)
                motor.ToggleSprint();
        }
        else
        {
            // Hold mode: start on performed, stop on canceled
            if (context.performed)
                motor.SetSprintHold(true);
            else if (context.canceled)
                motor.SetSprintHold(false);
        }
    }
}
