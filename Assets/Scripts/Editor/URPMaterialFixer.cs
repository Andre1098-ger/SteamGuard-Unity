using System.IO;
using UnityEditor;
using UnityEngine;

// Das importierte Turret-Asset-Pack liefert Materialien mit dem alten Built-in-"Standard"-Shader.
// Unter URP rendert das pink. Dieses Skript stellt die Materialien im Paket-Ordner auf
// "Universal Render Pipeline/Lit" um und übernimmt dabei alle Texturen/Farben/Werte.
// Aufruf: Unity.exe -batchmode -nographics -projectPath <proj> -executeMethod URPMaterialFixer.Run -logFile <log>
public static class URPMaterialFixer
{
    const string FolderPath = "Assets/TD_Sci-Fi_Turret1_Example/Materials";

    public static void Run()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP_MATFIX_FAIL shader_not_found");
            EditorApplication.Exit(1);
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { FolderPath });
        int fixedCount = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == urpLit) continue;

            var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            var bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            var occMap = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
            var emisMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            var emisColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            var metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            var glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

            mat.shader = urpLit;

            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);
            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (occMap != null) mat.SetTexture("_OcclusionMap", occMap);
            if (emisMap != null) mat.SetTexture("_EmissionMap", emisMap);
            mat.SetColor("_EmissionColor", emisColor);
            if (emisMap != null || emisColor.maxColorComponent > 0f)
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (emisMap != null || emisColor.maxColorComponent > 0f) mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", glossiness);

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"URP_MATFIX_OK count={fixedCount}");
        EditorApplication.Exit(0);
    }
}
