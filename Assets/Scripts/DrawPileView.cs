using UnityEngine;

public class DrawPileView : MonoBehaviour
{
    public void OnClicked()
    {
        if (GameSession.Instance == null) return;
        if (!GameSession.Instance.IsMyTurn()) return;
        GameSession.Instance.Server_RequestDraw();
    }
}
