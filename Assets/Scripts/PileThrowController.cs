using System.Collections;
using UnityEngine;

public class PileThrowController : MonoBehaviour
{
    public static PileThrowController Instance { get; private set; }

    [Header("Scene refs")]
    [SerializeField] private Transform pileAnchor;
    [SerializeField] private Transform[] seatThrowOrigins;
    [SerializeField] private PileView pileView;
    [SerializeField] private CardSpriteDB spriteDb;
    [SerializeField] private PileCardVisual flyingCardPrefab;

    [Header("Throw motion")]
    [SerializeField] private float throwDuration = 0.35f;
    [SerializeField] private float arcHeight = 1.2f;
    [SerializeField] private float extraSpinDegrees = 180f;
    [SerializeField] private float perCardDelayMany = 0.07f;

    private PendingStart pending;

    private void Awake()
    {
        Instance = this;
    }

    public void NotifyLocalClick(CardId card, Transform clickedCardTransform)
    {
        pending.active = true;
        pending.card = card;
        pending.pos = clickedCardTransform.position;
        pending.rot = clickedCardTransform.rotation;
        pending.scale = clickedCardTransform.lossyScale;
        pending.time = Time.time;
    }

    public void PlayThrow(int seatIndex, CardId card, int count, int pileStartIndex, int seed)
    {
        if (pileAnchor == null || pileView == null || spriteDb == null || flyingCardPrefab == null)
            return;

        if (count <= 0) count = 1;

        for (int i = 0; i < count; i++)
        {
            int pileIndex = pileStartIndex + i;
            int cardSeed = seed + (i * 9973);

            Vector3 startPos;
            Quaternion startRot;
            Vector3 startScale;

            if (TryGetLocalStartOverride(seatIndex, card, out var p, out var r, out var s) && i == 0)
            {
                startPos = p;
                startRot = r;
                startScale = s;
            }
            else
            {
                var origin = GetSeatOrigin(seatIndex);
                startPos = origin.position;
                startRot = origin.rotation;
                startScale = origin.lossyScale;
            }

            StartCoroutine(ThrowRoutine(startPos, startRot, startScale, card, pileIndex, cardSeed, i * perCardDelayMany));

        }
    }

    private IEnumerator ThrowRoutine(Vector3 startPos, Quaternion startRot, Vector3 startScale, CardId card, int pileIndex, int seed, float delay)
    { 
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        var fly = Instantiate(flyingCardPrefab, null);

        fly.SetSprite(spriteDb.GetSprite(card), sortingOrder: 5000);
        fly.transform.position = startPos;
        fly.transform.rotation = startRot;
        fly.transform.localScale = startScale;

        Vector3 endPos = pileAnchor.position;
        Quaternion endRot = pileAnchor.rotation;

        Vector3 mid = (startPos + endPos) * 0.5f + Vector3.up * arcHeight;

        float t = 0f;
        float inv = throwDuration > 0.0001f ? (1f / throwDuration) : 1f;

        while (t < 1f)
        {
            t += Time.deltaTime * inv;
            float u = Mathf.Clamp01(t);

            Vector3 p = Bezier(startPos, mid, endPos, u);
            fly.transform.position = p;

            Quaternion baseRot = Quaternion.Slerp(startRot, endRot, u);
            Quaternion spin = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, extraSpinDegrees, u));
            fly.transform.rotation = baseRot * spin;

            yield return null;
        }

        Destroy(fly.gameObject);

        pileView.AddCard(card, pileIndex, seed);


        if (pending.active && SameCard(pending.card, card))
            pending.active = false;
    }

    private Transform GetSeatOrigin(int seatIndex)
    {
        if (seatThrowOrigins == null || seatThrowOrigins.Length == 0)
            return pileAnchor;

        if (seatIndex < 0 || seatIndex >= seatThrowOrigins.Length || seatThrowOrigins[seatIndex] == null)
            return seatThrowOrigins[0] != null ? seatThrowOrigins[0] : pileAnchor;

        return seatThrowOrigins[seatIndex];
    }

    private bool TryGetLocalStartOverride(int seatIndex, CardId card, out Vector3 pos, out Quaternion rot, out Vector3 scale)
    {
        pos = default;
        rot = default;
        scale = default;

        if (!PlayerAvatar.TryGetLocal(out var local) || local == null)
            return false;

        if (local.SeatIndex != seatIndex)
            return false;

        if (!pending.active)
            return false;

        if (!SameCard(pending.card, card))
            return false;

        if (Time.time - pending.time > 2f)
            return false;

        pos = pending.pos;
        rot = pending.rot;
        scale = pending.scale;
        return true;
    }


    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return (u * u) * a + (2f * u * t) * b + (t * t) * c;
    }

    private static bool SameCard(CardId a, CardId b)
    {
        return a.type == b.type && a.suit == b.suit && a.value == b.value;
    }

    private struct PendingStart
    {
        public bool active;
        public CardId card;
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
        public float time;
    }
}
