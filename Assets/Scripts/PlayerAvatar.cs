using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAvatar : PlayerIdentity<PlayerAvatar>
{
    //public SyncVar<int> seatIndex = new(-1);

    //public int SeatIndex => seatIndex.value;

    //public void Server_SetSeat(int seat)
    //{
    //    seatIndex.value = seat;
    //}

    public SyncVar<int> seatIndex = new(-1);
    public SyncVar<string> displayName = new("Player");

    public SyncVar<ulong> steamId = new(0);

    public int SeatIndex => seatIndex.value;
    public string DisplayName => displayName.value;
    public ulong SteamId => steamId.value;

    public void Server_SetSeat(int seat) => seatIndex.value = seat;
    public void Server_SetName(string name) => displayName.value = name;

    public void Server_SetSteamId(ulong id) => steamId.value = id;

    protected override void OnSpawned(bool asServer)
    {
        if (asServer) return;

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
    var steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
    var name = Steamworks.SteamFriends.GetPersonaName();

    Server_SetSteamData(steamId, name);
#endif
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_SetSteamData(ulong id, string name, RPCInfo info = default)
    {
        steamId.value = id;
        displayName.value = string.IsNullOrWhiteSpace(name) ? "Player" : name;
    }
}
