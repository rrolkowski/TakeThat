using UnityEngine;

public class TopCardView : MonoBehaviour
{
    public static TopCardView Instance { get; private set; }

    [SerializeField] private CardSpriteDB spriteDb;
    [SerializeField] private SpriteRenderer sr;

    private bool locked;

    private void Awake()
    {
        Instance = this;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }
    public void SetCard(CardId card, bool onlyIfUnset)
    {
        if (locked && onlyIfUnset) return;
        if (sr == null || spriteDb == null) return;

        sr.sprite = spriteDb.GetSprite(card);

        if (onlyIfUnset) locked = true;
    }

    public void Clear(bool clearSprite = true)
    {
        locked = false;

        if (clearSprite && sr != null)
            sr.sprite = null;
    }

}
