using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using PurrNet;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseRoot;

    [Header("Buttons")]
    [SerializeField] private GameObject lobbyButtonHost;
    [SerializeField] private GameObject lobbyButtonClient;

    [SerializeField] private string lobbySceneName = "LobbySample";

    [Header("Disable input raycasters while paused")]
    [SerializeField] private ClickRaycaster clickRaycaster;
    [SerializeField] private HoverRaycaster hoverRaycaster;

    private void Start()
    {
        pauseRoot.SetActive(false);
        SetRaycastersEnabled(true);

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
            SetRaycastersEnabled(true);
            return;
        }

        bool isOpen = !pauseRoot.activeSelf;
        pauseRoot.SetActive(isOpen);

        SetRaycastersEnabled(!isOpen);

        if (isOpen)
        {
            bool isHost = NetworkManager.main != null && NetworkManager.main.isServer;

            if (lobbyButtonHost != null) lobbyButtonHost.SetActive(isHost);
            if (lobbyButtonClient != null) lobbyButtonClient.SetActive(!isHost);
        }
    }

    private void HandleGameOver(int winnerSeatIndex)
    {
        pauseRoot.SetActive(false);
        SetRaycastersEnabled(true);
    }

    private void SetRaycastersEnabled(bool enabled)
    {
        if (clickRaycaster != null) clickRaycaster.enabled = enabled;
        if (hoverRaycaster != null) hoverRaycaster.enabled = enabled;

        // Jeśli wyłączasz hover, to warto też zdjąć podświetlenie:
        if (!enabled && hoverRaycaster != null)
        {
            var hr = hoverRaycaster as HoverRaycaster;
            if (hr != null) hr.ForceClearHover();
        }
    }

    // HOST
    public void OnReturnToLobbyClicked()
    {
        GameSession.Instance?.Server_ReturnToLobby();
        pauseRoot.SetActive(false);
        SetRaycastersEnabled(true);
    }

    // KLIENT
    public void OnLeaveGameClicked()
    {
        var nm = NetworkManager.main;
        if (nm != null && nm.isClient)
            nm.StopClient();

        SetRaycastersEnabled(true);
        SceneManager.LoadScene(lobbySceneName);
    }

    public void Resume()
    {
        pauseRoot.SetActive(false);
        SetRaycastersEnabled(true);
    }
}
