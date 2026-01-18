using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSession : NetworkBehaviour
{
    public static GameSession Instance { get; private set; }

    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("Rules")]
    [SerializeField] private int initialHandSize = 7;
    [SerializeField] private int maxHandSize = 25;

    [Header("Deck - Numbers")]
    [SerializeField] private int copiesPerNumberPerColor = 4;

    [Header("Deck - Specials")]
    [SerializeField] private int copiesSkip = 16;
    [SerializeField] private int copiesReverse = 16;
    [SerializeField] private int copiesDraw2 = 16;
    [SerializeField] private int copiesDraw3 = 16;

    [Header("Draw3 Reaction Window")]
    [SerializeField] private float draw3ReactionSeconds = 7f;

    [Header("Turn Timer")]
    [SerializeField] private float turnSeconds = 12f;

    [Header("Turn Transition Delay")]
    [SerializeField] private float turnAdvanceDelay = 0.75f;

    [Header("Game Over")]

    [SerializeField] private bool autoReturnToLobbyOnGameOver = false;
    
    private readonly HashSet<PlayerID> resetVotes = new();

    private readonly Dictionary<PlayerID, List<CardId>> hands = new();
    private readonly Stack<CardId> drawPile = new();
    private readonly Stack<CardId> discardPile = new();

    private readonly List<PlayerID> turnOrder = new();
    private readonly Dictionary<PlayerID, int> handCounts = new();

    private bool gameOver;
    private PlayerID winnerPid;

    private bool advanceScheduled;
    private float advanceAt;
    private int advanceSteps;

    private bool turnTimerActive;
    private float turnEndsAt;

    private CardId topCard;
    public CardId TopCard => topCard;

    private PlayerID currentTurn;
    private int direction = 1;

    private bool draw3ReactionActive;
    private float draw3ReactionEndsAt;

    private bool started;
    private int turnIndex;

    private PlayerID localPid;
    private bool hasLocalPid;

    private int pendingDraw = 0;
    private CardType pendingType = CardType.Number;

    private bool clientTurnTransition;
    public int Direction => direction;
    public event Action<int> OnDirectionChanged;

    private int lastEffectId;
    private PlayerID lastEffectTarget;
    private CardType lastEffectType;
    private int lastEffectValue;

    private int clientLastEffectId;
    private int clientPendingDraw;
    private bool clientReactionActive;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[GameSession] Awake. isServer={(NetworkManager.main != null && NetworkManager.main.isServer)}");
    }

    protected override void OnSpawned(bool asServer)
    {
        Debug.Log($"[GameSession] OnSpawned asServer={asServer}");
    }

    public bool IsMyTurn()
    {
        return hasLocalPid && localPid == currentTurn && !clientTurnTransition;
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
        turnIndex = Random.Range(0, turnOrder.Count);

        pendingDraw = 0;
        pendingType = CardType.Number;

        Server_BuildDeck();
        Server_CreateHands(turnOrder);
        Server_Deal(turnOrder, initialHandSize);
        Server_FlipTop_NoSpecialsOnStart();

        currentTurn = GetCurrentTurn();
        Server_StartTurnTimer();


        foreach (var pid in turnOrder)
            Target_SetHand(pid, hands[pid].ToArray());

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_RequestPlay(CardId card, RPCInfo info = default)
    {
        if (!started) return;

        if (gameOver) return;

        if (advanceScheduled) return;

        var pid = info.sender;
        if (pid != currentTurn) return;

        if (!hands.TryGetValue(pid, out var hand)) return;

        int idx = hand.FindIndex(c => c.type == card.type && c.suit == card.suit && c.value == card.value);
        if (idx < 0) return;

        if (!IsPlayable(card)) return;

        hand.RemoveAt(idx);

        Server_CheckWin(pid);
        if (gameOver)
        {
            Target_SetHand(pid, hand.ToArray());
            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
            return;
        }

        topCard = card;
        discardPile.Push(card);

        int seatIndex = -1;
        if (PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar))
            seatIndex = avatar.SeatIndex;

        int pileIndex = discardPile.Count - 1;
        int seed = Random.Range(int.MinValue, int.MaxValue);

        Observers_CardPlayed(seatIndex, card, 1, pileIndex, seed);

        int steps = 1;

        switch (card.type)
        {
            case CardType.Skip:
                var target = PeekNextPlayer(1);
                lastEffectId++;
                lastEffectTarget = target;
                lastEffectType = CardType.Skip;
                lastEffectValue = 0;

                steps = 2;
                break;

            case CardType.Reverse:
                direction *= -1;
                steps = 1;
                break;

            case CardType.Draw2:
                {
                    var next = PeekNextPlayer(1);
                    Server_GiveCards(next, 2);

                    lastEffectId++;
                    lastEffectTarget = next;
                    lastEffectType = CardType.Draw2;
                    lastEffectValue = 2;

                    steps = 2;
                    break;
                }

            case CardType.Draw3:
                {
                    pendingType = CardType.Draw3;
                    pendingDraw += 3;
                    steps = 1;
                    break;
                }
        }

        Server_ScheduleAdvanceTurn(steps);

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();

        Target_SetHand(pid, hand.ToArray());
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_RequestPlayMany(CardId prototype, int count, RPCInfo info = default)
    {
        if (!started) return;

        if (gameOver) return;

        if (advanceScheduled) return;

        var pid = info.sender;
        if (pid != currentTurn) return;

        if (count <= 0) return;

        if (draw3ReactionActive && pendingDraw > 0 && pendingType == CardType.Draw3)
            return;

        if (prototype.type != CardType.Number) return;

        if (!IsPlayable(prototype)) return;

        if (!hands.TryGetValue(pid, out var hand)) return;

        int have = 0;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i].type == prototype.type && hand[i].suit == prototype.suit && hand[i].value == prototype.value)
                have++;

        if (have < count) return;

        int removed = 0;
        for (int i = hand.Count - 1; i >= 0 && removed < count; i--)
        {
            if (hand[i].type == prototype.type && hand[i].suit == prototype.suit && hand[i].value == prototype.value)
            {
                hand.RemoveAt(i);
                removed++;
            }
        }

        Server_CheckWin(pid);
        if (gameOver)
        {
            Target_SetHand(pid, hand.ToArray());
            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
            return;
        }

        for (int i = 0; i < count; i++)
            discardPile.Push(prototype);

        int seatIndex = -1;
        if (PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar))
            seatIndex = avatar.SeatIndex;

        int pileStartIndex = discardPile.Count - count;
        int seed = Random.Range(int.MinValue, int.MaxValue);

        Observers_CardPlayed(seatIndex, prototype, count, pileStartIndex, seed);


        topCard = prototype;

        Server_ScheduleAdvanceTurn(steps: 1);

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();

        Target_SetHand(pid, hand.ToArray());
    }


    [ServerRpc(requireOwnership: false)]
    public void Server_RequestDraw(RPCInfo info = default)
    {
        if (!started) return;

        if (gameOver) return;

        if (advanceScheduled) return;

        var pid = info.sender;
        if (pid != currentTurn) return;

        if (!hands.TryGetValue(pid, out var hand)) return;

        if (draw3ReactionActive && pendingDraw > 0 && pendingType == CardType.Draw3)
        {
            Server_ResolvePendingDraw3_KeepTurn();

            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
            Target_SetHand(pid, hand.ToArray());
            return;
        }

        if (hand.Count >= maxHandSize)
        {
            Server_ScheduleAdvanceTurn(steps: 1);

            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
            Target_SetHand(pid, hand.ToArray());
            return;
        }

        hand.Add(Server_DrawCard());

        Server_ScheduleAdvanceTurn(steps: 1);

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();
        Target_SetHand(pid, hand.ToArray());
    }

    [TargetRpc]
    private void Target_SetHand(PlayerID target, CardId[] hand)
    {
        localPid = target;
        hasLocalPid = true;

        LocalHandView.Instance?.SetHand(hand);
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_ResetMatch(RPCInfo info = default)
    {
        if (!NetworkManager.main.isServer) return;

        resetVotes.Clear();
        gameOver = false;
        winnerPid = default;

        started = false;
        advanceScheduled = false;
        Server_StopTurnTimer();
        draw3ReactionActive = false;
        pendingDraw = 0;
        pendingType = CardType.Number;

        Observers_MatchReset();

        Server_StartGame();
        Server_BroadcastPublicState();
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_ReturnToLobby(RPCInfo info = default)
    {
        var nm = NetworkManager.main;
        if (nm == null || !nm.isServer) return;

        // tylko host (seat 0) mo¿e to wywo³aæ
        if (!PlayerAvatar.allPlayers.TryGetValue(info.sender, out var av) || av.SeatIndex != 0)
            return;

        gameOver = false;
        started = false;
        advanceScheduled = false;
        Server_StopTurnTimer();
        draw3ReactionActive = false;
        pendingDraw = 0;

        Observers_ReturnToLobby();

        nm.sceneModule.LoadSceneAsync(lobbySceneName);
    }

    [ServerRpc(requireOwnership: false)]
    public void Server_VoteReset(bool vote, RPCInfo info = default)
    {
        if (!NetworkManager.main.isServer) return;
        if (!gameOver) return;

        var pid = info.sender;

        if (vote) resetVotes.Add(pid);
        else resetVotes.Remove(pid);

        Server_BroadcastResetVotes();

        int total = PlayerAvatar.allPlayers.Count;
        if (total > 0 && resetVotes.Count >= total)
        {
            resetVotes.Clear();
            Server_ResetMatch();
        }
    }


    private void Update()
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        if (!started) return;

        if (draw3ReactionActive && Time.time >= draw3ReactionEndsAt)
        {
            Server_ResolvePendingDraw3_KeepTurn();

            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
        }

        if (turnTimerActive && Time.time >= turnEndsAt)
        {
            Server_HandleTurnTimeout();
        }

        if (advanceScheduled && Time.time >= advanceAt)
        {
            advanceScheduled = false;

            Server_AdvanceTurn(advanceSteps);

            Server_StartTurnTimer();
            Server_StartDraw3ReactionIfPossible();

            Server_RecalcHandCounts();
            Server_BroadcastPublicState();
        }
    }

    [ObserversRpc]
    private void Observers_MatchReset()
    {
        GameOverPopup.Instance?.Hide();
        PileView.Instance.Clear();
        TopCardView.Instance?.Clear(clearSprite: false);
    }

    [ObserversRpc]
    private void Observers_GameOver(PlayerID winner, string winnerName, ulong winnerSteamId)
    {
        GameOverPopup.Instance?.Show(winner, winnerName, winnerSteamId);
    }

    [ObserversRpc]
    private void Observers_ReturnToLobby()
    {
        //LobbyFlow.Instance?.ReturnToLobby();
    }

    [ObserversRpc]
    private void Observers_CardPlayed(int seatIndex, CardId card, int count, int pileStartIndex, int seed)
    {
        PileThrowController.Instance?.PlayThrow(seatIndex, card, count, pileStartIndex, seed);
    }

    [ObserversRpc]
    private void Observers_PublicStateChanged(
        CardId newTopCard,
        PlayerID newCurrentTurn,
        int newDirection,
        PlayerID[] playerIds,
        int[] counts,
        int pending,
        bool reactionActive,
        float reactionTimeLeft,
        float reactionTotalSeconds,
        int effectId,
        PlayerID effectTarget,
        CardType effectType,
        int effectValue,
        float turnTimeLeft,
        float turnSeconds,
        bool turnTransition

    )
    {
        topCard = newTopCard;
        TopCardView.Instance?.SetCard(topCard, onlyIfUnset: true);

        currentTurn = newCurrentTurn;
        direction = newDirection;
        OnDirectionChanged?.Invoke(direction);

        clientPendingDraw = pending;
        clientReactionActive = reactionActive;
        clientTurnTransition = turnTransition;

        //Debug.Log($"[Public] Top Card: {topCard} | Turn: {currentTurn} | Dir: {direction} | pendingDraw={pendingDraw}");

        if (PlayerAvatar.allPlayers.TryGetValue(currentTurn, out var avatar))
            TurnIndicator.Instance?.SetTarget(avatar.transform);

        OpponentHandsView.Instance?.SetCounts(playerIds, counts);
        OpponentBadgesView.Instance?.SetPlayers(playerIds, counts);
        PlayerEffectView.Instance?.SetPlayers(playerIds);

        if (effectId != 0 && effectId != clientLastEffectId)
        {
            clientLastEffectId = effectId;
            PlayerEffectView.Instance?.ShowEffect(effectTarget, effectType, effectValue);
        }

        if (reactionActive && hasLocalPid && localPid == newCurrentTurn)
        {
            Draw3TimerUI.Instance?.Show(pending, reactionTimeLeft, reactionTotalSeconds);
        }
        else
        {
            Draw3TimerUI.Instance?.Hide();
        }

        if (turnTransition)
        {
            DrawPileIndicator.Instance?.SetVisible(false);
        }
        else
        {
            LocalHandView.Instance?.RefreshDrawIndicator();
        }
    }
    private void Server_BroadcastPublicState()
    {
        var pids = turnOrder.ToArray();
        var counts = new int[pids.Length];

        for (int i = 0; i < pids.Length; i++)
            counts[i] = handCounts.TryGetValue(pids[i], out var c) ? c : 0;

        float draw3TimeLeft = draw3ReactionActive ? Mathf.Max(0f, draw3ReactionEndsAt - Time.time) : 0f;
        float turnTimeLeft = turnTimerActive ? Mathf.Max(0f, turnEndsAt - Time.time) : 0f;

        Observers_PublicStateChanged(topCard, currentTurn, direction, pids, counts, pendingDraw, draw3ReactionActive, draw3TimeLeft, draw3ReactionSeconds,
            lastEffectId, lastEffectTarget, lastEffectType, lastEffectValue, turnTimeLeft, turnSeconds, advanceScheduled);
    }

    private bool Server_PlayerHasDraw3(PlayerID pid)
    {
        if (!hands.TryGetValue(pid, out var hand)) return false;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].type == CardType.Draw3)
                return true;
        }
        return false;
    }

    private void Server_StartDraw3ReactionIfPossible()
    {
        if (pendingDraw <= 0 || pendingType != CardType.Draw3)
        {
            draw3ReactionActive = false;
            return;
        }

        if (!Server_PlayerHasDraw3(currentTurn))
        {
            Server_ResolvePendingDraw3_KeepTurn();
            return;
        }

        draw3ReactionActive = true;
        draw3ReactionEndsAt = Time.time + draw3ReactionSeconds;
    }

    private void Server_ResolvePendingDraw3_KeepTurn()
    {
        int actuallyDrew = 0;

        if (hands.TryGetValue(currentTurn, out var hand))
        {
            int canTake = Mathf.Max(0, maxHandSize - hand.Count);
            int toAdd = Mathf.Min(pendingDraw, canTake);
            actuallyDrew = toAdd;

            for (int i = 0; i < toAdd; i++)
                hand.Add(Server_DrawCard());

            Target_SetHand(currentTurn, hand.ToArray());
        }

        if (actuallyDrew > 0)
        {
            lastEffectId++;
            lastEffectTarget = currentTurn;
            lastEffectType = CardType.Draw3;
            lastEffectValue = actuallyDrew;
        }

        pendingDraw = 0;
        pendingType = CardType.Number;
        draw3ReactionActive = false;
    }

    private void Server_RecalcHandCounts()
    {
        handCounts.Clear();
        foreach (var kv in hands)
            handCounts[kv.Key] = kv.Value.Count;
    }

    private void Server_StartTurnTimer()
    {
        turnTimerActive = true;
        turnEndsAt = Time.time + turnSeconds;
    }

    private void Server_StopTurnTimer()
    {
        turnTimerActive = false;
    }

    private void Server_HandleTurnTimeout()
    {
        if (!started) return;

        var pid = currentTurn;
        if (draw3ReactionActive && pendingDraw > 0 && pendingType == CardType.Draw3)
        {
            Server_ResolvePendingDraw3_KeepTurn();
            Server_ScheduleAdvanceTurn(steps: 1);
        }
        else
        {
            if (hands.TryGetValue(pid, out var hand))
            {
                if (hand.Count < maxHandSize)
                {
                    hand.Add(Server_DrawCard());
                    Target_SetHand(pid, hand.ToArray());
                }
            }

            Server_ScheduleAdvanceTurn(steps: 1);
        }

        Server_RecalcHandCounts();
        Server_BroadcastPublicState();
    }

    private void Server_ScheduleAdvanceTurn(int steps)
    {
        if (!NetworkManager.main.isServer) return;

        advanceScheduled = true;
        advanceSteps = steps;
        advanceAt = Time.time + turnAdvanceDelay;

        Server_StopTurnTimer();
    }

    private void Server_CheckWin(PlayerID pid)
    {
        if (gameOver) return;
        if (!hands.TryGetValue(pid, out var hand)) return;

        if (hand.Count == 0)
        {
            resetVotes.Clear();
            Server_BroadcastResetVotes();
            gameOver = true;
            winnerPid = pid;

            advanceScheduled = false;
            Server_StopTurnTimer();
            draw3ReactionActive = false;
            pendingDraw = 0;

            string name = "Winner";
            ulong steamId = 0;

            if (PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar) && avatar != null)
            {
                name = avatar.DisplayName;
                steamId = avatar.SteamId;
            }

            Observers_GameOver(pid, name, steamId);

            if (autoReturnToLobbyOnGameOver)
                Server_ReturnToLobby();
        }
    }

    private void Server_BroadcastResetVotes()
    {
        resetVotes.RemoveWhere(pid => !PlayerAvatar.allPlayers.ContainsKey(pid));

        int total = PlayerAvatar.allPlayers.Count;
        var voters = resetVotes.ToArray();

        Observers_ResetVotesChanged(resetVotes.Count, total, voters);
    }

    [ObserversRpc]
    private void Observers_ResetVotesChanged(int votes, int total, PlayerID[] voters)
    {
        GameOverPopup.Instance?.SetResetVotes(votes, total, voters);
    }


    private CardId Server_DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count <= 1)
            {
                return new CardId { type = CardType.Number, suit = Suit.Green, value = 2 };
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

    private void Server_GiveCards(PlayerID pid, int count)
    {
        if (!hands.TryGetValue(pid, out var hand)) return;

        int canTake = Mathf.Max(0, maxHandSize - hand.Count);
        int toAdd = Mathf.Min(count, canTake);

        for (int i = 0; i < toAdd; i++)
            hand.Add(Server_DrawCard());

        Target_SetHand(pid, hand.ToArray());
    }

    private void Server_BuildDeck()
    {
        drawPile.Clear();
        discardPile.Clear();
        hands.Clear();

        var suits = new[] { Suit.Green, Suit.Purple, Suit.Blue, Suit.Red };
        var deck = new List<CardId>(suits.Length * 9 * copiesPerNumberPerColor + copiesSkip + copiesReverse + copiesDraw2 + copiesDraw3);

        for (int c = 0; c < copiesPerNumberPerColor; c++)
        {
            for (int v = 2; v <= 10; v++)
            {
                for (int s = 0; s < suits.Length; s++)
                {
                    deck.Add(new CardId
                    {
                        type = CardType.Number,
                        suit = suits[s],
                        value = v
                    });
                }
            }
        }

        for (int i = 0; i < copiesSkip; i++)
            deck.Add(new CardId { type = CardType.Skip, suit = Suit.None, value = 0 });

        for (int i = 0; i < copiesReverse; i++)
            deck.Add(new CardId { type = CardType.Reverse, suit = Suit.None, value = 0 });

        for (int i = 0; i < copiesDraw2; i++)
            deck.Add(new CardId { type = CardType.Draw2, suit = Suit.None, value = 0 });

        for (int i = 0; i < copiesDraw3; i++)
            deck.Add(new CardId { type = CardType.Draw3, suit = Suit.None, value = 0 });

        Shuffle(deck);
        for (int i = 0; i < deck.Count; i++)
            drawPile.Push(deck[i]);
    }

    private void Server_CreateHands(List<PlayerID> playerList)
    {
        hands.Clear();
        foreach (var pid in playerList)
            hands[pid] = new List<CardId>(initialHandSize + 16);
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
    private void Server_FlipTop_NoSpecialsOnStart()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("[GameSession] Cannot flip top card: draw pile empty.");
            topCard = new CardId { type = CardType.Number, suit = Suit.Green, value = 2 };
            discardPile.Push(topCard);
            return;
        }

        var specials = new List<CardId>(8);
        CardId chosen = default;
        bool found = false;

        while (drawPile.Count > 0)
        {
            var c = drawPile.Pop();
            if (c.type == CardType.Number)
            {
                chosen = c;
                found = true;
                break;
            }
            specials.Add(c);
        }

        if (!found)
        {
            chosen = new CardId { type = CardType.Number, suit = Suit.Green, value = 2 };
        }

        for (int i = 0; i < specials.Count; i++)
            discardPile.Push(specials[i]);

        topCard = chosen;
        discardPile.Push(topCard);
    }
    private PlayerID GetCurrentTurn()
    {
        if (turnOrder.Count == 0) return default;

        if (turnIndex < 0) turnIndex = 0;
        if (turnIndex >= turnOrder.Count) turnIndex %= turnOrder.Count;

        return turnOrder[turnIndex];
    }

    private PlayerID PeekNextPlayer(int stepsFromCurrent)
    {
        if (turnOrder.Count == 0) return default;

        int idx = turnIndex;
        int s = Mathf.Abs(stepsFromCurrent);
        for (int i = 0; i < s; i++)
        {
            idx += direction;
            if (idx < 0) idx = turnOrder.Count - 1;
            else if (idx >= turnOrder.Count) idx = 0;
        }

        return turnOrder[idx];
    }

    private bool IsPlayable(CardId card)
    {
        if (draw3ReactionActive && pendingDraw > 0 && pendingType == CardType.Draw3)
            return card.type == CardType.Draw3;

        if (card.type != CardType.Number)
            return true;

        if (topCard.type != CardType.Number)
            return true;

        return card.suit == topCard.suit || card.value == topCard.value;
    }

    public bool IsTurnTransition
    {
        get
        {
            if (NetworkManager.main != null && NetworkManager.main.isServer)
                return clientTurnTransition || advanceScheduled;

            return clientTurnTransition;
        }
    }

    private static void Shuffle(List<CardId> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
