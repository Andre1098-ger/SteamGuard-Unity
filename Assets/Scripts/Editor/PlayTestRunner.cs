using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Startet den Play-Mode im Batch-Modus, lässt das Spiel ein paar Sekunden laufen und
// protokolliert den Live-Zustand, damit Laufzeitfehler ohne Editor-GUI sichtbar werden.
// Übersteht den Domain-Reload beim Play-Mode-Start via EditorPrefs (statische Felder
// werden sonst beim Reload zurückgesetzt und der Callback geht verloren).
// Aufruf: Unity.exe -batchmode -projectPath <proj> -executeMethod PlayTestRunner.Run -quit
[InitializeOnLoad]
public static class PlayTestRunner
{
    const string ActiveKey = "SteamGuard_PlayTestActive";
    static double startTime = -1;

    static PlayTestRunner()
    {
        if (SessionState.GetBool(ActiveKey, false))
        {
            EditorApplication.update += OnUpdate;
        }
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.update += OnUpdate;
        EditorApplication.isPlaying = true;
    }

    static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;
        if (startTime < 0) startTime = EditorApplication.timeSinceStartup;

        double elapsed = EditorApplication.timeSinceStartup - startTime;
        if (elapsed >= 5.0)
        {
            EditorApplication.update -= OnUpdate;
            SessionState.SetBool(ActiveKey, false);

            var gb = GameBootstrap.Instance;
            if (gb != null)
                Debug.Log($"PLAYTEST_RESULT gold={gb.Gold} lives={gb.Lives} enemies={gb.Enemies.Count}");
            else
                Debug.LogError("PLAYTEST_RESULT_FAIL instance_null");

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
}
