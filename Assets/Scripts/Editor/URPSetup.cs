using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Erstellt ein URP-Pipeline-Asset samt Renderer und aktiviert es projektweit (Graphics- und
// alle Quality-Level-Einstellungen), damit die veraltete Built-in Render Pipeline abgelöst wird.
// Aufruf: Unity.exe -batchmode -projectPath <proj> -executeMethod URPSetup.Run -quit -logFile <log>
public static class URPSetup
{
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Settings");

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/Settings/SteamGuard_Renderer.asset");

        var pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
        pipelineAsset.supportsHDR = true;
        pipelineAsset.msaaSampleCount = 4;
        pipelineAsset.supportsCameraDepthTexture = true;
        pipelineAsset.supportsCameraOpaqueTexture = true;
        AssetDatabase.CreateAsset(pipelineAsset, "Assets/Settings/SteamGuard_URP.asset");

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool ok = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;
        Debug.Log(ok ? "URP_SETUP_OK" : "URP_SETUP_FAIL currentRenderPipeline nicht URP");
    }
}
