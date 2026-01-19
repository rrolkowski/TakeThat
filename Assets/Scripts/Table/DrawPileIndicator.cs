using UnityEngine;

public class DrawPileIndicator : MonoBehaviour
{
    public static DrawPileIndicator Instance { get; private set; }

    [SerializeField] private GameObject root;

    private void Awake()
    {
        Instance = this;
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
