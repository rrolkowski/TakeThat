using UnityEngine;
using PurrNet;
using Steamworks;

[RequireComponent(typeof(PlayerAvatar))]
public class SteamIdentityReporter : MonoBehaviour
{
    private PlayerAvatar avatar;
    private bool sent;

    private void Awake()
    {
        avatar = GetComponent<PlayerAvatar>();
    }

    private void Update()
    {
        if (sent) return;

        if (!PlayerAvatar.TryGetLocal(out var local)) return;
        if (local != avatar) return;

        if (!SteamAPI.IsSteamRunning()) return;

        ulong id = SteamUser.GetSteamID().m_SteamID;
        string name = SteamFriends.GetPersonaName();

        avatar.Server_SetSteamData(id, name);
        sent = true;
    }
}
