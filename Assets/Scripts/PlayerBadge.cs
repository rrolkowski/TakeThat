using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBadge : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image icon;

    public void SetName(string n)
    {
        if (nameText != null) nameText.text = n;
    }

    public void SetCount(int c)
    {
        if (countText != null) countText.text = c.ToString();
    }

    public void SetIcon(Sprite s)
    {
        if (icon == null) return;

        icon.sprite = s;
        icon.enabled = (s != null);
    }
}
