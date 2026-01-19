using UnityEngine;

public class CardSpriteDB : MonoBehaviour
{
    [Header("Index 0 = value 2, ..., index 8 = value 10")]
    [Header("Cards sprite")]
    [SerializeField] private Sprite[] greenSprites = new Sprite[9];
    [SerializeField] private Sprite[] purpleSprites = new Sprite[9];
    [SerializeField] private Sprite[] blueSprites = new Sprite[9];
    [SerializeField] private Sprite[] redSprites = new Sprite[9];

    [Header("Specials")]
    [SerializeField] private Sprite skipSprite;
    [SerializeField] private Sprite reverseSprite;
    [SerializeField] private Sprite draw2Sprite;
    [SerializeField] private Sprite draw3Sprite;


    public Sprite GetSprite(CardId card)
    {
        if (card.type != CardType.Number)
        {
            return card.type switch
            {
                CardType.Skip => skipSprite,
                CardType.Reverse => reverseSprite,
                CardType.Draw2 => draw2Sprite,
                CardType.Draw3 => draw3Sprite,
                _ => null
            };
        }

        int idx = card.value - 2;
        if (idx < 0 || idx >= 9) return null;

        return card.suit switch
        {
            Suit.Green => greenSprites[idx],
            Suit.Purple => purpleSprites[idx],
            Suit.Blue => blueSprites[idx],
            Suit.Red => redSprites[idx],
            _ => null
        };
    }
}

