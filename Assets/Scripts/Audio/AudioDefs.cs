using UnityEngine;

[System.Serializable]
public class AudioMusicDef
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.7f;
    public bool loop = true;
}

[System.Serializable]
public class AudioSfxDef
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitchMin = 1f;
    [Range(0.5f, 2f)] public float pitchMax = 1f;
}
