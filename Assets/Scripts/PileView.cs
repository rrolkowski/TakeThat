using System.Collections.Generic;
using UnityEngine;

public class PileView : MonoBehaviour
{
    [SerializeField] private Transform pileRoot;
    [SerializeField] private Transform pileAnchor;
    [SerializeField] private CardSpriteDB spriteDb;
    [SerializeField] private PileCardVisual pileCardPrefab;

    [Header("Stack look")]
    [SerializeField] private float yLiftPerCard = 0.0001f;
    [SerializeField] private float randomOffset = 0.06f;
    [SerializeField] private float randomTiltDegrees = 8f;

    [Header("Stack limit")]
    [SerializeField] private int maxVisibleCards = 30;
    [SerializeField] private bool usePooling = true;
    [SerializeField] private bool renormalizeSortingOrders = true;

    private readonly List<PileCardVisual> active = new List<PileCardVisual>(64);
    private readonly Queue<PileCardVisual> pool = new Queue<PileCardVisual>(64);

    public void AddCard(CardId card, int pileIndex, int seed)
    {
        if (pileRoot == null || pileAnchor == null || spriteDb == null || pileCardPrefab == null)
            return;

        var vis = GetOrCreateVisual();
        var sprite = spriteDb.GetSprite(card);

        vis.SetSprite(sprite, sortingOrder: pileIndex);

        Vector3 basePos = pileAnchor.position;
        Quaternion baseRot = pileAnchor.rotation;

        var rng = new System.Random(Hash(seed, pileIndex));
        float ox = Range(rng, -randomOffset, randomOffset);
        float oz = Range(rng, -randomOffset, randomOffset);
        float tilt = Range(rng, -randomTiltDegrees, randomTiltDegrees);

        Vector3 pos = basePos + new Vector3(ox, pileIndex * yLiftPerCard, oz);

        Quaternion rot = baseRot * Quaternion.Euler(0f, 0f, tilt);

        vis.transform.SetPositionAndRotation(pos, rot);
        vis.transform.localScale = pileAnchor.localScale;

        active.Add(vis);

        EnforceLimit();

        if (renormalizeSortingOrders)
            RenormalizeOrders();
    }

    private PileCardVisual GetOrCreateVisual()
    {
        PileCardVisual vis;

        if (usePooling && pool.Count > 0)
        {
            vis = pool.Dequeue();
            vis.gameObject.SetActive(true);
            vis.transform.SetParent(pileRoot, worldPositionStays: true);
        }
        else
        {
            vis = Instantiate(pileCardPrefab, pileRoot);
        }

        return vis;
    }

    private void EnforceLimit()
    {
        if (maxVisibleCards <= 0) return;

        while (active.Count > maxVisibleCards)
        {
            var oldest = active[0];
            active.RemoveAt(0);

            if (usePooling)
            {
                oldest.gameObject.SetActive(false);
                oldest.transform.SetParent(pileRoot, worldPositionStays: false);
                pool.Enqueue(oldest);
            }
            else
            {
                Destroy(oldest.gameObject);
            }
        }
    }

    private void RenormalizeOrders()
    {
        for (int i = 0; i < active.Count; i++)
        {

            var sr = active[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = i;
        }
    }

    private static int Hash(int a, int b)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + a;
            h = h * 31 + b;
            return h;
        }
    }

    private static float Range(System.Random rng, float min, float max)
    {
        return (float)(min + (rng.NextDouble() * (max - min)));
    }
}
