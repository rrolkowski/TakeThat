using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class RoundTimerUI : MonoBehaviour
{
    public static RoundTimerUI Instance { get; private set; }

    [SerializeField] private Image radial;
    [SerializeField] private float showLastSeconds = 5f;

    private float _serverTimeLeft;
    private float _turnDuration;
    private float _receivedAt;
    private bool _turnTransition;

    private void Awake()
    {
        Instance = this;
        if (radial == null) radial = GetComponentInChildren<Image>();
        radial.gameObject.SetActive(false);
    }

    public void SetState(float timeLeft, float duration, bool turnTransition)
    {
        _serverTimeLeft = Mathf.Max(0f, timeLeft);
        _turnDuration = Mathf.Max(0.0001f, duration);
        _turnTransition = turnTransition;
        _receivedAt = Time.unscaledTime;

        UpdateVisual();
    }

    private void Update()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (radial == null || GameSession.Instance == null)
        {
            radial.gameObject.SetActive(false);
            return;
        }

        if (GameSession.Instance.IsGameOverClient || _turnTransition || !GameSession.Instance.IsMyTurn())
        {
            radial.gameObject.SetActive(false);
            return;
        }

        float left = Mathf.Max(0f, _serverTimeLeft - (Time.unscaledTime - _receivedAt));

        bool visible = left > 0f && left <= showLastSeconds;
        if (radial.gameObject.activeSelf != visible)
            radial.gameObject.SetActive(visible);

        if (visible)
            //radial.fillAmount = left / _turnDuration;
            radial.fillAmount = Mathf.Clamp01(left / showLastSeconds);

    }
}
