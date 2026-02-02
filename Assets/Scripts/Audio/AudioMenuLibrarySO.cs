using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Menu Library", fileName = "AudioMenuLibrary")]
public class AudioMenuLibrarySO : ScriptableObject
{
    [Header("Music")]
    public AudioMusicDef mainMenuLoop = new AudioMusicDef();

    [Header("UI SFX")]
    public AudioSfxDef uiHighlightButton = new AudioSfxDef();
    public AudioSfxDef uiClickButton = new AudioSfxDef();
    public AudioSfxDef uiClickReadyButton = new AudioSfxDef();
}
