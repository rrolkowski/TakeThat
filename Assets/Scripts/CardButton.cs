using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButton : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Button button;

    private CardId card;

    public void Set(CardId c)
    {
        card = c;
        text.text = $"{c.suit} {c.value}";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        var session = FindFirstObjectByType<GameSession>();
        if (session == null) return;

        session.Server_RequestPlay(card);
    }
}
