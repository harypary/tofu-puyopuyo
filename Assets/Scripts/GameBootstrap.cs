using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ランタイム自動セットアップ。
/// シーンに何も置かなくてもゲームが動くようにする。
/// TofuGame > Setup Scene の代替（ビルド環境向け）。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // シーンリロードをまたいでテンプレートを使い回す（DontDestroyOnLoad リーク防止）
    static GameObject s_tofuTemplate;

    // ─────────────────────────────────────────────────────────
    // 起動エントリポイント（シーンロード後に必ず呼ばれる）
    // ─────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // 既にセットアップ済みならスキップ
        if (GameManager.Instance != null) return;

        Debug.Log("[Bootstrap] ゲームシーンを自動セットアップ中...");

        SetupCamera();
        SetupLighting();
        CreateBackground();
        CreateGround();

        var tofuTemplate = CreateTofuTemplate();
        var shadow       = CreateShadow();

        // GameManager
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        // Spawner（非アクティブ開始）
        var spawnerGo = new GameObject("Spawner");
        spawnerGo.transform.position = new Vector3(0f, GameConfig.SpawnHeight, 0f);
        var spawner = spawnerGo.AddComponent<Spawner>();
        spawnerGo.SetActive(false);

        // UIManager + StaminaManager
        var uiGo = new GameObject("UIManager");
        uiGo.AddComponent<StaminaManager>();
        uiGo.AddComponent<UIManager>();

        // エフェクト
        new GameObject("GameEffect").AddComponent<GameEffect>();

        // 広告
        new GameObject("AdManager").AddComponent<AdManager>();

        // リンク
        gm.spawner          = spawner;
        spawner.tofuPrefab   = tofuTemplate;
        spawner.shadowObject = shadow;

        Debug.Log("[Bootstrap] セットアップ完了！");
    }

    // ─────────────────────────────────────────────────────────
    // カメラ
    // ─────────────────────────────────────────────────────────
    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.transform.position  = new Vector3(0f, 8f, -17f);
        cam.transform.rotation  = Quaternion.Euler(15f, 0f, 0f);
        cam.clearFlags          = CameraClearFlags.SolidColor;
        cam.backgroundColor     = new Color(0.60f, 0.82f, 0.95f, 1f);
        cam.fieldOfView         = 60f;
        cam.farClipPlane        = 100f;
    }

    // ─────────────────────────────────────────────────────────
    // ライティング
    // ─────────────────────────────────────────────────────────
    static void SetupLighting()
    {
        // 既存のDirectionalLightを使用
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light main = null;
        foreach (var l in lights)
            if (l.type == LightType.Directional && main == null) main = l;

        if (main != null)
        {
            main.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            main.color     = new Color(1.00f, 0.96f, 0.88f, 1f);
            main.intensity = 1.2f;
            main.shadows   = LightShadows.None;
        }

        // フィルライトが無ければ追加
        bool hasFill = false;
        foreach (var l in lights)
            if (l.gameObject.name == "FillLight") { hasFill = true; break; }

        if (!hasFill)
        {
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.type              = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
            fill.color             = new Color(0.72f, 0.86f, 1.00f, 1f);
            fill.intensity         = 0.40f;
            fill.shadows           = LightShadows.None;
        }

        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.80f, 0.82f, 0.85f, 1f);
        RenderSettings.fog          = false;
    }

    // ─────────────────────────────────────────────────────────
    // 背景
    // ─────────────────────────────────────────────────────────
    static void CreateBackground()
    {
        if (GameObject.Find("SkyBackground") != null) return;

        var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sky.name = "SkyBackground";
        Object.Destroy(sky.GetComponent<MeshCollider>());
        sky.transform.position   = new Vector3(0f, 5f, 22f);
        sky.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
        sky.transform.localScale = new Vector3(80f, 60f, 1f);
        sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        ApplyColor(sky, new Color(0.55f, 0.80f, 0.96f, 1f));
    }

    // ─────────────────────────────────────────────────────────
    // 地面
    // ─────────────────────────────────────────────────────────
    static void CreateGround()
    {
        if (GameObject.FindWithTag("Ground") != null) return;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.tag  = "Ground";
        ground.transform.position   = new Vector3(0f, -1f, 0f);
        ground.transform.localScale = new Vector3(7f, 1f, 7f);
        ApplyColor(ground, new Color(0.3f, 0.4f, 0.3f, 1f));
    }

    // ─────────────────────────────────────────────────────────
    // 豆腐テンプレート（非アクティブで保持、Spawnerが Instantiate する）
    // シーンリロード後も同一インスタンスを再利用してメモリリークを防ぐ
    // ─────────────────────────────────────────────────────────
    static GameObject CreateTofuTemplate()
    {
        // 既に作成済みなら再利用（RetryGame のシーンリロードで重複生成しない）
        if (s_tofuTemplate != null) return s_tofuTemplate;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TofuPrefab";
        go.tag  = "Tofu";
        go.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);

        var mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
        ApplyColor(go, Color.white);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic            = true;
        rb.useGravity             = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        go.AddComponent<Tofu>();

        // Spawner が Instantiate する前に非アクティブにしてテンプレートとして保持
        go.SetActive(false);
        Object.DontDestroyOnLoad(go);
        s_tofuTemplate = go;
        return go;
    }

    // ─────────────────────────────────────────────────────────
    // 影
    // ─────────────────────────────────────────────────────────
    static GameObject CreateShadow()
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
        s.name = "TofuShadow";
        Object.Destroy(s.GetComponent<MeshCollider>());
        s.transform.rotation   = Quaternion.Euler(-90f, 0f, 0f);
        s.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        s.transform.position   = new Vector3(0f, -0.45f, 0f);

        // 半透明の黒マテリアル
        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Particles/Standard Unlit");
        if (sh != null)
        {
            var mat = new Material(sh);
            mat.color = new Color(0f, 0f, 0f, 0.45f);
            s.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        s.SetActive(false);
        return s;
    }

    // ─────────────────────────────────────────────────────────
    // 共通マテリアル適用（ランタイム版）
    // ─────────────────────────────────────────────────────────
    static void ApplyColor(GameObject obj, Color color)
    {
        var mr = obj.GetComponent<MeshRenderer>();
        if (mr == null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                 ?? Shader.Find("Standard");
        if (sh == null) return;

        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        mr.sharedMaterial = mat;
    }
}
