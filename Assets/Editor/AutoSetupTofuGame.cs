using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class AutoSetupTofuGame : EditorWindow
{
    [MenuItem("TofuGame/Setup Scene")]
    public static void Setup()
    {
        // ===== 1. 旧オブジェクトを一掃 =====
        string[] killWords = {
            "ground", "spawner", "manager", "tofu", "shadow", "plate",
            "palm", "finger", "thumb", "sky", "horizon", "background",
            "light", "fill", "effect", "postprocess", "volume", "initial", "rim"
        };
        foreach (var obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            string n = obj.name.ToLower();
            foreach (var w in killWords)
                if (n.Contains(w)) { DestroyImmediate(obj); break; }
        }

        // ===== 2. カメラ（LookAt で正確に向ける）=====
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera");
            cgo.tag = "MainCamera";
            cam = cgo.AddComponent<Camera>();
            cgo.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(0f, 8f, -17f); // 引いてフィールド全体を映す
        cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        cam.clearFlags     = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.60f, 0.82f, 0.95f, 1f);
        cam.fieldOfView    = 60f;
        cam.farClipPlane   = 100f;

        // ===== 3. ライティング =====
        SetupLighting();

        // ===== 4. 背景 =====
        CreateBackground();

        // ===== 5. 地面 =====
        CreateGround();

        // ===== 6. 豆腐プレハブ =====
        GameObject tofuPrefab = CreateTofuPrefab();

        // ===== 7. 影 =====
        GameObject shadow = CreateShadow();

        // ===== 8. GameManager =====
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        // ===== 9. Spawner（非アクティブ開始）=====
        var spawnerGo = new GameObject("Spawner");
        spawnerGo.transform.position = new Vector3(0f, 6f, 0f);
        var spawner = spawnerGo.AddComponent<Spawner>();
        spawnerGo.SetActive(false);

        // ===== 10. UI / スタミナ =====
        var uiGo = new GameObject("UIManager");
        uiGo.AddComponent<StaminaManager>();
        uiGo.AddComponent<UIManager>();

        // ===== 11. エフェクト =====
        new GameObject("GameEffect").AddComponent<GameEffect>();

        // ===== 12. 広告マネージャー =====
        new GameObject("AdManager").AddComponent<AdManager>();

        // ===== 13. リンク =====
        gm.spawner           = spawner;
        spawner.tofuPrefab   = tofuPrefab;
        spawner.shadowObject = shadow;

        AddTag("Ground");
        AddTag("Tofu");

        Debug.Log("★ おとうふぷよぷよゲーム セットアップ完了！★");
    }

    // ─────────────────────────────────
    // ライティング
    // ─────────────────────────────────

    static void SetupLighting()
    {
        Light main = GameObject.FindFirstObjectByType<Light>();
        if (main != null)
        {
            main.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            main.color     = new Color(1.00f, 0.96f, 0.88f, 1f);
            main.intensity = 1.2f;
            main.shadows   = LightShadows.None;
        }

        var fill = new GameObject("FillLight").AddComponent<Light>();
        fill.type              = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
        fill.color             = new Color(0.72f, 0.86f, 1.00f, 1f);
        fill.intensity         = 0.40f;
        fill.shadows           = LightShadows.None;

        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.80f, 0.82f, 0.85f, 1f); // 明るくして白が白く見えるように
        RenderSettings.fog          = false;
    }

    // ─────────────────────────────────
    // 背景（草原 + 空）
    // ─────────────────────────────────

    static void CreateBackground()
    {
        // 空背景のみ（草原プレーンは廃止して統合）
        // 下方向も十分カバーするよう大きく・低めに配置
        var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sky.name = "SkyBackground";
        DestroyImmediate(sky.GetComponent<MeshCollider>());
        sky.transform.position   = new Vector3(0f, 5f, 22f);
        sky.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
        sky.transform.localScale = new Vector3(80f, 60f, 1f);
        sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        ApplyMat(sky, new Color(0.55f, 0.80f, 0.96f, 1f), "SkyMat", 0f);
    }

    // ─────────────────────────────────
    // 地面（初版と同じ緑キューブ）
    // ─────────────────────────────────

    static void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.tag  = "Ground";
        ground.transform.position   = new Vector3(0f, -1f, 0f);
        ground.transform.localScale = new Vector3(7f, 1f, 7f);
        ApplyMat(ground, new Color(0.3f, 0.4f, 0.3f, 1f), "GroundMat", 0.1f);
    }

    // ─────────────────────────────────
    // 豆腐プレハブ
    // ─────────────────────────────────

    static GameObject CreateTofuPrefab()
    {
        // 古いマテリアルを強制削除して再生成（色キャッシュ問題を防ぐ）
        AssetDatabase.DeleteAsset("Assets/Resources/TofuMat.mat");

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TofuPrefab";
        go.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);
        go.tag = "Tofu";

        var mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
        ApplyMat(go, new Color(1f, 1f, 1f, 1f), "TofuMat", 0.35f);

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic            = true;
        rb.useGravity             = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (go.GetComponent<Tofu>()) DestroyImmediate(go.GetComponent<Tofu>());
        go.AddComponent<Tofu>();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Tofu.prefab");
        DestroyImmediate(go);
        return prefab;
    }

    // ─────────────────────────────────
    // 影
    // ─────────────────────────────────

    static GameObject CreateShadow()
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
        s.name = "TofuShadow";
        DestroyImmediate(s.GetComponent<MeshCollider>());
        s.transform.rotation   = Quaternion.Euler(-90f, 0f, 0f);
        s.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
        s.transform.position   = new Vector3(0f, -0.45f, 0f);

        // 実行時に Spawner.ApplyShadowMaterial() が上書きするので
        // ここでは最低限のマテリアルだけ設定（プレイ開始前のプレビュー用）
        Shader shadowSh = Shader.Find("Sprites/Default")
                       ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                       ?? Shader.Find("Particles/Standard Unlit");
        if (shadowSh != null)
        {
            var shadowMat = new Material(shadowSh);
            shadowMat.color = new Color(0f, 0f, 0f, 0.45f);
            s.GetComponent<MeshRenderer>().sharedMaterial = shadowMat;
        }

        s.SetActive(false);
        return s;
    }

    // ─────────────────────────────────
    // マテリアル共通
    // ─────────────────────────────────

    static void ApplyMat(GameObject obj, Color color, string matName, float smoothness)
    {
        string   path = "Assets/Resources/" + matName + ".mat";
        Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            // 一時プリミティブからシェーダーを取得（Shader.Find より確実）
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Shader sh = tmp.GetComponent<MeshRenderer>().sharedMaterial.shader;
            DestroyImmediate(tmp);

            mat = new Material(sh);

            if (color.a < 1f)
            {
                if (mat.HasProperty("_Surface"))         // URP 透明
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend",   0f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = 3000;
                }
                else if (sh.name.Contains("Standard"))  // Built-in 透明
                {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);

        // プロパティ変更をディスクに保存（これがないと色が反映されない）
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        var ren = obj.GetComponent<Renderer>();
        if (ren != null) ren.sharedMaterial = mat;
    }

    // ─────────────────────────────────
    // タグ追加
    // ─────────────────────────────────

    static void AddTag(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var mgr   = new SerializedObject(assets[0]);
        var prop  = mgr.FindProperty("tags");
        for (int i = 0; i < prop.arraySize; i++)
            if (prop.GetArrayElementAtIndex(i).stringValue == tag) return;
        prop.InsertArrayElementAtIndex(0);
        prop.GetArrayElementAtIndex(0).stringValue = tag;
        mgr.ApplyModifiedProperties();
    }
}
