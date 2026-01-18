using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ClientReadyReporter : MonoBehaviour
{
    private IEnumerator Start()
    {
        PlayerAvatar local;

        while (!PlayerAvatar.TryGetLocal(out local))
            yield return null;

        while (local.SteamId == 0 || local.SeatIndex < 0)
            yield return null;

        var auto = FindFirstObjectByType<AutoStartGameSession>();
        if (auto != null)
        {
            Debug.Log("[ClientReady] READY sent");
            auto.Server_ClientReady(local.SteamId);
        }
    }

}
