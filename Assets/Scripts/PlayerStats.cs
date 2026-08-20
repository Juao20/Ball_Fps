using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public static readonly List<PlayerStats> Players = new List<PlayerStats>();
    public static event System.Action ScoreboardChanged;

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float respawnDelay = 3f;

    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player";

    [SyncVar(hook = nameof(OnKillsChanged))]
    public int kills;

    [SyncVar(hook = nameof(OnDeathsChanged))]
    public int deaths;

    [SyncVar]
    public int team; // 0 = no team (Deathmatch), 1 = Red, 2 = Blue (Team Deathmatch)

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public event System.Action<int, int> OnHealthUpdated;

    private PlayerController controller;
    private PlayerMotor motor;
    private PlayerBodyVisual bodyVisual;
    private Collider[] colliders;
    private Rigidbody rb;
    private NetworkTransformUnreliable networkTransform;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        motor = GetComponent<PlayerMotor>();
        bodyVisual = GetComponent<PlayerBodyVisual>();
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        networkTransform = GetComponent<NetworkTransformUnreliable>();
    }

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        if (GameModeManager.Instance != null)
            team = GameModeManager.Instance.AssignTeam(this);
    }

    public override void OnStopServer()
    {
        GameModeManager.Instance?.RemovePlayer(this);
    }

    public override void OnStartClient()
    {
        Players.Add(this);
        ScoreboardChanged?.Invoke();
    }

    public override void OnStopClient()
    {
        Players.Remove(this);
        ScoreboardChanged?.Invoke();
    }

    public override void OnStartLocalPlayer()
    {
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        string finalName = string.IsNullOrWhiteSpace(savedName) ? ("Player " + netId) : savedName.Trim();
        CmdSetName(finalName);
        PlayerHUD.Bind(this);
    }

    [Command]
    private void CmdSetName(string requestedName)
    {
        playerName = requestedName;
    }

    [Server]
    public bool TakeDamage(float amount, NetworkConnectionToClient attacker)
    {
        if (currentHealth <= 0) return false;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));

        bool isKill = currentHealth <= 0;
        if (isKill)
        {
            Die(attacker);
        }

        return isKill;
    }

    [Server]
    private void Die(NetworkConnectionToClient attacker)
    {
        deaths++;

        if (attacker != null && attacker.identity != null)
        {
            PlayerStats attackerStats = attacker.identity.GetComponent<PlayerStats>();
            if (attackerStats != null && attackerStats != this)
            {
                attackerStats.kills++;
                GameModeManager.Instance?.CheckScoreLimit(attackerStats);
            }
        }

        SetAliveState(false);
        bodyVisual?.SetDeadPose(true);
        RpcOnDeath(respawnDelay);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        bool isTeamMode = GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameMode.TeamDeathmatch;
        Transform spawn = isTeamMode && NetworkManager.singleton is FpsNetworkManager fpsManager
            ? fpsManager.GetTeamSpawnPosition(team)
            : NetworkManager.singleton.GetStartPosition();

        if (spawn != null)
        {
            // Teleport instead of a raw transform set: the NetworkTransform interpolates
            // by default, which would make the player visibly slide to the spawn point.
            if (networkTransform != null)
                networkTransform.ServerTeleport(spawn.position, spawn.rotation);
            else
                transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentHealth = maxHealth;
        SetAliveState(true);
        bodyVisual?.SetDeadPose(false);
        RpcOnRespawn();
    }

    [Server]
    private void SetAliveState(bool alive)
    {
        if (rb != null) rb.isKinematic = !alive;
        RpcSetAliveState(alive);
    }

    [ClientRpc]
    private void RpcSetAliveState(bool alive)
    {
        // Input/movement components must only ever be (re-)enabled for the local player's own
        // object. Enabling them for a remote player's object would make it react to the local
        // player's own keyboard/mouse input on every client watching that remote player.
        bool controlsEnabled = alive && isLocalPlayer;
        if (controller != null) controller.enabled = controlsEnabled;
        if (motor != null) motor.enabled = controlsEnabled;

        // Colliders reflect physical presence and must match the alive state for everyone.
        foreach (Collider c in colliders)
            if (c != null) c.enabled = alive;
    }

    [ClientRpc]
    private void RpcOnDeath(float delay)
    {
        if (isLocalPlayer) PlayerHUD.ShowRespawnMessage(true, delay);
    }

    [ClientRpc]
    private void RpcOnRespawn()
    {
        if (isLocalPlayer)
        {
            PlayerHUD.ShowRespawnMessage(false, 0);
            motor?.ResetLook();
        }
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        OnHealthUpdated?.Invoke(newVal, maxHealth);
    }

    private void OnNameChanged(string oldVal, string newVal)
    {
        ScoreboardChanged?.Invoke();
    }

    private void OnKillsChanged(int oldVal, int newVal)
    {
        ScoreboardChanged?.Invoke();
    }

    private void OnDeathsChanged(int oldVal, int newVal)
    {
        ScoreboardChanged?.Invoke();
    }
}