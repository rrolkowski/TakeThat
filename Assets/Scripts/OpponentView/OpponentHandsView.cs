using PurrNet;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Fan Visual (base)")]
    [SerializeField] private int maxBackCards = 10;
    [SerializeField] private float fanRadius = 0.9f;
    [SerializeField] private float fanAngleRange = 35f;
    [SerializeField] private float zStep = 0.001f;
    [SerializeField] private Vector3 fanOffset = Vector3.zero;

    [Header("Dynamic Fan (like HandFan)")]
    [SerializeField] private bool useDynamic = true;
    [SerializeField] private int minCards = 1;
    [SerializeField] private int maxCards = 10;

    [SerializeField] private float minAngleRange = 8f;
    [SerializeField] private float maxAngleRange = 35f;

    [SerializeField] private float minArcFlatten = 1f;
    [SerializeField] private float maxArcFlatten = 0.75f;

    [SerializeField] private float rotationFlatten = 0.6f;

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
        if (backCardPrefab == null) return;

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

            Transform anchor = RelativeToAnchor(relative);
            if (anchor == null) continue;

            RenderOpponent(pid, anchor, counts[i]);
        }

        var alive = new HashSet<PlayerID>(playerIds);
        alive.Remove((PlayerID)localId);

        foreach (var pid in spawnedBacks.Keys.ToArray())
        {
            if (!alive.Contains(pid))
            {
                foreach (var go in spawnedBacks[pid])
                    if (go != null) Destroy(go);

                spawnedBacks.Remove(pid);

                if (spawnedTexts.TryGetValue(pid, out var txt) && txt != null)
                    Destroy(txt.gameObject);

                spawnedTexts.Remove(pid);
            }
        }
    }

    private Transform RelativeToAnchor(int relative)
    {
        if (relative == 1) return uiLeft;
        if (relative == 2) return uiTop;
        if (relative == 3) return uiRight;
        return null;
    }

    private void RenderOpponent(PlayerID pid, Transform anchor, int count)
    {
        int toShow = Mathf.Min(count, maxBackCards);

        if (!spawnedBacks.TryGetValue(pid, out var list) || list == null)
        {
            list = new List<GameObject>(toShow);
            spawnedBacks[pid] = list;
        }

        while (list.Count < toShow)
        {
            var go = Instantiate(backCardPrefab, anchor);
            list.Add(go);
        }

        for (int i = toShow; i < list.Count; i++)
        {
            if (list[i] != null) list[i].SetActive(false);
        }

        if (toShow == 0) return;

        float effectiveAngleRange = fanAngleRange;
        float effectiveArcFlatten = 1f;

        if (useDynamic && maxCards > minCards)
        {
            int clamped = Mathf.Clamp(toShow, minCards, maxCards);
            float u = (clamped - minCards) / (float)(maxCards - minCards);

            effectiveAngleRange = Mathf.Lerp(minAngleRange, maxAngleRange, u);
            effectiveArcFlatten = Mathf.Lerp(minArcFlatten, maxArcFlatten, u);
        }
        else if (useDynamic && maxCards <= minCards)
        {
            effectiveAngleRange = minAngleRange;
            effectiveArcFlatten = minArcFlatten;
        }

        var positions = new Vector3[toShow];
        var rotations = new Quaternion[toShow];

        for (int k = 0; k < toShow; k++)
        {
            float t = toShow == 1 ? 0.5f : k / (float)(toShow - 1);
            float deg = Mathf.Lerp(-effectiveAngleRange * 0.5f, effectiveAngleRange * 0.5f, t);
            float rad = deg * Mathf.Deg2Rad;

            var pos = new Vector3(
                Mathf.Sin(rad) * fanRadius,
                Mathf.Cos(rad) * fanRadius * effectiveArcFlatten,
                0f
            );

            pos += fanOffset;
            pos.z = -zStep * k;

            positions[k] = pos;
            rotations[k] = Quaternion.Euler(0f, 0f, -deg * rotationFlatten);
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < toShow; i++) center += positions[i];
        center /= toShow;

        for (int i = 0; i < toShow; i++)
            positions[i] -= new Vector3(center.x, center.y, 0f);

        for (int k = 0; k < toShow; k++)
        {
            var go = list[k];
            if (go == null) continue;

            go.SetActive(true);

            if (go.transform.parent != anchor)
                go.transform.SetParent(anchor, worldPositionStays: false);

            go.transform.localPosition = positions[k];
            go.transform.localRotation = rotations[k];

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = k;
        }
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
}

