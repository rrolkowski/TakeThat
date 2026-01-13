using UnityEngine;

public class LocalHandView : MonoBehaviour
{
    public static LocalHandView Instance { get; private set; }

    [SerializeField] private HandFan fan;
    [SerializeField] private CardSpriteDB spriteDb;

    private void Awake()
    {
        Instance = this;
    }

    public void SetHand(CardId[] hand)
    {
        fan.SetHand(hand, spriteDb.GetSprite);
    }
}
