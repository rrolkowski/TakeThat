using System.Collections.Generic;
using System.Linq;
using PurrNet;
using UnityEngine;

public class GameSession : NetworkBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int initialHandSize = 7;
    [SerializeField] private int copiesPerCard = 4;

    // Publiczny stan
    private CardId topCard;
    private PlayerID currentTurn;
    private int direction = 1; // na razie zawsze 1; reverse póŸniej

    // Serwer: stan gry
    private readonly Dictionary<PlayerID, List<CardId>> hands = new();
    private readonly Stack<CardId> drawPile = new();
    private readonly Stack<CardId> discardPile = new();

    private readonly List<PlayerID> turnOrder = new();
    private readonly Dictionary<PlayerID, int> handCounts = new();

    private bool started;
    private int turnIndex;

    private void Awake()
    {
        Instance = this;
    }

    protected override void OnSpawned(bool asServer)
    {
        Debug.Log($"[GameSession] OnSpawned asServer={asServer}");
        // start z lobby: Server_StartGame()
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_StartGame(RPCInfo info = default)
    {
        if (started) return;

        turnOrder.Clear();
        turnOrder.AddRange(PlayerAvatar.allPlayers.Keys);

        if (turnOrder.Count == 0)
        {
            Debug.LogWarning("[GameSession] StartGame: no players connected.");
            return;
        }

        started = true;
        direction = 1;
        turnIndex = 0;

        Server_BuildDeck(copiesPerCard);
        Server_CreateHands(turnOrder);
        Server_Deal(turnOrder, initialHandSize);
        Server_FlipTop();

        currentTurn = GetCurrentTurn();

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();

        foreach (var pid in turnOrder)
            Target_SetHand(pid, hands[pid].ToArray());
    }

    // =========================
    // ETAP 4: GRACIE KARTY
    // =========================

    [ServerRpc(requireOwnership: false)]
    public void Server_RequestPlay(CardId card, RPCInfo info = default)
    {
        if (!started) return;

        var pid = info.sender;

        // 1) czyja tura
        if (pid != currentTurn) return;

        // 2) czy karta jest w rêce
        if (!hands.TryGetValue(pid, out var hand)) return;

        int idx = hand.FindIndex(c => c.suit == card.suit && c.value == card.value);
        if (idx < 0) return;

        // 3) czy pasuje do top
        if (!IsPlayable(card, topCard)) return;

        // OK -> wykonaj ruch
        hand.RemoveAt(idx);

        topCard = card;
        discardPile.Push(card);

        // prze³¹cz turê
        Server_AdvanceTurn(steps: 1);

        // update
        Server_RecalcHandCounts();
        Server_BroadcastPublicState();

        // prywatna rêka tylko dla gracza, który zagra³
        Target_SetHand(pid, hand.ToArray());
    }

    private static bool IsPlayable(CardId card, CardId top)
    {
        return card.suit == top.suit || card.value == top.value;
    }

    // =========================
    // PUBLICZNY UPDATE (Etap 4-6)
    // =========================

    private void Server_RecalcHandCounts()
    {
        handCounts.Clear();
        foreach (var kv in hands)
            handCounts[kv.Key] = kv.Value.Count;
    }

    private void Server_BroadcastPublicState()
    {
        var pids = turnOrder.ToArray();
        var counts = new int[pids.Length];

        for (int i = 0; i < pids.Length; i++)
            counts[i] = handCounts.TryGetValue(pids[i], out var c) ? c : 0;

        Observers_PublicStateChanged(topCard, currentTurn, direction, pids, counts);
    }

    [ObserversRpc]
    private void Observers_PublicStateChanged(
        CardId newTopCard,
        PlayerID newCurrentTurn,
        int newDirection,
        PlayerID[] playerIds,
        int[] counts
    )
    {
        topCard = newTopCard;
        currentTurn = newCurrentTurn;
        direction = newDirection;

        Debug.Log($"[Public] Top Card: {topCard.suit} {topCard.value} | Turn: {currentTurn} | Dir: {direction}");

        // Etap 6: tu podepniesz przeciwników:
        // playerIds[i] ma counts[i] kart
    }

    [TargetRpc]
    private void Target_SetHand(PlayerID target, CardId[] hand)
    {
        LocalHandView.Instance?.SetHand(hand);
    }

    // =========================
    // TURN HELPERS
    // =========================

    private PlayerID GetCurrentTurn()
    {
        if (turnOrder.Count == 0) return default;

        if (turnIndex < 0) turnIndex = 0;
        if (turnIndex >= turnOrder.Count) turnIndex %= turnOrder.Count;

        return turnOrder[turnIndex];
    }

    private void Server_AdvanceTurn(int steps)
    {
        if (turnOrder.Count == 0) return;

        int s = Mathf.Abs(steps);
        for (int i = 0; i < s; i++)
        {
            turnIndex += direction;
            if (turnIndex < 0) turnIndex = turnOrder.Count - 1;
            else if (turnIndex >= turnOrder.Count) turnIndex = 0;
        }

        currentTurn = GetCurrentTurn();
    }

    // =========================
    // DECK / DEAL
    // =========================

    private void Server_BuildDeck(int copiesPerCardLocal)
    {
        drawPile.Clear();
        discardPile.Clear();
        hands.Clear();

        var deck = new List<CardId>(2 * 9 * copiesPerCardLocal);

        for (int c = 0; c < copiesPerCardLocal; c++)
        {
            for (int v = 2; v <= 10; v++)
            {
                deck.Add(new CardId { suit = Suit.Green, value = v });
                deck.Add(new CardId { suit = Suit.Purple, value = v });
            }
        }

        Shuffle(deck);
        for (int i = 0; i < deck.Count; i++)
            drawPile.Push(deck[i]);
    }

    private void Server_CreateHands(List<PlayerID> playerList)
    {
        hands.Clear();
        foreach (var pid in playerList)
            hands[pid] = new List<CardId>(initialHandSize + 8);
    }

    private void Server_Deal(List<PlayerID> playerList, int count)
    {
        for (int i = 0; i < count; i++)
        {
            for (int p = 0; p < playerList.Count; p++)
            {
                if (drawPile.Count == 0)
                {
                    Debug.LogWarning("[GameSession] Draw pile empty during deal.");
                    return;
                }
                hands[playerList[p]].Add(drawPile.Pop());
            }
        }
    }

    private void Server_FlipTop()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[GameSession] Cannot flip top card: draw pile empty.");
            topCard = new CardId { suit = Suit.Green, value = 2 };
            discardPile.Push(topCard);
            return;
        }

        topCard = drawPile.Pop();
        discardPile.Push(topCard);
    }

    private static void Shuffle(List<CardId> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
