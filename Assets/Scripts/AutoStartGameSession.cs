using PurrNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoStartGameSession : NetworkBehaviour
{
    //[SerializeField] private int minPlayers = 1;

    //protected override void OnSpawned(bool asServer)
    //{
    //    if (!asServer) return;
    //    StartCoroutine(Co());
    //}

    //private IEnumerator Co()
    //{
    //    while (PlayerAvatar.allPlayers == null)
    //    {
    //        Debug.Log("[AutoStart] allPlayers null");
    //        yield return null;
    //    }

    //    while (PlayerAvatar.allPlayers.Count < minPlayers)
    //    {
    //        Debug.Log("[AutoStart] waiting, players count=" + PlayerAvatar.allPlayers.Count);
    //        yield return null;
    //    }

    //    Debug.Log("[AutoStart] starting game, count=" + PlayerAvatar.allPlayers.Count);
    //    GameSession.Instance.Server_StartGame();
    //}
    private readonly HashSet<ulong> expected = new();
    private readonly HashSet<ulong> ready = new();
    private bool started;

    protected override void OnSpawned(bool asServer)
    {
        if (!asServer) return;

        expected.Clear();
        ready.Clear();
        started = false;

        var holder = FindFirstObjectByType<PurrLobby.LobbyDataHolder>();
        if (holder != null && holder.ExpectedSteamIds != null)
            foreach (var sid in holder.ExpectedSteamIds)
                expected.Add(sid);

        Debug.Log($"[AutoStart] expected={expected.Count}");
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_ClientReady(ulong steamId, RPCInfo info = default)
    {
        if (started) return;
        if (steamId == 0) return;

        if (expected.Count > 0 && !expected.Contains(steamId))
            return;

        ready.Add(steamId);
        TryStart();
    }

    private void TryStart()
    {
        if (started) return;

        if (expected.Count > 0 && ready.Count != expected.Count)
            return;

        foreach (var kv in PlayerAvatar.allPlayers)
        {
            var a = kv.Value;
            if (a == null) continue;

            if (expected.Count == 0 || expected.Contains(a.SteamId))
            {
                if (a.SteamId == 0) return;
                if (a.SeatIndex < 0) return;
            }
        }

        started = true;
        Debug.Log("[AutoStart] ALL READY -> Server_StartGame()");
        GameSession.Instance.Server_StartGame();
    }
}

