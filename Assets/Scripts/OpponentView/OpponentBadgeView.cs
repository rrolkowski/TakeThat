using PurrNet;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
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

    private static readonly Dictionary<ulong, Sprite> avatarSpriteCache = new();
    private static Callback<AvatarImageLoaded_t> avatarLoadedCb;

    private void Awake()
    {
        Instance = this;

        if (avatarLoadedCb == null)
            avatarLoadedCb = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
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

            if (!PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar) || avatar == null)
                continue;

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

            badge.SetName(avatar.DisplayName);
            badge.SetCount(counts[i]);

            if (avatar.SteamId != 0)
                badge.SetIcon(TryGetAvatarSprite(avatar.SteamId));
            else
                badge.SetIcon(null);

            badge.transform.localPosition = Vector3.zero;
            badge.transform.localRotation = Quaternion.identity;
            badge.gameObject.SetActive(true);
        }

        var alive = new HashSet<PlayerID>(playerIds);
        alive.Remove((PlayerID)localId);

        foreach (var pid in badges.Keys.ToArray())
        {
            if (!alive.Contains(pid))
            {
                if (badges[pid] != null) Destroy(badges[pid].gameObject);
                badges.Remove(pid);
            }
        }
    }

    private static Sprite TryGetAvatarSprite(ulong steamId)
    {
        if (steamId == 0) return null;

        if (avatarSpriteCache.TryGetValue(steamId, out var cached) && cached != null)
            return cached;

        int imageId = SteamFriends.GetLargeFriendAvatar(new CSteamID(steamId));

        if (imageId <= 0) return null;

        var sprite = SteamImageToSprite(imageId);
        if (sprite != null)
            avatarSpriteCache[steamId] = sprite;

        return sprite;
    }

    private static void OnAvatarLoaded(AvatarImageLoaded_t cb)
    {
        if (cb.m_iImage <= 0) return;

        ulong steamId = cb.m_steamID.m_SteamID;

        var sprite = SteamImageToSprite(cb.m_iImage);
        if (sprite != null)
            avatarSpriteCache[steamId] = sprite;

        if (Instance == null || sprite == null) return;

        foreach (var kv in Instance.badges)
        {
            var pid = kv.Key;
            var badge = kv.Value;
            if (badge == null) continue;

            if (PlayerAvatar.allPlayers.TryGetValue(pid, out var avatar) && avatar != null)
            {
                if (avatar.SteamId == steamId)
                    badge.SetIcon(sprite);
            }
        }
    }

    private static Sprite SteamImageToSprite(int imageId)
    {
        if (!SteamUtils.GetImageSize(imageId, out uint w, out uint h)) return null;
        if (w == 0 || h == 0) return null;

        byte[] rgba = new byte[w * h * 4];
        if (!SteamUtils.GetImageRGBA(imageId, rgba, rgba.Length)) return null;

        var tex = new Texture2D((int)w, (int)h, TextureFormat.RGBA32, false, true);
        tex.LoadRawTextureData(rgba);
        tex.Apply(false, true);

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
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
