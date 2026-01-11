using PurrNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TableUI : MonoBehaviour
{
    public static TableUI Instance { get; private set; }

    [SerializeField] private TMP_Text topCardText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button drawButton;

    private void Awake()
    {
        Instance = this;
        winnerText.text = "";
    }

    public void BindDraw(GameSession session)
    {
        drawButton.onClick.RemoveAllListeners();
        drawButton.onClick.AddListener(() => session.Server_RequestDraw());
    }

    public void SetTopCard(CardId card)
    {
        topCardText.text = $"Top: {card.suit} {card.value}";
    }

    public void SetTurn(PlayerID pid)
    {
        turnText.text = $"Turn: {pid}";
    }

    public void ShowWinner(PlayerID pid)
    {
        turnText.text = $"Turn: {pid}";
    }
}
