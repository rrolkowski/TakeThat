using UnityEngine;
using UnityEngine.SceneManagement; // Potrzebne do wyjścia klienta
using UnityEngine.InputSystem;
using PurrNet;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseRoot;

    [Header("Buttons")]
    [SerializeField] private GameObject lobbyButtonHost;   // Podepnij przycisk "Return to Lobby" (dla Hosta)
    [SerializeField] private GameObject lobbyButtonClient; // Podepnij przycisk "Leave Game" (dla Klienta)

    [SerializeField] private string lobbySceneName = "LobbySample";

    private void Start()
    {
        pauseRoot.SetActive(false);
        GameSession.OnGameOverClient += HandleGameOver;
    }

    private void OnDestroy()
    {
        GameSession.OnGameOverClient -= HandleGameOver;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Jeśli gra się skończyła, nie otwieramy pauzy
        if (GameSession.Instance != null && GameSession.Instance.IsGameOverClient)
        {
            if (pauseRoot.activeSelf) pauseRoot.SetActive(false);
            return;
        }

        bool isOpen = !pauseRoot.activeSelf;
        pauseRoot.SetActive(isOpen);

        if (isOpen)
        {
            // Sprawdzamy czy jesteśmy serwerem (Hostem)
            bool isHost = NetworkManager.main != null && NetworkManager.main.isServer;

            if (lobbyButtonHost != null) lobbyButtonHost.SetActive(isHost);
            if (lobbyButtonClient != null) lobbyButtonClient.SetActive(!isHost);
        }
    }

    private void HandleGameOver(int winnerSeatIndex)
    {
        pauseRoot.SetActive(false);
    }

    // HOST
    public void OnReturnToLobbyClicked()
    {
        GameSession.Instance?.Server_ReturnToLobby();
        pauseRoot.SetActive(false);
    }

    // KLIENT
    public void OnLeaveGameClicked()
    {
        SceneManager.LoadScene(lobbySceneName);
    }

    public void Resume()
    {
        pauseRoot.SetActive(false);
    }
}