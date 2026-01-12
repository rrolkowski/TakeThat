using UnityEngine;

public class TopCardView : MonoBehaviour
{
    public static TopCardView Instance { get; private set; }

    [SerializeField] private CardSpriteDB spriteDb;
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        Instance = this;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    public void SetCard(CardId card)
    {
        if (sr == null || spriteDb == null) return;
        sr.sprite = spriteDb.GetSprite(card);
    }
}
