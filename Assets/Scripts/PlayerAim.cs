using UnityEngine;
using Mirror;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float fovLerpSpeed = 12f;

    public bool IsAiming { get; private set; }

    // The local player's own FPS camera, exposed for other systems (e.g. teammate markers)
    // that need to project world positions to screen space without relying on Camera.main
    // (no camera in this project is tagged MainCamera).
    public static Camera LocalCamera { get; private set; }

    private void Start()
    {
        NetworkIdentity identity = GetComponent<NetworkIdentity>();
        if (identity != null && !identity.isLocalPlayer)
        {
            enabled = false;
            return;
        }

        if (cam != null)
        {
            defaultFOV = cam.fieldOfView;
            LocalCamera = cam;
        }
    }

    private void OnDestroy()
    {
        if (LocalCamera == cam)
            LocalCamera = null;
    }

    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;
    }

    private void Update()
    {
        if (cam == null) return;

        float targetFOV = IsAiming ? aimFOV : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }
}
