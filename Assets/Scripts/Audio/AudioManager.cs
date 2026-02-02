using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Libraries (SO)")]
    [SerializeField] private AudioGlobalSettingsSO globalSettings;
    [SerializeField] private AudioMenuLibrarySO menuLibrary;
    [SerializeField] private AudioGameLibrarySO gameLibrary;

    private const int SfxPoolSize = 8;

    [HideInInspector][SerializeField] private AudioSource musicSource;
    [HideInInspector] private AudioSource[] sfxSources;
    [HideInInspector] private int sfxIndex;

    private readonly Dictionary<AudioSfxDef, int> lastPick = new Dictionary<AudioSfxDef, int>();
    private Coroutine musicFadeRoutine;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureSources();
    }

    private void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        var existingRoot = transform.Find("SFX_POOL");
        if (existingRoot != null)
            Destroy(existingRoot.gameObject);

        var poolRoot = new GameObject("SFX_POOL");
        poolRoot.transform.SetParent(transform, worldPositionStays: false);

        sfxSources = new AudioSource[SfxPoolSize];
        for (int i = 0; i < SfxPoolSize; i++)
        {
            var go = new GameObject($"SFX_{i}");
            go.transform.SetParent(poolRoot.transform, worldPositionStays: true);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.dopplerLevel = 0f;
            src.spatialBlend = 0f; // default 2D
            src.panStereo = 0f;

            sfxSources[i] = src;
        }

        sfxIndex = 0;
    }

    private AudioSource NextSfx()
    {
        var src = sfxSources[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
        return src;
    }

    private float MusicFadeSeconds => globalSettings != null ? Mathf.Max(0f, globalSettings.musicFadeSeconds) : 0f;
    private float OtherMul => globalSettings != null ? Mathf.Clamp01(globalSettings.otherPlayerVolumeMul) : 1f;
    private float SfxMinDist => globalSettings != null ? Mathf.Max(0.01f, globalSettings.sfx3DMinDistance) : 2f;
    private float SfxMaxDist
    {
        get
        {
            float min = SfxMinDist;
            float max = globalSettings != null ? globalSettings.sfx3DMaxDistance : 18f;
            return Mathf.Max(min + 0.01f, max);
        }
    }

    // =========================
    // MUSIC
    // =========================
    private void PlayMusic(AudioMusicDef m)
    {
        if (m == null || m.clip == null) return;

        if (musicSource.isPlaying && musicSource.clip == m.clip)
            return;

        float fade = MusicFadeSeconds;

        if (!musicSource.isPlaying || fade <= 0f)
        {
            musicSource.clip = m.clip;
            musicSource.loop = m.loop;
            musicSource.volume = m.volume;
            musicSource.pitch = 1f;
            musicSource.Play();
            return;
        }

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(FadeSwitchMusic(m, fade));
    }

    private IEnumerator FadeSwitchMusic(AudioMusicDef m, float fade)
    {
        float startVol = musicSource.volume;
        float t = 0f;

        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = m.clip;
        musicSource.loop = m.loop;
        musicSource.pitch = 1f;
        musicSource.Play();

        t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, m.volume, t / fade);
            yield return null;
        }

        musicSource.volume = m.volume;
        musicFadeRoutine = null;
    }

    // =========================
    // SFX internals
    // =========================
    private AudioClip ChooseRandomClip(AudioSfxDef s)
    {
        if (s?.clips == null || s.clips.Length == 0) return null;
        if (s.clips.Length == 1) return s.clips[0];

        int idx = Random.Range(0, s.clips.Length);
        if (lastPick.TryGetValue(s, out int lastIdx) && idx == lastIdx)
            idx = (idx + 1) % s.clips.Length;

        lastPick[s] = idx;
        return s.clips[idx];
    }

    private void ApplyPitchVolume(AudioSource src, AudioSfxDef s, float volumeMul)
    {
        float min = Mathf.Min(s.pitchMin, s.pitchMax);
        float max = Mathf.Max(s.pitchMin, s.pitchMax);
        src.pitch = Random.Range(min, max);

        src.volume = Mathf.Clamp01(s.volume * Mathf.Clamp01(volumeMul));
    }

    private void PlaySfx2D(AudioSfxDef s, float volumeMul = 1f)
    {
        if (s == null) return;

        var clip = ChooseRandomClip(s);
        if (clip == null) return;

        var src = NextSfx();
        src.spatialBlend = 0f;
        src.panStereo = 0f;

        src.clip = clip;
        src.loop = false;

        ApplyPitchVolume(src, s, volumeMul);
        src.Play();
    }

    private void PlaySfx3D(AudioSfxDef s, Vector3 worldPos, float volumeMul = 1f)
    {
        if (s == null) return;

        var clip = ChooseRandomClip(s);
        if (clip == null) return;

        var src = NextSfx();
        src.transform.position = worldPos;

        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = SfxMinDist;
        src.maxDistance = SfxMaxDist;
        src.dopplerLevel = 0f;
        src.panStereo = 0f;

        src.clip = clip;
        src.loop = false;

        ApplyPitchVolume(src, s, volumeMul);
        src.Play();
    }

    // =========================
    // PUBLIC API
    // =========================

    public void PlayMainMenuMusic()
    {
        if (menuLibrary == null) return;
        PlayMusic(menuLibrary.mainMenuLoop);
    }

    public void PlayGameMusic()
    {
        if (gameLibrary == null) return;
        PlayMusic(gameLibrary.gameLoop);
    }

    // UI (2D)
    public void UiHighlight()
    {
        if (menuLibrary == null) return;
        PlaySfx2D(menuLibrary.uiHighlightButton);
    }

    public void UiClick()
    {
        if (menuLibrary == null) return;
        PlaySfx2D(menuLibrary.uiClickButton);
    }

    public void UiReadyClick()
    {
        if (menuLibrary == null) return;
        PlaySfx2D(menuLibrary.uiClickReadyButton);
    }

    // GAME (3D)
    public void PlayCardFromHandAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.playCardFromHand, pos, isOtherPlayer ? OtherMul : 1f);
    }

    public void DrawCardAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.drawCard, pos, isOtherPlayer ? OtherMul : 1f);
    }

    public void TurnStartAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.yourTurnStart, pos, isOtherPlayer ? OtherMul : 1f);
    }

    public void TurnTimeoutAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.yourTurnTimeout, pos, isOtherPlayer ? OtherMul : 1f);
    }

    public void ThrowBlockCardAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.throwBlockCard, pos, isOtherPlayer ? OtherMul : 1f);
    }

    public void ThrowPlusCardAt(Vector3 pos, bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx3D(gameLibrary.throwPlusCard, pos, isOtherPlayer ? OtherMul : 1f);
    }

    // GAME OVER (2D)
    public void GameEndWinner2D(bool isOtherPlayer)
    {
        if (gameLibrary == null) return;
        PlaySfx2D(gameLibrary.gameEndWinner, isOtherPlayer ? OtherMul : 1f);
    }
}
