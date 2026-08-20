using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

// Custom NetworkManager: adds anti-spawn-camping and team-aware spawn point selection
// on top of Mirror's default GetStartPosition(). Used for both initial join
// (base.OnServerAddPlayer calls GetStartPosition()) and respawn (PlayerStats calls it
// directly, or GetTeamSpawnPosition() once a player's team is known).
public class FpsNetworkManager : NetworkManager
{
    public override Transform GetStartPosition()
    {
        List<Transform> candidates = startPositions.Where(t => t != null).ToList();
        if (candidates.Count == 0)
            return base.GetStartPosition();

        return PickFarthestFromEnemies(candidates, 0);
    }

    public Transform GetTeamSpawnPosition(int team)
    {
        List<Transform> all = startPositions.Where(t => t != null).ToList();
        if (all.Count == 0) return base.GetStartPosition();

        List<Transform> teamCandidates = all
            .Where(t => t.TryGetComponent(out SpawnPointTeam spt) && spt.team == team)
            .ToList();

        List<Transform> candidates = teamCandidates.Count > 0 ? teamCandidates : all;
        return PickFarthestFromEnemies(candidates, team);
    }

    // Picks the spawn point whose nearest enemy player is as far away as possible.
    private Transform PickFarthestFromEnemies(List<Transform> candidates, int forTeam)
    {
        if (candidates.Count == 1) return candidates[0];

        bool isTeamMode = GameModeManager.Instance != null && GameModeManager.Instance.currentMode == GameMode.TeamDeathmatch;

        Transform best = candidates[0];
        float bestDistance = float.NegativeInfinity;

        foreach (Transform candidate in candidates)
        {
            float nearestEnemyDist = float.PositiveInfinity;

            foreach (PlayerStats p in PlayerStats.Players)
            {
                if (p == null) continue;
                if (isTeamMode && forTeam != 0 && p.team == forTeam) continue; // skip teammates

                float d = Vector3.Distance(candidate.position, p.transform.position);
                if (d < nearestEnemyDist) nearestEnemyDist = d;
            }

            if (nearestEnemyDist > bestDistance)
            {
                bestDistance = nearestEnemyDist;
                best = candidate;
            }
        }

        return best;
    }
}
