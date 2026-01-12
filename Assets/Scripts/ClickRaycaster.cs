using UnityEngine;
using UnityEngine.InputSystem;

public class ClickRaycaster : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask clickableLayers = ~0; // wszystko

    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        var hits = Physics.RaycastAll(ray, 1000f, clickableLayers);

        if (hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var card = hits[i].collider.GetComponentInParent<CardView>();
            if (card != null)
            {
                card.OnClicked();
                break;
            }
            var pile = hits[i].collider.GetComponentInParent<DrawPileView>();
            if (pile != null)
            { 
                pile.OnClicked();
                break;
            }
        }
    }
}
