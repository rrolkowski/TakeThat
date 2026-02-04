using UnityEngine;
using TMPro;

public class HoverCardInfoUI : MonoBehaviour
{
    public static HoverCardInfoUI Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Colors")]
    [SerializeField] private string defaultHex = "F3C748";
    [SerializeField] private string numberHex = "C475FF";

    private Color defaultColor;
    private Color numberColor;

    private CardView current; // kto aktualnie "trzyma" UI

    void Awake()
    {
        Instance = this;

        if (root == null) root = gameObject;

        ColorUtility.TryParseHtmlString("#" + defaultHex, out defaultColor);
        ColorUtility.TryParseHtmlString("#" + numberHex, out numberColor);

        Hide();
    }

    public void Show(CardView view)
    {
        if (view == null || label == null || root == null) return;

        current = view;

        string typeStr = view.Card.type.ToString(); // działa nawet jak enum ma inne nazwy
        string text = "SELECTED: " + GetPrettyType(typeStr);

        label.text = text;
        label.color = IsNumber(typeStr) ? numberColor : defaultColor;

        root.SetActive(true);
    }

    public void HideIfOwner(CardView view)
    {
        if (current == view)
            Hide();
    }

    public void Hide()
    {
        current = null;
        if (root != null) root.SetActive(false);
    }

    private static bool IsNumber(string typeStr)
    {
        // super bezpieczne pod różne nazwy enumów
        return typeStr.ToLowerInvariant().Contains("number");
    }

    private static string GetPrettyType(string typeStr)
    {
        // mapowanie na Twoje napisy
        string t = typeStr.ToLowerInvariant();

        if (t.Contains("number")) return "NUMBER CARD";
        if (t.Contains("block") || t.Contains("skip")) return "BLOCK CARD";
        if (t.Contains("reverse")) return "REVERSE CARD";
        if (t.Contains("plus2") || t.Contains("+2") || t.Contains("draw2")) return "+2 CARD";
        if (t.Contains("plus3") || t.Contains("+3") || t.Contains("draw3")) return "+3 CARD";

        // fallback jakby doszły inne typy
        return typeStr.ToUpperInvariant() + " CARD";
    }
}
