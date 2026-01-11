using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class PlayerSpawnerTT : NetworkBehaviour
{
    [SerializeField] private NetworkIdentity playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<PlayerID, NetworkIdentity> spawned = new();
    private int nextSeat;

    protected override void OnSpawned(bool asServer)
    {
        Debug.Log($"[PlayerSpawnerTT] OnSpawned asServer={asServer}");
        if (!asServer) return;
        StartCoroutine(Server_SpawnWhenPlayersReady());
    }

    private System.Collections.IEnumerator Server_SpawnWhenPlayersReady()
    {
        Debug.Log($"[PlayerSpawnerTT] allPlayers.Count={PlayerAvatar.allPlayers.Count}");

        yield return null;
        yield return null;

        foreach (var kv in PlayerAvatar.allPlayers)
            TrySpawnFor(kv.Key);
    }

    public void TrySpawnFor(PlayerID playerId)
    {
        if (spawned.ContainsKey(playerId)) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int seat = nextSeat % spawnPoints.Length;
        nextSeat++;

        var t = spawnPoints[seat];
        var obj = Instantiate(playerPrefab, t.position, t.rotation);

        obj.GiveOwnership(playerId);
        spawned[playerId] = obj;

        if (obj.TryGetComponent<PlayerAvatar>(out var avatar))
            avatar.Server_SetSeat(seat);
    }
}

