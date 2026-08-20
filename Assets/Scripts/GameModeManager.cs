using Mirror;
using UnityEngine;

public enum GameMode { Deathmatch, TeamDeathmatch }

// Server-authoritative match rules: team assignment, friendly fire, score limit / match end.
// Lives on its own scene GameObject with a NetworkIdentity so its SyncVars replicate to clients.
public class GameModeManager : NetworkBehaviour
{
    public static GameModeManager Instance { get; private set; }

    // Set by the host-side mode selection UI before StartHost() is called.
    public static GameMode SelectedMode = GameMode.Deathmatch;

    [SyncVar] public GameMode currentMode;
    [SerializeField] private int scoreLimit = 30;

    private int redCount;
    private int blueCount;
    private bool matchEnded;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        currentMode = SelectedMode;
        matchEnded = false;
        redCount = 0;
        blueCount = 0;
    }

    [Server]
    public int AssignTeam(PlayerStats player)
    {
        if (currentMode != GameMode.TeamDeathmatch)
            return 0;

        int team;
        if (redCount <= blueCount)
        {
            team = 1;
            redCount++;
        }
        else
        {
            team = 2;
            blueCount++;
        }
        return team;
    }

    [Server]
    public void RemovePlayer(PlayerStats player)
    {
        if (player.team == 1) redCount = Mathf.Max(0, redCount - 1);
        else if (player.team == 2) blueCount = Mathf.Max(0, blueCount - 1);
    }

    [Server]
    public bool IsFriendlyFire(PlayerStats attacker, PlayerStats target)
    {
        if (currentMode != GameMode.TeamDeathmatch) return false;
        if (attacker == null || target == null || attacker == target) return false;
        return attacker.team == target.team;
    }

    [Server]
    public void CheckScoreLimit(PlayerStats scorer)
    {
        if (matchEnded || scorer == null) return;

        if (scorer.kills >= scoreLimit)
        {
            matchEnded = true;
            RpcMatchEnded($"{scorer.playerName} a gagné la partie !");
        }
    }

    [ClientRpc]
    private void RpcMatchEnded(string winnerText)
    {
        PlayerHUD.ShowMatchEnd(winnerText);
    }
}
