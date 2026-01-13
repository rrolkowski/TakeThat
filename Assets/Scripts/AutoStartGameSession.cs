using PurrNet;
using System.Collections;
using UnityEngine;

public class AutoStartGameSession : NetworkBehaviour
{
    [SerializeField] private int minPlayers = 1;

    protected override void OnSpawned(bool asServer)
    {
        if (!asServer) return;
        StartCoroutine(Co());
    }

    private IEnumerator Co()
    {
        while (PlayerAvatar.allPlayers == null)
        {
            Debug.Log("[AutoStart] allPlayers null");
            yield return null;
        }

        while (PlayerAvatar.allPlayers.Count < minPlayers)
        {
            Debug.Log("[AutoStart] waiting, players count=" + PlayerAvatar.allPlayers.Count);
            yield return null;
        }

        Debug.Log("[AutoStart] starting game, count=" + PlayerAvatar.allPlayers.Count);
        GameSession.Instance.Server_StartGame();
    }
}

