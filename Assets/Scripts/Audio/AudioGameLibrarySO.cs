using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Game Library", fileName = "AudioGameLibrary")]
public class AudioGameLibrarySO : ScriptableObject
{
    [Header("Music")]
    public AudioMusicDef gameLoop = new AudioMusicDef();

    [Header("Core SFX")]
    public AudioSfxDef playCardFromHand = new AudioSfxDef();
    public AudioSfxDef drawCard = new AudioSfxDef();

    [Header("Turn SFX")]
    public AudioSfxDef yourTurnStart = new AudioSfxDef();
    public AudioSfxDef yourTurnTimeout = new AudioSfxDef();

    [Header("Special Cards SFX")]
    public AudioSfxDef throwBlockCard = new AudioSfxDef();
    public AudioSfxDef throwPlusCard = new AudioSfxDef();

    [Header("End Game")]
    public AudioSfxDef gameEndWinner = new AudioSfxDef();
}
