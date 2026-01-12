using UnityEngine;
using UnityEngine.EventSystems;

public class DisableEventSystemIfNoFocus : MonoBehaviour
{
    void Update()
    {
        var es = EventSystem.current;
        if (!Application.isFocused && es != null && es.gameObject.activeSelf)
            es.gameObject.SetActive(false);

        if (Application.isFocused && es != null && !es.gameObject.activeSelf)
            es.gameObject.SetActive(true);
    }
}

