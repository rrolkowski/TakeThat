using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Draw3TimerUI : MonoBehaviour
{
    public static Draw3TimerUI Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Image fill;

    private float timeLeft;
    private float total;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;

        timeLeft = Mathf.Max(0f, timeLeft - Time.deltaTime);

        if (text != null)
            text.text = $"Time left: {timeLeft:0.0}s";

        if (fill != null && total > 0.01f)
            fill.fillAmount = timeLeft / total;

        if (timeLeft <= 0f)
            Hide();
    }

    public void Show(int pending, float newTimeLeft, float totalSeconds)
    {
        total = Mathf.Max(0.01f, totalSeconds);
        timeLeft = Mathf.Clamp(newTimeLeft, 0f, total);

        if (root != null) root.SetActive(true);
        if (text != null) text.text = $"+{pending} | {timeLeft:0.0}s";
        if (fill != null) fill.fillAmount = timeLeft / total;
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        timeLeft = 0f;
        total = 0f;
    }
}
