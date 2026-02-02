using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UiButtonAudioSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public enum ClickSound
    {
        Normal,
        Ready
    }

    [Header("Highlight")]
    [SerializeField] private bool playHighlightOnHover = true;
    [SerializeField] private bool playHighlightOnSelect = true; // pad/keyboard
    [SerializeField] private float highlightCooldown = 0.05f;

    [Header("Click")]
    [SerializeField] private ClickSound clickSound = ClickSound.Normal;

    private Button _button;
    private float _lastHighlightTime = -999f;

    private void Awake()
    {
        _button = GetComponent<Button>();

        // Dopinamy się do onClick jako dodatkowy listener — istniejące eventy zostają.
        _button.onClick.AddListener(PlayClickSfx);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHighlightOnHover) return;
        TryPlayHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!playHighlightOnSelect) return;
        TryPlayHighlight();
    }

    private void TryPlayHighlight()
    {
        if (Time.unscaledTime - _lastHighlightTime < highlightCooldown) return;
        _lastHighlightTime = Time.unscaledTime;

        AudioManager.I?.UiHighlight();
    }

    private void PlayClickSfx()
    {
        if (clickSound == ClickSound.Ready)
            AudioManager.I?.UiReadyClick();
        else
            AudioManager.I?.UiClick();
    }
}
