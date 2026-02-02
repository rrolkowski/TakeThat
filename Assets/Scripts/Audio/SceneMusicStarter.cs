using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicStarter : MonoBehaviour
{
    [Header("Scene names -> which music to play")]
    [SerializeField] private string[] mainMenuScenes = { "LobbySample", "NetworkedScene" };
    [SerializeField] private string[] gameScenes = { "SampleScene" };

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Odpal od razu dla sceny, w której aktualnie jesteś (np. jak startujesz Play z tej sceny)
        ApplyMusicFor(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMusicFor(scene.name);
    }

    private void ApplyMusicFor(string sceneName)
    {
        if (AudioManager.I == null) return;

        if (IsIn(sceneName, mainMenuScenes))
        {
            AudioManager.I.PlayMainMenuMusic();
            return;
        }

        if (IsIn(sceneName, gameScenes))
        {
            AudioManager.I.PlayGameMusic();
            return;
        }

        // Jeśli scena nie jest na liście, nic nie robimy (albo możesz tu dać domyślne)
    }

    private bool IsIn(string name, string[] list)
    {
        for (int i = 0; i < list.Length; i++)
            if (list[i] == name) return true;
        return false;
    }
}
