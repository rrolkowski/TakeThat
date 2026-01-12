using PurrNet;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OpponentHandsView : MonoBehaviour
{
    public static OpponentHandsView Instance { get; private set; }

    [Header("World seat points (Seat1..Seat4) - used ONLY to detect who sits where")]
    [SerializeField] private Transform[] worldSeatPoints;

    [Header("Local UI anchors (children of Camera/LocalTableUI)")]
    [SerializeField] private Transform uiLeft;
    [SerializeField] private Transform uiTop;
    [SerializeField] private Transform uiRight;

    [Header("Prefabs")]
    [SerializeField] private GameObject backCardPrefab;

    [Header("Fan Visual")]
    [SerializeField] private int maxBackCards = 10;
    [SerializeField] private float fanRadius = 0.9f;
    [SerializeField] private float fanAngleRange = 35f;
    [SerializeField] private float zStep = 0.001f;
    [SerializeField] private Vector3 fanOffset = Vector3.zero;

    private readonly Dictionary<PlayerID, List<GameObject>> spawnedBacks = new();
    private readonly Dictionary<PlayerID, TMP_Text> spawnedTexts = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetCounts(PlayerID[] playerIds, int[] counts)
    {
        if (playerIds == null || counts == null) return;
        if (worldSeatPoints == null || worldSeatPoints.Length == 0) return;
        if (uiLeft == null || uiTop == null || uiRight == null) return;

        if (!PlayerAvatar.TryGetLocal(out var local)) return;
        var localId = local.owner;

        int localSeat = FindNearestSeatIndex(local.transform.position);
        if (localSeat < 0) return;

        int seatCount = worldSeatPoints.Length;

        for (int i = 0; i < playerIds.Length && i < counts.Length; i++)
        {
            var pid = playerIds[i];
            if (pid == localId) continue;

            if (!PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar)) continue;

            int oppSeat = FindNearestSeatIndex(avatar.transform.position);
            if (oppSeat < 0) continue;

            int relative = (oppSeat - localSeat + seatCount) % seatCount;

            // relative=1 left, 2 top, 3 right (dla 4 graczy)
            Transform anchor = RelativeToAnchor(relative);
            if (anchor == null) continue;

            RenderOpponent(pid, anchor, counts[i]);
        }
    }

    private Transform RelativeToAnchor(int relative)
    {
        // Zak³adamy 4 seaty:
        // 0 = ja (ignorujemy)
        // 1 = left, 2 = top, 3 = right
        if (relative == 1) return uiLeft;
        if (relative == 2) return uiTop;
        if (relative == 3) return uiRight;
        return null;
    }

    private void RenderOpponent(PlayerID pid, Transform anchor, int count)
    {
        Clear(pid);

        int toShow = Mathf.Min(count, maxBackCards);
        var list = new List<GameObject>(toShow);

        for (int k = 0; k < toShow; k++)
        {
            float t = toShow <= 1 ? 0.5f : (k / (float)(toShow - 1));
            float deg = Mathf.Lerp(-fanAngleRange * 0.5f, fanAngleRange * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;

            // ³uk w lokalnych wspó³rzêdnych anchoru
            var pos = new Vector3(Mathf.Sin(rad) * fanRadius, Mathf.Cos(rad) * fanRadius, 0f);
            pos += fanOffset;
            pos.z = -zStep * k;

            var rot = Quaternion.Euler(0f, 0f, -deg);

            var go = Instantiate(backCardPrefab, anchor);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;

            list.Add(go);
        }

        spawnedBacks[pid] = list;
    }

    private int FindNearestSeatIndex(Vector3 p)
    {
        int best = -1;
        float bestD = float.PositiveInfinity;

        for (int i = 0; i < worldSeatPoints.Length; i++)
        {
            var s = worldSeatPoints[i];
            if (s == null) continue;

            float d = (s.position - p).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }

        return best;
    }

    private void Clear(PlayerID pid)
    {
        if (spawnedBacks.TryGetValue(pid, out var backs))
        {
            for (int i = 0; i < backs.Count; i++)
                if (backs[i] != null) Destroy(backs[i]);
            spawnedBacks.Remove(pid);
        }

        if (spawnedTexts.TryGetValue(pid, out var txt) && txt != null)
            txt.gameObject.SetActive(false);
    }
}
