using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Baut die Main-Szene automatisiert per Kommandozeile auf, da hier kein Editor-GUI-Zugriff
// verfügbar ist: Unity.exe -batchmode -projectPath <proj> -executeMethod SceneSetup.CreateMainScene -quit
public static class SceneSetup
{
    [MenuItem("SteamGuard/Create Main Scene")]
    public static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapGO = new GameObject("GameBootstrap");
        bootstrapGO.AddComponent<GameBootstrap>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        bool ok = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        Debug.Log(ok ? "Main.unity erfolgreich gespeichert." : "FEHLER beim Speichern der Szene.");

        var scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        EditorBuildSettings.scenes = scenes;
    }
}
