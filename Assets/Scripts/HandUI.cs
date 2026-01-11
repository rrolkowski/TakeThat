using System.Collections.Generic;
using UnityEngine;

public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    [SerializeField] private CardButton cardButtonPrefab;
    [SerializeField] private Transform container;

    private readonly List<CardButton> spawned = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetHand(CardId[] cards)
    {
        Clear();

        for (int i = 0; i < cards.Length; i++)
        {
            var btn = Instantiate(cardButtonPrefab, container);
            btn.Set(cards[i]);
            spawned.Add(btn);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
            Destroy(spawned[i].gameObject);

        spawned.Clear();
    }
}