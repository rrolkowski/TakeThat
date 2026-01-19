using PurrNet;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectView : MonoBehaviour
{
    public static PlayerEffectView Instance { get; private set; }

    [Header("World seat points (Seat1..Seat4)")]
    [SerializeField] private Transform[] worldSeatPoints;

    [Header("Effect anchors (children of Camera/LocalTableUI)")]
    [SerializeField] private Transform effectLeft;
    [SerializeField] private Transform effectTop;
    [SerializeField] private Transform effectRight;
    [SerializeField] private Transform effectBottom;

    [Header("Prefab")]
    [SerializeField] private PlayerEffectIndicator effectPrefab;

    private readonly Dictionary<PlayerID, Transform> anchorByPid = new();
    private readonly Dictionary<PlayerID, PlayerEffectIndicator> indicatorByPid = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetPlayers(PlayerID[] playerIds)
    {
        anchorByPid.Clear();

        if (playerIds == null || playerIds.Length == 0) return;
        if (worldSeatPoints == null || worldSeatPoints.Length == 0) return;

        if (!PlayerAvatar.TryGetLocal(out var local)) return;
        var localPidN = local.owner;

        if (!localPidN.HasValue) return;
        var localPid = localPidN.Value;

        int localSeat = FindNearestSeatIndex(local.transform.position);
        if (localSeat < 0) return;

        int seatCount = worldSeatPoints.Length;

        if (effectBottom != null)
            anchorByPid[localPid] = effectBottom;

        for (int i = 0; i < playerIds.Length; i++)
        {
            var pid = playerIds[i];
            if (pid == localPid) continue;

            if (!PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar) || avatar == null)
                continue;

            int oppSeat = FindNearestSeatIndex(avatar.transform.position);
            if (oppSeat < 0) continue;

            int relative = (oppSeat - localSeat + seatCount) % seatCount;

            Transform anchor = RelativeToEffectAnchor(relative);
            if (anchor != null)
                anchorByPid[pid] = anchor;
        }
    }

    public void ShowEffect(PlayerID targetPid, CardType type, int value)
    {
        if (!anchorByPid.TryGetValue(targetPid, out var anchor) || anchor == null)
            return;

        if (effectPrefab == null)
            return;

        if (!indicatorByPid.TryGetValue(targetPid, out var fx) || fx == null)
        {
            fx = Instantiate(effectPrefab, anchor);
            indicatorByPid[targetPid] = fx;
        }

        if (fx.transform.parent != anchor)
            fx.transform.SetParent(anchor, worldPositionStays: false);

        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;

        switch (type)
        {
            case CardType.Skip:
                fx.ShowBlock();
                break;

            case CardType.Draw2:
            case CardType.Draw3:
                fx.ShowPlus(value);
                break;
        }
    }

    private Transform RelativeToEffectAnchor(int relative)
    {
        if (relative == 1) return effectLeft;
        if (relative == 2) return effectTop;
        if (relative == 3) return effectRight;
        return null;
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
