using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyDataHolder : MonoBehaviour
    {
        [SerializeField] private Lobby serializedLobby;
        public List<ulong> ExpectedSteamIds { get; private set; } = new();
        public Lobby CurrentLobby { get; private set; }

        public void SetCurrentLobby(Lobby newLobby)
        {
            CurrentLobby = newLobby;
            serializedLobby = newLobby;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        public void SnapshotExpectedFromCurrentLobby()
        {
            ExpectedSteamIds.Clear();

            if (!CurrentLobby.IsValid || CurrentLobby.Members == null) return;

            foreach (var m in CurrentLobby.Members)
                if (ulong.TryParse(m.Id, out var sid) && sid != 0)
                    ExpectedSteamIds.Add(sid);

            Debug.Log("[LobbyDataHolder] ExpectedSteamIds=" + ExpectedSteamIds.Count);
        }
    }
}
