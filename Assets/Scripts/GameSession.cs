using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class GameSession : NetworkBehaviour
{
    [Header("Rules")]
    [SerializeField] private int initialHandSize = 7;

    private readonly Dictionary<PlayerID, List<CardId>> hands = new();
    private readonly List<PlayerID> turnOrder = new();
    private readonly Stack<CardId> drawPile = new();
    private readonly Stack<CardId> discardPile = new();

    private int turnIndex;

    protected override void OnSpawned(bool asServer)
    {
        if (!asServer) return;
        StartCoroutine(Server_StartWhenReady());
    }
    private System.Collections.IEnumerator Server_StartWhenReady()
    {
        while (PlayerAvatar.allPlayers.Count == 0)
            yield return null;

        Server_BuildDeck();
        Server_BuildTurnOrderFromConnectedPlayers();
        Server_DealHands();
        Server_FlipFirstCard();

        turnIndex = 0;

        Observers_PublicStateChanged(discardPile.Peek(), CurrentTurn());
        Server_SendHandsToOwners();
    }

    private PlayerID CurrentTurn() => turnOrder[turnIndex];

    [ServerRpc]
    public void Server_RequestPlay(CardId card, RPCInfo info = default)
    {
        var playerId = info.sender;

        if (turnOrder.Count == 0) return;
        if (playerId != CurrentTurn()) return;

        if (!hands.TryGetValue(playerId, out var hand)) return;

        int idx = hand.FindIndex(c => c.suit == card.suit && c.value == card.value);
        if (idx < 0) return;

        var top = discardPile.Peek();
        if (!IsPlayable(card, top)) return;

        hand.RemoveAt(idx);
        discardPile.Push(card);

        Target_HandChanged(playerId, hand.ToArray());

        if (hand.Count == 0)
        {
            Observers_GameEnded(playerId);
            return;
        }

        AdvanceTurn();
        Observers_PublicStateChanged(discardPile.Peek(), CurrentTurn());
    }

    [ServerRpc]
    public void Server_RequestDraw(RPCInfo info = default)
    {
        var playerId = info.sender;

        if (turnOrder.Count == 0) return;
        if (playerId != CurrentTurn()) return;

        if (!hands.TryGetValue(playerId, out var hand)) return;

        hand.Add(DrawOne());
        Target_HandChanged(playerId, hand.ToArray());

        AdvanceTurn();
        Observers_PublicStateChanged(discardPile.Peek(), CurrentTurn());
    }

    [ObserversRpc]
    private void Observers_PublicStateChanged(CardId topCard, PlayerID currentTurn)
    {
        TableUI.Instance?.SetTopCard(topCard);
        TableUI.Instance?.SetTurn(currentTurn);
    }

    [TargetRpc]
    private void Target_HandChanged(PlayerID target, CardId[] newHand)
    {
        HandUI.Instance?.SetHand(newHand);
    }

    [ObserversRpc]
    private void Observers_GameEnded(PlayerID winner)
    {
        TableUI.Instance?.ShowWinner(winner);
    }

    private void Server_BuildTurnOrderFromConnectedPlayers()
    {
        turnOrder.Clear();
        foreach (var kv in PlayerAvatar.allPlayers)
            turnOrder.Add(kv.Key);

        //turnOrder.Sort((a, b) => a.Value.CompareTo(b.Value));


        hands.Clear();
        foreach (var pid in turnOrder)
            hands[pid] = new List<CardId>(16);
    }

    private void Server_BuildDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        var deck = new List<CardId>(18);

        for (int v = 2; v <= 10; v++)
        {
            deck.Add(new CardId { suit = Suit.Red, value = v });
            deck.Add(new CardId { suit = Suit.Black, value = v });
        }

        Shuffle(deck);

        for (int i = 0; i < deck.Count; i++)
            drawPile.Push(deck[i]);
    }

    private void Server_DealHands()
    {
        for (int i = 0; i < initialHandSize; i++)
        {
            foreach (var pid in turnOrder)
                hands[pid].Add(DrawOne());
        }
    }

    private void Server_FlipFirstCard()
    {
        discardPile.Push(DrawOne());
    }

    private CardId DrawOne()
    {
        if (drawPile.Count == 0)
            ReshuffleFromDiscard();

        return drawPile.Pop();
    }

    private void ReshuffleFromDiscard()
    {
        if (discardPile.Count <= 1) return;

        var top = discardPile.Pop();
        var tmp = discardPile.ToList();
        discardPile.Clear();
        discardPile.Push(top);

        Shuffle(tmp);

        for (int i = 0; i < tmp.Count; i++)
            drawPile.Push(tmp[i]);
    }

    private void AdvanceTurn()
    {
        turnIndex = (turnIndex + 1) % turnOrder.Count;
    }

    private bool IsPlayable(CardId card, CardId top)
    {
        return card.suit == top.suit || card.value == top.value;
    }

    private void Shuffle(List<CardId> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void Server_SendHandsToOwners()
    {
        foreach (var pid in turnOrder)
            Target_HandChanged(pid, hands[pid].ToArray());
    }
}
