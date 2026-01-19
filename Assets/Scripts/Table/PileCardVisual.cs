using UnityEngine;

public class PileCardVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
    }

    public void SetSprite(Sprite sprite, int sortingOrder)
    {
        if (sr == null) return;
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
    }
}
