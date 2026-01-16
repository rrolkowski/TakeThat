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

    private CardId topCard;
    private PlayerID currentTurn;
    private int direction = 1;

    private readonly Dictionary<PlayerID, List<CardId>> hands = new();
    private readonly Stack<CardId> drawPile = new();
    private readonly Stack<CardId> discardPile = new();

    private readonly List<PlayerID> turnOrder = new();
    private readonly Dictionary<PlayerID, int> handCounts = new();

    private bool started;
    private int turnIndex;

    private PlayerID localPid;
    private bool hasLocalPid;

    private void Awake()
    {
        Instance = this;
        Debug.Log($"[GameSession] Awake. isServer={(NetworkManager.main != null && NetworkManager.main.isServer)}");

    }

    protected override void OnSpawned(bool asServer)
    {
        Debug.Log($"[GameSession] OnSpawned asServer={asServer}");
    }

    public bool IsMyTurn()
    {
        return hasLocalPid && localPid == currentTurn;
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

    [ServerRpc(requireOwnership: false)]
    public void Server_RequestPlay(CardId card, RPCInfo info = default)
    {
        if (!started) return;

        var pid = info.sender;

        if (pid != currentTurn) return;

        if (!hands.TryGetValue(pid, out var hand)) return;

        int idx = hand.FindIndex(c => c.suit == card.suit && c.value == card.value);
        if (idx < 0) return;

        if (!IsPlayable(card, topCard)) return;

        hand.RemoveAt(idx);

        topCard = card;
        discardPile.Push(card);

        Server_AdvanceTurn(steps: 1);

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();

        Target_SetHand(pid, hand.ToArray());
    }

    private static bool IsPlayable(CardId card, CardId top)
    {
        return card.suit == top.suit || card.value == top.value;
    }

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
        TopCardView.Instance?.SetCard(topCard);

        currentTurn = newCurrentTurn;
        direction = newDirection;

        Debug.Log($"[Public] Top Card: {topCard.suit} {topCard.value} | Turn: {currentTurn} | Dir: {direction}");

        if (PlayerAvatar.allPlayers.TryGetValue(currentTurn, out var avatar))
            TurnIndicator.Instance?.SetTarget(avatar.transform);

        OpponentHandsView.Instance?.SetCounts(playerIds, counts);
        OpponentBadgesView.Instance?.SetPlayers(playerIds, counts);

    }

    [TargetRpc]
    private void Target_SetHand(PlayerID target, CardId[] hand)
    {
        localPid = target;
        hasLocalPid = true;

        LocalHandView.Instance?.SetHand(hand);
    }


    [ServerRpc(requireOwnership: false)]
    public void Server_RequestDraw(RPCInfo info = default)
    {
        if (!started) return;

        var pid = info.sender;

        if (pid != currentTurn) return;

        if (!hands.TryGetValue(pid, out var hand)) return;

        var card = Server_DrawCard();
        hand.Add(card);

        Server_AdvanceTurn(steps: 1);

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();
        Target_SetHand(pid, hand.ToArray());
    }

    private CardId Server_DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count <= 1)
            {
                return new CardId { suit = Suit.Green, value = 2 };
            }

            var top = discardPile.Pop();
            var temp = discardPile.ToList();
            discardPile.Clear();
            discardPile.Push(top);

            Shuffle(temp);
            for (int i = 0; i < temp.Count; i++)
                drawPile.Push(temp[i]);
        }

        return drawPile.Pop();
    }

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
