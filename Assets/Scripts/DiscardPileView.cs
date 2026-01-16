using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardPileView : MonoBehaviour
{
    public static DiscardPileView Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private CardSpriteDB spriteDb;
    [SerializeField] private SpriteRenderer pileCardPrefab;
    [SerializeField] private Transform pileAnchor;

    [Header("Fallback start (for opponents)")]
    [SerializeField] private Transform defaultFrom; // np. gdzieœ nad sto³em / przy œrodku

    [Header("Stack look")]
    [SerializeField] private int maxVisible = 25;
    [SerializeField] private float zStep = -0.0005f;
    [SerializeField] private float randomRotZ = 6f;
    [SerializeField] private Vector2 randomXY = new Vector2(0.03f, 0.03f);

    [Header("Throw anim")]
    [SerializeField] private float throwTime = 0.18f;
    [SerializeField] private float arcHeight = 0.6f;
    [SerializeField] private float startScale = 1.0f;
    [SerializeField] private float endScale = 1.0f;

    [Header("3D rotation")]
    [SerializeField] private float startX = 0f;      // w locie startujemy z 0
    [SerializeField] private float endX = 90f;       // na stole zawsze 90
    [SerializeField] private float startZ = 0f;      // jeœli nie podasz inaczej
    [SerializeField] private bool useCardStartZIfProvided = true;

    private readonly List<SpriteRenderer> stack = new();

    private void Awake() => Instance = this;

    public Vector3 DefaultFromPos =>
        defaultFrom != null ? defaultFrom.position : (pileAnchor != null ? pileAnchor.position + Vector3.up * 2f : Vector3.zero);

    // Najprostsze API: rzucamy kartê ze œwiata (np. z pozycji klikniêtej karty)
    public void ThrowToPile(CardId card, Vector3 fromWorldPos, float? optionalStartZ = null)
    {
        if (spriteDb == null || pileCardPrefab == null || pileAnchor == null) return;
        Debug.Log("hehe");
        var sr = Instantiate(pileCardPrefab, pileAnchor);
        sr.sprite = spriteDb.GetSprite(card);

        sr.transform.position = fromWorldPos;
        sr.transform.localScale = Vector3.one * startScale;

        float z0 = startZ;
        if (useCardStartZIfProvided && optionalStartZ.HasValue)
            z0 = optionalStartZ.Value;

        StartCoroutine(AnimateThrow(sr, z0));
    }

    IEnumerator AnimateThrow(SpriteRenderer sr, float z0)
    {
        Vector3 start = sr.transform.position;

        int idx = stack.Count;

        Vector3 end = pileAnchor.position;
        end += new Vector3(
            Random.Range(-randomXY.x, randomXY.x),
            Random.Range(-randomXY.y, randomXY.y),
            zStep * idx
        );

        float z1 = Random.Range(-randomRotZ, randomRotZ);

        // ³uk
        Vector3 mid = (start + end) * 0.5f + Vector3.up * arcHeight;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, throwTime);
            float u = Mathf.Clamp01(t);

            // quadratic bezier
            Vector3 a = Vector3.Lerp(start, mid, u);
            Vector3 b = Vector3.Lerp(mid, end, u);
            sr.transform.position = Vector3.Lerp(a, b, u);

            // ROT: X p³ynnie 0 -> 90, Z p³ynnie z0 -> z1
            float x = Mathf.Lerp(startX, endX, u);
            float z = Mathf.Lerp(z0, z1, u);
            sr.transform.rotation = Quaternion.Euler(x, 0f, z);

            sr.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, u);

            yield return null;
        }

        // commit
        sr.transform.SetParent(pileAnchor, true);
        sr.transform.rotation = Quaternion.Euler(endX, 0f, z1); // finalnie pewne 90
        stack.Add(sr);

        while (stack.Count > maxVisible)
        {
            Destroy(stack[0].gameObject);
            stack.RemoveAt(0);
        }
    }
}
