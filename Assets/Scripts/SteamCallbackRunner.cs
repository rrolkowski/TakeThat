using Steamworks;
using UnityEngine;

public class SteamCallbackRunner : MonoBehaviour
{
    private static SteamCallbackRunner instance;
    private bool warned;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!SteamAPI.IsSteamRunning()) return;

        try
        {
            SteamAPI.RunCallbacks();
        }
        catch (System.Exception e)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning($"SteamAPI.RunCallbacks failed (expected in multi-instance/editor): {e.Message}");
            }
        }
    }
}
