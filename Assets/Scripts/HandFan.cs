using System.Collections.Generic;
using UnityEngine;

public class HandFan : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform root;

    [SerializeField] private float radius = 3.0f;
    [SerializeField] private float angleRange = 35f;

    private readonly List<CardView> spawned = new();

    public void SetHand(CardId[] hand, System.Func<CardId, Sprite> spriteProvider)
    {
        Clear();

        int count = hand.Length;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float deg = Mathf.Lerp(-angleRange * 0.5f, angleRange * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;

            var pos = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius, 0f);
            pos.z = -0.001f * i;
            var rot = Quaternion.Euler(0f, 0f, -deg);

            var c = Instantiate(cardPrefab, root);
            c.transform.localPosition = pos;
            c.transform.localRotation = rot;

            c.Init(hand[i], spriteProvider(hand[i]));
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = i;
            spawned.Add(c);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
            Destroy(spawned[i].gameObject);

        spawned.Clear();
    }
}

