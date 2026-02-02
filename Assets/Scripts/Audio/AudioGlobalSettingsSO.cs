using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Global Settings", fileName = "AudioGlobalSettings")]
public class AudioGlobalSettingsSO : ScriptableObject
{
    [Header("Music")]
    [Tooltip("Fade in/out przy zmianie muzyki (sekundy). 0 = brak fade.")]
    [Min(0f)]
    public float musicFadeSeconds = 0.35f;

    [Header("3D SFX")]
    [Tooltip("Min distance (3D).")]
    [Min(0.01f)]
    public float sfx3DMinDistance = 2f;

    [Tooltip("Max distance (3D).")]
    [Min(0.02f)]
    public float sfx3DMaxDistance = 6f;

    [Header("Other Player")]
    [Tooltip("Mnożnik głośności dla akcji innych graczy.")]
    [Range(0f, 1f)]
    public float otherPlayerVolumeMul = 0.2f;
}
