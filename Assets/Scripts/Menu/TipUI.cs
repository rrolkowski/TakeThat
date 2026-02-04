using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Co ma się pokazać po najechaniu")]
    [SerializeField] private GameObject objectToShow;

    [Header("UI Image do podświetlenia (domyślnie: Image na tym obiekcie)")]
    [SerializeField] private Image targetImage;

    [Header("Alpha (0-255)")]
    [Range(0, 255)][SerializeField] private int defaultAlpha = 10;
    [Range(0, 255)][SerializeField] private int hoverAlpha = 200;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Stan początkowy
        if (objectToShow != null)
            objectToShow.SetActive(false);

        if (targetImage != null)
            SetAlpha255(targetImage, defaultAlpha);
        else
            Debug.LogWarning("TipUI: Nie znaleziono Image. Podepnij targetImage albo dodaj Image na obiekt.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (objectToShow != null)
            objectToShow.SetActive(true);

        if (targetImage != null)
            SetAlpha255(targetImage, hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (objectToShow != null)
            objectToShow.SetActive(false);

        if (targetImage != null)
            SetAlpha255(targetImage, defaultAlpha);
    }

    private static void SetAlpha255(Image img, int alpha255)
    {
        var c = img.color;
        c.a = Mathf.Clamp01(alpha255 / 255f);
        img.color = c;
    }
}
