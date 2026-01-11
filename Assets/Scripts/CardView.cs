using UnityEngine;

public class CardView : MonoBehaviour
{
    public int DebugId { get; private set; }

    public void Init(int id, Sprite sprite)
    {
        DebugId = id;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void OnClicked()
    {
        Debug.Log($"Clicked card {DebugId}");
    }
}
