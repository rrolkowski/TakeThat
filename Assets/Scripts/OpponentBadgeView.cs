using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class OpponentBadgesView : MonoBehaviour
{
    public static OpponentBadgesView Instance { get; private set; }

    [Header("World seat points (Seat1..Seat4)")]
    [SerializeField] private Transform[] worldSeatPoints;

    [Header("Local UI anchors (children of Camera/LocalTableUI)")]
    [SerializeField] private Transform uiLeft;
    [SerializeField] private Transform uiTop;
    [SerializeField] private Transform uiRight;

    [Header("Prefab")]
    [SerializeField] private PlayerBadge badgePrefab;

    private readonly Dictionary<PlayerID, PlayerBadge> badges = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetPlayers(PlayerID[] playerIds, int[] counts)
    {
        if (playerIds == null || counts == null) return;
        if (worldSeatPoints == null || worldSeatPoints.Length == 0) return;
        if (badgePrefab == null) return;

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

            if (!badges.TryGetValue(pid, out var badge) || badge == null)
            {
                badge = Instantiate(badgePrefab, anchor);
                badges[pid] = badge;
            }

            // Na razie nazwa = PlayerID. Potem podmienisz na nick z lobby.
            badge.SetName(pid.ToString());
            badge.SetCount(counts[i]);

            badge.transform.localPosition = Vector3.zero;
            badge.transform.localRotation = Quaternion.identity;
            badge.gameObject.SetActive(true);
        }
    }

    private Transform RelativeToAnchor(int relative)
    {
        if (relative == 1) return uiLeft;
        if (relative == 2) return uiTop;
        if (relative == 3) return uiRight;
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
