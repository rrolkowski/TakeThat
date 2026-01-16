using System.Collections.Generic;
using UnityEngine;

public class HandFan : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform root;

    [Header("Settings")]
    [SerializeField] private float radius = 3.0f;
    [SerializeField] private float angleRange = 35f;
    [SerializeField] private float arcFlatten = 1f;
    [SerializeField] private float rotationFlatten = 0.35f;

    [Header("Dynamic Angle Range")]
    [SerializeField] private bool useDynamic = true;
    [SerializeField] private int minCards = 1;
    [SerializeField] private int maxCards = 25;
    [SerializeField] private float minAngleRange = -10f;
    [SerializeField] private float maxAngleRange = -60f;
    [SerializeField] private float minArcFlatten = 1f;
    [SerializeField] private float maxArcFlatten = 0.5f;
    [SerializeField] private float minRotationFlatten = 1f;
    [SerializeField] private float maxRotationFlatten = 0.5f;

    private readonly List<CardView> spawned = new();

    public void SetHand(CardId[] hand, System.Func<CardId, Sprite> spriteProvider)
    {
        FindFirstObjectByType<HoverRaycaster>()?.ForceClearHover();

        int count = hand.Length;

        float effectiveAngleRange = angleRange;
        float effectiveArcFlatten = arcFlatten;
        float effectiveRotationFlatten = rotationFlatten;

        if (useDynamic && maxCards > minCards)
        {
            int clamped = Mathf.Clamp(count, minCards, maxCards);
            float u = (clamped - minCards) / (float)(maxCards - minCards);

            effectiveAngleRange = Mathf.Lerp(minAngleRange, maxAngleRange, u);
            effectiveArcFlatten = Mathf.Lerp(minArcFlatten, maxArcFlatten, u);
            effectiveRotationFlatten = Mathf.Lerp(minRotationFlatten, maxRotationFlatten, u);
        }

        while (spawned.Count < count)
        {
            var c = Instantiate(cardPrefab, root);
            spawned.Add(c);
        }

        for (int i = 0; i < spawned.Count; i++)
            spawned[i].ForceUnhover();

        for (int i = count; i < spawned.Count; i++)
            spawned[i].gameObject.SetActive(false);

        if (count == 0) return;

        var positions = new Vector3[count];
        var rotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float deg = Mathf.Lerp(-effectiveAngleRange * 0.5f, effectiveAngleRange * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;

            var pos = new Vector3(
                Mathf.Sin(rad) * radius,
                Mathf.Cos(rad) * radius * effectiveArcFlatten,
                0f
            );
            pos.z = -0.001f * i;

            positions[i] = pos;
            rotations[i] = Quaternion.Euler(0f, 0f, -deg * effectiveRotationFlatten);
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < count; i++)
            center += positions[i];
        center /= count;

        for (int i = 0; i < count; i++)
            positions[i] -= new Vector3(center.x, center.y, 0f);

        for (int i = 0; i < count; i++)
        {
            var c = spawned[i];
            c.gameObject.SetActive(true);

            c.transform.localPosition = positions[i];
            c.transform.localRotation = rotations[i];

            c.Init(hand[i], spriteProvider(hand[i]));

            var sr = c.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = i;
        }
    }

}

