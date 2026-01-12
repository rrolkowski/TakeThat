using UnityEngine;

public class DrawPileView : MonoBehaviour
{
    public void OnClicked()
    {
        if (GameSession.Instance == null) return;
        GameSession.Instance.Server_RequestDraw();
    }
}
