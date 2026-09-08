using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Installiert das URP-Paket über den Package Manager (löst automatisch die zur Editor-Version
// passende Paketversion auf, statt eine Versionsnummer zu raten).
// Aufruf: Unity.exe -batchmode -projectPath <proj> -executeMethod PackageInstaller.InstallURP -logFile <log>
public static class PackageInstaller
{
    static AddRequest request;

    public static void InstallURP()
    {
        request = Client.Add("com.unity.render-pipelines.universal");
        EditorApplication.update += Progress;
    }

    static void Progress()
    {
        if (!request.IsCompleted) return;
        EditorApplication.update -= Progress;

        if (request.Status == StatusCode.Success)
            Debug.Log($"PACKAGE_INSTALL_OK {request.Result.packageId}");
        else
            Debug.LogError($"PACKAGE_INSTALL_FAIL {request.Error?.message}");

        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}
