using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioSfxListener : MonoBehaviour
{
    private const string FallbackGameSceneName = "SampleScene";
    private const string SceneMusicStarterTypeName = "SceneMusicStarter";

    private bool isActiveScene;
    private string[] gameScenesCached;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        CacheGameScenes();
        RefreshActive(SceneManager.GetActiveScene().name);

        GameSession.OnCardPlayedClient += OnCardPlayed;
        GameSession.OnCardsDrawnClient += OnCardsDrawn;
        GameSession.OnTurnStartedClient += OnTurnStarted;
        GameSession.OnTurnTimedOutClient += OnTurnTimedOut;
        GameSession.OnGameOverClient += OnGameOver;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        GameSession.OnCardPlayedClient -= OnCardPlayed;
        GameSession.OnCardsDrawnClient -= OnCardsDrawn;
        GameSession.OnTurnStartedClient -= OnTurnStarted;
        GameSession.OnTurnTimedOutClient -= OnTurnTimedOut;
        GameSession.OnGameOverClient -= OnGameOver;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        CacheGameScenes();
        RefreshActive(newScene.name);
    }

    private void RefreshActive(string sceneName)
    {
        if (gameScenesCached == null || gameScenesCached.Length == 0)
        {
            isActiveScene = (sceneName == FallbackGameSceneName);
            return;
        }

        isActiveScene = gameScenesCached.Contains(sceneName);
    }

    private void CacheGameScenes()
    {
        gameScenesCached = TryReadGameScenesFromStarter();

        if (gameScenesCached == null || gameScenesCached.Length == 0)
            gameScenesCached = new[] { FallbackGameSceneName };
    }

    private static bool IsLocalSeat(int seatIndex)
    {
        if (!PlayerAvatar.TryGetLocal(out var local) || local == null)
            return true; // brak danych => traktuj jako local (bez ściszania)

        return seatIndex == local.SeatIndex;
    }

    private static Vector3 GetSeatAudioPos(int seatIndex)
    {
        if (PileThrowController.Instance != null &&
            PileThrowController.Instance.TryGetAudioWorldPos(seatIndex, out var p))
            return p;

        if (Camera.main != null) return Camera.main.transform.position;
        return Vector3.zero;
    }

    // ========= Event handlers =========

    private void OnCardPlayed(int seatIndex, CardId card, int count)
    {
        if (!isActiveScene || AudioManager.I == null) return;

        Vector3 pos = GetSeatAudioPos(seatIndex);
        bool isOther = !IsLocalSeat(seatIndex);

        if (card.type == CardType.Draw2 || card.type == CardType.Draw3)
            AudioManager.I.ThrowPlusCardAt(pos, isOther);
        else if (card.type == CardType.Reverse)
            AudioManager.I.ThrowReverseCardAt(pos, isOther);
        else if (card.type == CardType.Skip)
            AudioManager.I.ThrowBlockCardAt(pos, isOther);
        else
            AudioManager.I.PlayCardFromHandAt(pos, isOther);

    }

    private void OnCardsDrawn(int seatIndex, int count)
    {
        if (!isActiveScene || AudioManager.I == null) return;

        Vector3 pos = GetSeatAudioPos(seatIndex);
        bool isOther = !IsLocalSeat(seatIndex);

        AudioManager.I.DrawCardAt(pos, isOther);
    }

    private void OnTurnStarted(int seatIndex)
    {
        if (!isActiveScene || AudioManager.I == null) return;

        Vector3 pos = GetSeatAudioPos(seatIndex);
        bool isOther = !IsLocalSeat(seatIndex);

        AudioManager.I.TurnStartAt(pos, isOther);
    }

    private void OnTurnTimedOut(int seatIndex)
    {
        if (!isActiveScene || AudioManager.I == null) return;

        Vector3 pos = GetSeatAudioPos(seatIndex);
        bool isOther = !IsLocalSeat(seatIndex);

        AudioManager.I.TurnTimeoutAt(pos, isOther);
    }

    private void OnGameOver(int winnerSeatIndex)
    {
        if (!isActiveScene || AudioManager.I == null) return;

        bool isOther = !IsLocalSeat(winnerSeatIndex);
        AudioManager.I.GameEndWinner2D(isOther); // zawsze 2D
    }

    // ========= Reflection: pobierz string[] gameScenes z SceneMusicStarter =========

    private static string[] TryReadGameScenesFromStarter()
    {
        try
        {
            var t = FindTypeByName(SceneMusicStarterTypeName);
            if (t == null) return null;

            var starter = FindObjectOfType(t);
            if (starter == null) return null;

            // Najpierw spróbuj dokładnie: "gameScenes"
            var exact = ReadStringArrayField(starter, t, "gameScenes");
            if (exact != null && exact.Length > 0) return exact;

            // Fallback: znajdź pierwsze pole string[] z "game" i "scene" w nazwie
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(string[])) continue;

                string n = f.Name.ToLowerInvariant();
                if (!n.Contains("game")) continue;
                if (!n.Contains("scene")) continue;

                var arr = f.GetValue(starter) as string[];
                if (arr != null && arr.Length > 0) return arr;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string[] ReadStringArrayField(object instance, Type t, string fieldName)
    {
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string[]))
            return f.GetValue(instance) as string[];

        return null;
    }

    private static Type FindTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var found = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                if (found != null) return found;
            }
            catch
            {
                // ignore
            }
        }
        return null;
    }
}
