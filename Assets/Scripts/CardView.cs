using UnityEngine;

public class CardView : MonoBehaviour
{
    public CardId Card { get; private set; }

    public void Init(CardId card, Sprite sprite)
    {
        Card = card;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void OnClicked()
    {
        if (GameSession.Instance == null) return;
        GameSession.Instance.Server_RequestPlay(Card);
    }
}
