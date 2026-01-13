using PurrNet;
using PurrNet.Transports;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGameOnConnected : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private string gameSceneName = "SampleScene";

    private bool _started;

    private void Awake()
    {
        if (networkManager == null) networkManager = NetworkManager.main;

        StartCoroutine(WaitForNMThenSubscribe());
    }

    private IEnumerator WaitForNMThenSubscribe()
    {
        while (networkManager == null)
        {
            networkManager = NetworkManager.main;
            yield return null;
        }

        networkManager.onClientConnectionState += OnClientState;
        Debug.Log("[SceneLoad] Subscribed to onClientConnectionState");
    }

    private void OnDestroy()
    {
        if (networkManager != null)
            networkManager.onClientConnectionState -= OnClientState;
    }

    private void OnClientState(ConnectionState state)
    {
        Debug.Log($"[SceneLoad] state={state} isServer={networkManager.isServer} isClient={networkManager.isClient}");

        if (state != ConnectionState.Connected) return;

        if (_started) return;

        // tylko host/serwer mo¿e zmieniæ scenê
        if (!networkManager.isServer) return;

        _started = true;
        Debug.Log($"[NM] name={networkManager.name} scene={networkManager.gameObject.scene.name} dontDestroyFlag? (check inspector) ");
        Debug.Log("[SceneLoad] Host calling sceneModule.LoadSceneAsync...");
        networkManager.sceneModule.LoadSceneAsync(gameSceneName);
    }


}
