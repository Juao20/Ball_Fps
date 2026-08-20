using UnityEngine;

// Tags a NetworkStartPosition with a team for Team Deathmatch spawn filtering.
// team = 0 means the point is shared/used in Deathmatch and as a fallback.
public class SpawnPointTeam : MonoBehaviour
{
    public int team = 0;
}
