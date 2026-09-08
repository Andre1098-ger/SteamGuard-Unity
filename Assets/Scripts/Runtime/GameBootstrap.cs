using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// SteamGuard (Unity-Portierung) - Grundgerüst, das komplett per Code aufgebaut wird
// (kein manuelles Szenen-Wiring nötig), analog zum ursprünglichen Three.js-Prototyp.
// Einfach dieses Skript auf ein leeres GameObject in der Szene legen und Play drücken.
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    const int COLS = 15;
    const int ROWS = 10;
    const float CELL = 1f;

    static readonly int[,] PathCells = new int[,]
    {
        {0,2},{1,2},{2,2},{3,2},{3,3},{3,4},
        {4,4},{5,4},{6,4},{6,3},{6,2},{6,1},
        {7,1},{8,1},{9,1},{10,1},{11,1},{11,2},
        {11,3},{11,4},{11,5},{10,5},{9,5},{8,5},
        {8,6},{8,7},{8,8},{9,8},{10,8},{11,8},
        {12,8},{13,8},{14,8},{14,9}
    };

    List<Vector3> waypoints = new List<Vector3>();
    HashSet<(int, int)> pathSet = new HashSet<(int, int)>();

    List<EnemyController> enemies = new List<EnemyController>();
    TowerController tower;

    int gold = 150;
    int lives = 20;

    public int Gold => gold;
    public int Lives => lives;

    static Vector3 CellToWorld(int cx, int cy)
    {
        return new Vector3(cx - COLS / 2f + 0.5f, 0f, cy - ROWS / 2f + 0.5f);
    }

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < PathCells.GetLength(0); i++)
        {
            int cx = PathCells[i, 0], cy = PathCells[i, 1];
            pathSet.Add((cx, cy));
            waypoints.Add(CellToWorld(cx, cy));
        }

        LoadModels();
        SetupCamera();
        SetupPostProcessing();
        SetupLighting();
        BuildGround();
        BuildCoreMarker();

        // Ein Turm auf einem freien Feld nahe dem Pfadanfang
        tower = BuildTower(CellToWorld(1, 1));

        StartCoroutine(SpawnLoop());
    }

    void SetupCamera()
    {
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 13f, -11f);
        camGO.transform.LookAt(new Vector3(0f, 0f, 0.5f));
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.035f, 0.024f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.02f;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    // Ohne Himmel hat Unity keine Reflexionsquelle - glänzendes Metall wirkt dann komplett
    // flach/matt statt poliert. Ein einfacher prozeduraler Skybox behebt das.
    void SetupSkyboxAndReflections()
    {
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null) { Debug.LogWarning("Skybox/Procedural-Shader nicht gefunden - Reflexionen bleiben flach."); return; }
        var skyMat = new Material(skyShader);
        skyMat.SetColor("_SkyTint", new Color(0.22f, 0.17f, 0.11f));
        skyMat.SetColor("_GroundColor", new Color(0.05f, 0.035f, 0.02f));
        skyMat.SetFloat("_AtmosphereThickness", 0.7f);
        skyMat.SetFloat("_Exposure", 1.0f);
        RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.reflectionIntensity = 1f;
        DynamicGI.UpdateEnvironment();
    }

    // Bloom + leichte Farbkorrektur/Vignette über das URP-Volume-System - der größte einzelne
    // Schritt weg vom "flachen Browser-Look", genau wie schon in der Three.js-Version.
    void SetupPostProcessing()
    {
        var cam = Camera.main;
        var camData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.2f);
        bloom.intensity.Override(0.25f);
        bloom.scatter.Override(0.35f);

        var colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.1f);
        colorAdjustments.contrast.Override(8f);
        colorAdjustments.saturation.Override(5f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.18f);
        vignette.smoothness.Override(0.6f);

        var volumeGO = new GameObject("GlobalVolume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.weight = 1f;
        volume.profile = profile;
    }

    void SetupLighting()
    {
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.88f, 0.7f);
        sun.intensity = 3.2f;
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        var fillGO = new GameObject("FillLight");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.55f, 0.65f, 0.8f);
        fill.intensity = 0.6f;
        fillGO.transform.rotation = Quaternion.Euler(35f, 145f, 0f);

        SetupSkyboxAndReflections();
    }

    // Sucht den URP-Lit-Shader (fällt auf Built-in "Standard" zurück, falls URP mal nicht aktiv sein sollte)
    static Shader litShader;
    static Shader LitShader => litShader != null ? litShader : (litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

    public static Material MakeMaterial(Color c, float metallic = 0.3f, float smoothness = 0.4f)
    {
        var mat = new Material(LitShader);
        mat.color = c; // Unity mappt das dank [MainColor]-Tag automatisch auf _BaseColor (URP) bzw. _Color (Built-in)
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    void BuildGround()
    {
        var floorMatA = MakeMaterial(new Color(0.14f, 0.10f, 0.06f));
        var floorMatB = MakeMaterial(new Color(0.11f, 0.08f, 0.05f));
        var pathMat = MakeMaterial(new Color(0.23f, 0.16f, 0.09f), 0.2f, 0.3f);

        var groundParent = new GameObject("Ground").transform;
        for (int cx = 0; cx < COLS; cx++)
        {
            for (int cy = 0; cy < ROWS; cy++)
            {
                bool isPath = pathSet.Contains((cx, cy));
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = isPath ? $"Path_{cx}_{cy}" : $"Tile_{cx}_{cy}";
                var pos = CellToWorld(cx, cy);
                tile.transform.position = new Vector3(pos.x, isPath ? -0.03f : 0f, pos.z);
                tile.transform.localScale = new Vector3(CELL * 0.98f, 0.12f, CELL * 0.98f);
                tile.transform.parent = groundParent;
                var rend = tile.GetComponent<Renderer>();
                rend.material = isPath ? pathMat : (((cx + cy) % 2 == 0) ? floorMatA : floorMatB);
            }
        }
    }

    void BuildCoreMarker()
    {
        var end = waypoints[waypoints.Count - 1];
        var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "FabrikKern";
        core.transform.position = end + Vector3.up * 0.3f;
        core.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);
        var mat = MakeMaterial(new Color(0.9f, 0.15f, 0.08f), 0.4f, 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1.2f, 0.15f, 0.05f));
        core.GetComponent<Renderer>().material = mat;

        var coreLightGO = new GameObject("KernLicht");
        var coreLight = coreLightGO.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = new Color(1f, 0.25f, 0.1f);
        coreLight.range = 5f;
        coreLight.intensity = 2.5f;
        coreLightGO.transform.position = end + Vector3.up;
    }

    // 3 Skin-Varianten desselben hochwertigen Sci-Fi-Turret-Modells (echte PBR-Texturen:
    // Diffuse/Normal/AO/Emission) - Ersatz für die einfachen Kenney-Primitive-Modelle.
    GameObject[] turretTierPrefabs = new GameObject[3];

    // Zielhöhe (Weltunits), auf die jedes importierte Turret-Prefab automatisch skaliert wird,
    // damit es unabhängig vom Export-Maßstab zur 1x1-Gitterzelle passt.
    const float TurretTargetHeight = 1.5f;

    void LoadModels()
    {
        turretTierPrefabs[0] = Resources.Load<GameObject>("Models/Turret1/turret_1_1");
        turretTierPrefabs[1] = Resources.Load<GameObject>("Models/Turret1/turret_1_2");
        turretTierPrefabs[2] = Resources.Load<GameObject>("Models/Turret1/turret_1_3");
        if (turretTierPrefabs[0] == null) Debug.LogError("Konnte Turret-Modell 'turret_1_1' nicht aus Resources/Models/Turret1 laden!");
    }

    TowerController BuildTower(Vector3 pos)
    {
        var prefab = turretTierPrefabs[0];

        var root = new GameObject("Geschützturm_Tier1");
        root.transform.position = pos;

        Transform headRoot = root.transform;
        Vector3 muzzleLocal = new Vector3(0f, 0f, 0.6f);

        if (prefab != null)
        {
            var inst = Instantiate(prefab, root.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            AutoScaleToHeight(inst, TurretTargetHeight);

            var mount = FindDeepChild(inst.transform, "mount");
            headRoot = mount != null ? mount : inst.transform;

            var barrelPart = FindDeepChild(headRoot, "barrel");
            if (barrelPart != null)
            {
                Vector3 tipWorld = FindMuzzleTip(barrelPart);
                muzzleLocal = headRoot.InverseTransformPoint(tipWorld);
            }
        }

        var tc = root.AddComponent<TowerController>();
        tc.Init(this, headRoot, muzzleLocal);
        return tc;
    }

    // Skaliert ein importiertes Modell gleichmäßig auf eine Ziel-Weltraumhöhe, damit der
    // ursprüngliche FBX-Export-Maßstab (der beliebig sein kann) keine Rolle mehr spielt.
    static void AutoScaleToHeight(GameObject go, float targetHeight)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        if (b.size.y <= 0.0001f) return;
        float scale = targetHeight / b.size.y;
        go.transform.localScale *= scale;
    }

    static Transform FindDeepChild(Transform parent, string nameContains)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(nameContains.ToLower())) return child;
            var found = FindDeepChild(child, nameContains);
            if (found != null) return found;
        }
        return null;
    }

    // Nimmt die längste Achse der lokalen Mesh-Bounding-Box als Lauf-Richtung an und liefert
    // deren äußersten Punkt als ungefähre Mündung - robust, ohne die genaue Export-Achse zu kennen.
    static Vector3 FindMuzzleTip(Transform part)
    {
        var mf = part.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return part.position;
        var b = mf.sharedMesh.bounds;
        Vector3 e = b.extents;
        Vector3 tipLocal = (e.x >= e.y && e.x >= e.z) ? new Vector3(b.center.x + e.x, b.center.y, b.center.z)
                          : (e.y >= e.x && e.y >= e.z) ? new Vector3(b.center.x, b.center.y + e.y, b.center.z)
                          : new Vector3(b.center.x, b.center.y, b.center.z + e.z);
        return mf.transform.TransformPoint(tipLocal);
    }

    IEnumerator SpawnLoop()
    {
        int waveNumber = 0;
        while (true)
        {
            waveNumber++;
            int count = 5 + waveNumber * 2;
            float hp = 20f + waveNumber * 12f;
            float speed = 1.1f + Mathf.Min(waveNumber * 0.04f, 0.6f);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(hp, speed, 6 + waveNumber / 2);
                yield return new WaitForSeconds(0.5f);
            }
            // warten bis Welle leergeräumt ist, bevor die nächste startet
            while (enemies.Count > 0) yield return null;
            yield return new WaitForSeconds(1.5f);
        }
    }

    void SpawnEnemy(float hp, float speed, int reward)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Roboter";
        root.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
        root.transform.position = waypoints[0] + Vector3.up * 0.2f;
        root.GetComponent<Renderer>().material = MakeMaterial(new Color(0.37f, 0.32f, 0.28f), 0.5f, 0.4f);
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.transform.parent = root.transform;
        eye.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        eye.transform.localPosition = new Vector3(0f, 0.3f, 0.55f);
        var eyeMat = MakeMaterial(new Color(0.9f, 0.1f, 0.1f));
        eyeMat.EnableKeyword("_EMISSION");
        eyeMat.SetColor("_EmissionColor", new Color(1.5f, 0.15f, 0.1f));
        eye.GetComponent<Renderer>().material = eyeMat;

        var ec = root.AddComponent<EnemyController>();
        ec.Init(this, waypoints, hp, speed, reward);
        enemies.Add(ec);
    }

    public List<EnemyController> Enemies => enemies;

    public void OnEnemyDied(EnemyController e, int reward)
    {
        enemies.Remove(e);
        gold += reward;
    }

    public void OnEnemyReachedEnd(EnemyController e)
    {
        enemies.Remove(e);
        lives -= 1;
    }

    void OnGUI()
    {
        GUI.color = new Color(0.91f, 0.84f, 0.72f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        GUI.Label(new Rect(12, 10, 400, 28), $"⚙ Zahnräder: {gold}   ❤ Fabrik-Kern: {lives}   Gegner aktiv: {enemies.Count}", style);
    }
}
