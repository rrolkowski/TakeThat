using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PurrLobby
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [PurrScene, SerializeField] private string nextScene;

        public void SwitchScene()
        {
            var holder = FindFirstObjectByType<LobbyDataHolder>();
            holder?.SnapshotExpectedFromCurrentLobby();

            lobbyManager.SetLobbyStarted();
            SceneManager.LoadSceneAsync(nextScene);
        }
    }
}
