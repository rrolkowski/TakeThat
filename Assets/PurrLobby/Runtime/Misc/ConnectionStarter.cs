using System.Collections;
using PurrNet;
using PurrNet.Logging;
using PurrNet.Transports;
using UnityEngine;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

namespace PurrLobby
{
    public class ConnectionStarter : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private LobbyDataHolder _lobbyDataHolder;

        private void Awake()
        {
            if (!TryGetComponent(out _networkManager))
            {
                PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
            }

            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!_lobbyDataHolder)
                PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);
        }

        private void Start()
        {
            if (!_networkManager)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
                return;
            }

            if (!_lobbyDataHolder)
            {
                PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
                return;
            }

            if (!_lobbyDataHolder.CurrentLobby.IsValid)
            {
                PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
                return;
            }

            if (_networkManager.transport is PurrTransport purrTransport)
            {
                purrTransport.roomName = _lobbyDataHolder.CurrentLobby.LobbyId;
            }

#if UTP_LOBBYRELAY
            // --- UTP Relay Transport ---
            else if (_networkManager.transport is UTPTransport utpTransport)
            {
                if (_lobbyDataHolder.CurrentLobby.IsOwner)
                {
                    utpTransport.InitializeRelayServer((Allocation)_lobbyDataHolder.CurrentLobby.ServerObject);
                }

                utpTransport.InitializeRelayClient(_lobbyDataHolder.CurrentLobby.Properties["JoinCode"]);
            }
#else
            else if (_networkManager.transport != null && _networkManager.transport.GetType().Name == "SteamTransport")
            {
                if (_lobbyDataHolder.CurrentLobby.IsOwner)
                {
                    _networkManager.StartHost();
                    return;
                }

                if (_lobbyDataHolder.CurrentLobby.Properties == null ||
                    !_lobbyDataHolder.CurrentLobby.Properties.TryGetValue("HostSteamId", out var hostSteamId) ||
                    string.IsNullOrWhiteSpace(hostSteamId))
                {
                    PurrLogger.LogError("Missing HostSteamId in lobby properties. Client cannot connect via SteamTransport.", this);
                    return;
                }

                var transport = _networkManager.transport;
                var t = transport.GetType();

                var addrField = t.GetField("address");
                if (addrField != null)
                {
                    addrField.SetValue(transport, hostSteamId);
                }
                else
                {
                    var addrProp = t.GetProperty("address");
                    if (addrProp != null)
                    {
                        addrProp.SetValue(transport, hostSteamId);
                    }
                    else
                    {
                        PurrLogger.LogError("SteamTransport has no 'address' field/property.", this);
                        return;
                    }
                }

                StartCoroutine(StartClient());
                return;
            }
#endif

            if (_lobbyDataHolder.CurrentLobby.IsOwner)
                _networkManager.StartHost();
            else
                StartCoroutine(StartClient());
        }

        private IEnumerator StartClient()
        {
            yield return new WaitForSeconds(2f);
            _networkManager.StartClient();
        }
    }
}
