using UnityEngine;

public class CardSpriteDB : MonoBehaviour
{
    [Header("Index 0 = value 2, ..., index 8 = value 10")]
    [Header("Cards sprite")]
    [SerializeField] private Sprite[] greenSprites = new Sprite[9];
    [SerializeField] private Sprite[] purpleSprites = new Sprite[9];

    public Sprite GetSprite(CardId card)
    {
        int idx = card.value - 2;
        if (idx < 0 || idx >= 9) return null;

        return card.suit switch
        {
            Suit.Green => greenSprites[idx],
            Suit.Purple => purpleSprites[idx],
            _ => null
        };
    }
}
