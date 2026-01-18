using UnityEngine;

public class LocalHandView : MonoBehaviour
{
    public static LocalHandView Instance { get; private set; }

    [SerializeField] private HandFan fan;
    [SerializeField] private CardSpriteDB spriteDb;

    private CardId[] currentHand = System.Array.Empty<CardId>();

    private void Awake()
    {

        Instance = this;
    }

    public void SetHand(CardId[] hand)
    {
        currentHand = hand;
        fan.SetHand(hand, spriteDb.GetSprite);

        RefreshDrawIndicator();
    }

    public void RefreshDrawIndicator()
    {
        if (DrawPileIndicator.Instance == null || GameSession.Instance == null)
            return;

        if (!GameSession.Instance.IsMyTurn())
        {
            DrawPileIndicator.Instance.SetVisible(false);
            return;
        }

        bool canPlay = HasAnyPlayableCard(currentHand, GameSession.Instance.TopCard);

        DrawPileIndicator.Instance.SetVisible(!canPlay);
    }

    private static bool HasAnyPlayableCard(CardId[] hand, CardId top)
    {
        for (int i = 0; i < hand.Length; i++)
        {
            if (IsPlayableClient(hand[i], top))
                return true;
        }
        return false;
    }

    private static bool IsPlayableClient(CardId card, CardId top)
    {
        if (card.type != CardType.Number)
            return true;

        if (top.type != CardType.Number)
            return true;

        return card.suit == top.suit || card.value == top.value;
    }

    public int CountCopiesInHand(CardId card)
    {
        int c = 0;
        for (int i = 0; i < currentHand.Length; i++)
            if (currentHand[i].type == card.type && currentHand[i].suit == card.suit && currentHand[i].value == card.value)
                c++;
        return c;
    }

}

