using Mirror;
using UnityEngine;

public enum BodyPose : byte { Standing, Crouching, Sliding, Dead }

// Replicates the crouch/slide body shape to ALL clients, not just the owner. PlayerMotor
// (which decides the pose) only runs on the owning client, so without this, other clients
// would never see a remote player crouch or slide - only the owner would see their own.
public class PlayerBodyVisual : NetworkBehaviour
{
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Transform bodyGraphics;
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private float crouchHeight = 1f;
    [Tooltip("Forward lean applied to the body while sliding, so it reads as a slide from the outside.")]
    [SerializeField] private float slideTiltAngle = 25f;
    [Tooltip("Vitesse de transition vers la position couchée à la mort - plus rapide que le lerp normal pour que ça bascule vite.")]
    [SerializeField] private float deathLerpSpeed = 25f;

    [SyncVar(hook = nameof(OnPoseChanged))]
    private BodyPose pose = BodyPose.Standing;

    private float standHeight;
    private Vector3 standCenter;
    private Vector3 crouchCenter;

    private Vector3 standGraphicsScale = Vector3.one;
    private Vector3 standGraphicsLocalPos;
    private Quaternion standGraphicsLocalRot = Quaternion.identity;
    private Vector3 crouchGraphicsScale = Vector3.one;
    private Vector3 crouchGraphicsLocalPos;
    private Quaternion slideGraphicsLocalRot = Quaternion.identity;
    private Quaternion deathGraphicsLocalRot = Quaternion.identity;

    private void Awake()
    {
        if (capsule != null)
        {
            standHeight = capsule.height;
            standCenter = capsule.center;
            // Keep the feet planted: only the top comes down, so shift the center down
            // by half the height that gets removed.
            crouchCenter = standCenter + Vector3.down * ((standHeight - crouchHeight) * 0.5f);
        }

        if (bodyGraphics != null)
        {
            standGraphicsScale = bodyGraphics.localScale;
            standGraphicsLocalPos = bodyGraphics.localPosition;
            standGraphicsLocalRot = bodyGraphics.localRotation;

            float heightRatio = standHeight > 0f ? crouchHeight / standHeight : 1f;
            crouchGraphicsScale = new Vector3(standGraphicsScale.x, standGraphicsScale.y * heightRatio, standGraphicsScale.z);
            crouchGraphicsLocalPos = standGraphicsLocalPos + Vector3.down * ((standHeight - crouchHeight) * 0.5f);
            slideGraphicsLocalRot = Quaternion.Euler(slideTiltAngle, 0f, 0f) * standGraphicsLocalRot;
            deathGraphicsLocalRot = Quaternion.Euler(90f, 0f, 0f) * standGraphicsLocalRot;
        }
    }

    // Called by PlayerMotor on the owning client whenever its motion state changes.
    public void SetPose(BodyPose newPose)
    {
        if (!isLocalPlayer) return;
        if (pose == newPose) return;
        CmdSetPose(newPose);
    }

    [Server]
    public void SetDeadPose(bool isDead)
    {
        pose = isDead ? BodyPose.Dead : BodyPose.Standing;
    }

    [Command]
    private void CmdSetPose(BodyPose newPose)
    {
        pose = newPose;
    }

    private void OnPoseChanged(BodyPose oldPose, BodyPose newPose) { }

    private void Update()
    {
        bool crouchedShape = pose == BodyPose.Crouching || pose == BodyPose.Sliding || pose == BodyPose.Dead;
        float currentLerpSpeed = pose == BodyPose.Dead ? deathLerpSpeed : lerpSpeed;

        if (capsule != null)
        {
            float targetHeight = crouchedShape ? crouchHeight : standHeight;
            Vector3 targetCenter = crouchedShape ? crouchCenter : standCenter;
            capsule.height = Mathf.Lerp(capsule.height, targetHeight, currentLerpSpeed * Time.deltaTime);
            capsule.center = Vector3.Lerp(capsule.center, targetCenter, currentLerpSpeed * Time.deltaTime);
        }

        if (bodyGraphics != null)
        {
            Vector3 targetScale = crouchedShape ? crouchGraphicsScale : standGraphicsScale;
            Vector3 targetPos = crouchedShape ? crouchGraphicsLocalPos : standGraphicsLocalPos;
            Quaternion targetRot = pose switch
            {
                BodyPose.Sliding => slideGraphicsLocalRot,
                BodyPose.Dead => deathGraphicsLocalRot,
                _ => standGraphicsLocalRot
            };

            bodyGraphics.localScale = Vector3.Lerp(bodyGraphics.localScale, targetScale, currentLerpSpeed * Time.deltaTime);
            bodyGraphics.localPosition = Vector3.Lerp(bodyGraphics.localPosition, targetPos, currentLerpSpeed * Time.deltaTime);
            bodyGraphics.localRotation = Quaternion.Slerp(bodyGraphics.localRotation, targetRot, currentLerpSpeed * Time.deltaTime);
        }
    }
}