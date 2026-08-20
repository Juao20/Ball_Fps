using System.Collections;
using Mirror;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    public PlayerWeapon currentWeapon;

    [SerializeField] private Camera cam;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LayerMask shootableLayerMask;
    [SerializeField] private PlayerAim playerAim;

    [Header("Accuracy")]
    [Tooltip("Half-angle (degrees) of the random spread cone when firing from the hip.")]
    [SerializeField] private float hipfireSpread = 3f;
    [Tooltip("Half-angle (degrees) of the random spread cone when aiming down sights.")]
    [SerializeField] private float adsSpread = 0.3f;

    [Header("Laser")]
    [SerializeField] private float laserDuration = 0.05f;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.02f;

    private PlayerStats selfStats;

    void Awake()
    {
        selfStats = GetComponent<PlayerStats>();
    }

    void Start()
    {
        if (cam == null)
        {
            Debug.LogError("Pas de camera assigné.");
            this.enabled = false;
        }
    }

    public void Shoot()
    {
        if (!isLocalPlayer) return;
        if (currentWeapon == null)
        {
            Debug.LogWarning("Pas d'arme assignée.");
            return;
        }

        CmdShoot(cam.transform.position, ApplySpread(cam.transform.forward));
    }

    private Vector3 ApplySpread(Vector3 direction)
    {
        bool isAiming = playerAim != null && playerAim.IsAiming;
        float spread = isAiming ? adsSpread : hipfireSpread;
        if (spread <= 0f) return direction;

        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f);
        return randomRotation * direction;
    }

    [Command]
    private void CmdShoot(Vector3 origin, Vector3 direction)
    {
        direction.Normalize();
        Vector3 endPoint = origin + direction * currentWeapon.range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, currentWeapon.range, shootableLayerMask))
        {
            endPoint = hit.point;

            if (hit.transform.root != transform)
            {
                PlayerStats targetStats = hit.collider.GetComponentInParent<PlayerStats>();
                bool friendlyFire = GameModeManager.Instance != null && GameModeManager.Instance.IsFriendlyFire(selfStats, targetStats);
                if (targetStats != null && !friendlyFire)
                {
                    bool isKill = targetStats.TakeDamage(currentWeapon.damage, connectionToClient);
                    TargetShowHitMarker(isKill);
                }
            }
        }

        Vector3 laserStart = muzzle != null ? muzzle.position : origin;
        RpcShowLaser(laserStart, endPoint);
    }

    [ClientRpc]
    private void RpcShowLaser(Vector3 start, Vector3 end)
    {
        StartCoroutine(DrawLaser(start, end));
    }

    [TargetRpc]
    private void TargetShowHitMarker(bool isKill)
    {
        PlayerHUD.ShowHitMarker(isKill);
    }

    private IEnumerator DrawLaser(Vector3 start, Vector3 end)
    {
        GameObject laserObj = new GameObject("Laser");
        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = laserColor;
        lr.endColor = laserColor;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        yield return new WaitForSeconds(laserDuration);
        Destroy(laserObj);
    }
}