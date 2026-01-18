using PurrNet;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPopup : MonoBehaviour
{
    public static GameOverPopup Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_Text resetVotesText;


    private bool votedReset;

    private static Callback<AvatarImageLoaded_t> avatarLoadedCb;
    private ulong currentSteamId;

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);

        if (avatarLoadedCb == null)
            avatarLoadedCb = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);
    }

    public void Show(PlayerID winner, string winnerName, ulong steamId)
    {
        if (root != null) root.SetActive(true);

        if (titleText != null)
            titleText.text = $"{winnerName}";

        currentSteamId = steamId;

        if (avatarImage != null)
        {
            avatarImage.sprite = null;
            avatarImage.enabled = false;
        }

        if (steamId != 0)
        {
            var spr = TryGetAvatarSprite(steamId);
            if (spr != null && avatarImage != null)
            {
                avatarImage.sprite = spr;
                avatarImage.enabled = true;
            }
        }
    }
    public void OnResetVoteClicked()
    {
        votedReset = !votedReset;
        GameSession.Instance?.Server_VoteReset(votedReset);
    }
    public void OnLobbyClicked() => GameSession.Instance.Server_ReturnToLobby();

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private static Sprite TryGetAvatarSprite(ulong steamId)
    {
        int imageId = SteamFriends.GetLargeFriendAvatar(new CSteamID(steamId));
        if (imageId <= 0) return null;
        return SteamImageToSprite(imageId);
    }

    private void OnAvatarLoaded(AvatarImageLoaded_t cb)
    {
        if (cb.m_iImage <= 0) return;
        if (currentSteamId == 0) return;

        ulong loadedId = cb.m_steamID.m_SteamID;
        if (loadedId != currentSteamId) return;

        var spr = SteamImageToSprite(cb.m_iImage);
        if (spr != null && avatarImage != null)
        {
            avatarImage.sprite = spr;
            avatarImage.enabled = true;
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

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    public void SetResetVotes(int votes, int total, PlayerID[] voters)
    {
        resetVotesText.text = $"{votes}/{total}";
    }
}
