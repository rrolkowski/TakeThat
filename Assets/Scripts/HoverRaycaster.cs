using UnityEngine;
using UnityEngine.InputSystem;

public class HoverRaycaster : MonoBehaviour
{


    [SerializeField] private Camera cam;

    [Header("Layers")]
    [SerializeField] private LayerMask lockLayers;
    [SerializeField] private LayerMask hoverLayers;

    [SerializeField] private float maxDistance = 1000f;

    private CardView hovered;

    void Update()
    {
        if (Mouse.current == null) return;

        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());


        var best = PickTopmost(ray, lockLayers);
        if (best == null)
            best = PickTopmost(ray, hoverLayers);

        if (hovered == best) return;

        if (hovered != null) hovered.OnHoverExit();
        hovered = best;
        if (hovered != null) hovered.OnHoverEnter();
    }

    CardView PickTopmost(Ray ray, LayerMask mask)
    {
        var hits = Physics.RaycastAll(ray, maxDistance, mask);

        CardView best = null;
        int bestOrder = int.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            var cv = hits[i].collider.GetComponentInParent<CardView>();
            if (cv == null) continue;

            var sr = cv.GetComponent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;

            if (order > bestOrder)
            {
                bestOrder = order;
                best = cv;
            }
        }

        return best;
    }
}