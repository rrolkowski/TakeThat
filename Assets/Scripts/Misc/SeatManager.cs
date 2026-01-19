using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SeatManager : NetworkBehaviour
{
    [SerializeField] private int seatCount = 4;

    private readonly Dictionary<PlayerID, int> assigned = new();
    private bool[] taken;

    protected override void OnSpawned(bool asServer)
    {
        if (!asServer) return;
        taken = new bool[seatCount];
        assigned.Clear();
    }

    private void Update()
    {
        if (!isServer) return;

        foreach (var kv in PlayerAvatar.allPlayers)
        {
            var playerId = kv.Key;
            var avatar = kv.Value;

            if (assigned.ContainsKey(playerId)) continue;

            int seat = FindFreeSeat();
            if (seat < 0) return;

            assigned[playerId] = seat;
            taken[seat] = true;

            avatar.Server_SetSeat(seat);
        }
    }

    private int FindFreeSeat()
    {
        for (int i = 0; i < taken.Length; i++)
            if (!taken[i]) return i;
        return -1;
    }
}