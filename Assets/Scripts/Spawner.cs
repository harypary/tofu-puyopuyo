using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Spawner : MonoBehaviour
{
    // tofuPrefab は不使用（SpawnNewTofu が毎回直接生成するため）
    [HideInInspector] public GameObject tofuPrefab;
    public GameObject shadowObject;
    // moveSpeed / moveRange は GameConfig から直接読む（serialized 値に依存しない）

    private GameObject currentTofu;
    private GameObject droppedTofu;   // 落下中の豆腐を追跡（影用）
    private bool  isMoving    = false;
    private float dropCooldown = 0f;   // 起動直後の誤タップ防止
    private float randomOffsetX;
    private float randomOffsetZ;

    // 安全網: Playing 中に豆腐が存在しない時間が続いた場合に強制スポーンするタイマー
    private float noTofuTimer = 0f;
    private const float NoTofuTimeout = 4f; // NotifyWhenSettled の最大待機(3.4s)より長く設定

    /// <summary>
    /// 浮遊中 or 落下・着地待ち中の豆腐が存在するか。
    /// currentTofu（浮遊中）だけでなく droppedTofu（落下〜NotifyWhenSettled 完了まで）も含める。
    /// ドロップ後 〜 着地完了の間も HasTofu=true になるため OnApplicationFocus の誤スポーンを防ぐ。
    /// </summary>
    public bool HasTofu         => currentTofu != null || droppedTofu != null;

    // デバッグオーバーレイ用
    public bool  IsMovingDebug      => isMoving;
    public bool  HasCurrentTofu     => currentTofu != null;
    public bool  HasDroppedTofu     => droppedTofu != null;
    public float NoTofuTimerDebug   => noTofuTimer;

    void Start()
    {
        ApplyShadowMaterial();
    }

    void ApplyShadowMaterial()
    {
        if (shadowObject == null) return;
        var mr = shadowObject.GetComponent<MeshRenderer>();
        if (mr == null) return;

        // ソフト円形グラデーションのテクスチャを生成
        int sz = 128;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var pixels = new Color[sz * sz];
        float center = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float t    = Mathf.Clamp01(1f - dist / center);
                float a    = t * t * 0.55f; // ソフトな円形フェード
                pixels[y * sz + x] = new Color(0f, 0f, 0f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        // Sprites/Default は Unity 組み込みで α が確実に動く
        Shader sh = Shader.Find("Sprites/Default")
                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Particles/Standard Unlit");
        if (sh == null) return;

        var mat = new Material(sh);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap"))   mat.SetTexture("_BaseMap",   tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",   Color.white);
        if (mat.HasProperty("_Color"))     mat.color = Color.white;
        // 両面描画（向き依存をなくす）
        if (mat.HasProperty("_Cull"))      mat.SetFloat("_Cull", 0f);

        mr.sharedMaterial = mat;
    }

    void OnEnable()
    {
        dropCooldown = GameConfig.DropCooldown;
        // SpawnNewTofu は GameManager から明示的に呼ばれる（OnEnable 依存を排除）
    }

    void OnDisable()
    {
        if (shadowObject != null) shadowObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance?.State != GameState.Playing)
        {
            noTofuTimer = 0f; // Playing 以外ではタイマーをリセット
            return;
        }

        // 安全網①: isMoving=true なのに currentTofu が消えていたら緊急再スポーン
        // iOS など予期しないタイミングで tofu が失われた場合に自動復旧する
        if (isMoving && currentTofu == null)
        {
            Debug.LogWarning("[Spawner] currentTofu が null です。緊急再スポーン。");
            SpawnNewTofu();
            return;
        }

        if (isMoving && currentTofu != null)
        {
            noTofuTimer = 0f; // 浮遊豆腐あり → タイマーリセット

            float xPos = (Mathf.PerlinNoise(Time.time * GameConfig.MoveSpeed + randomOffsetX, 0) * 2f - 1f) * GameConfig.MoveRange;
            float zPos = (Mathf.PerlinNoise(0, Time.time * GameConfig.MoveSpeed + randomOffsetZ) * 2f - 1f) * GameConfig.MoveRange;
            float curY = transform.position.y;
            currentTofu.transform.position = new Vector3(xPos, curY, zPos);

            UpdateShadow(new Vector3(xPos, curY, zPos));

            Tofu tofu = currentTofu.GetComponent<Tofu>();
            if (tofu != null)
                tofu.SetConstantWobble(Mathf.Sin(Time.time * GameConfig.WobbleIdleFreq) * GameConfig.WobbleIdleAmp);

            if (dropCooldown > 0f) { dropCooldown -= Time.deltaTime; return; }
            if (IsInputPressed()) DropTofu();
        }
        else if (!isMoving && droppedTofu != null)
        {
            noTofuTimer = 0f; // 落下中の豆腐を追跡中 → タイマーリセット

            // 着地済みなら追跡終了
            Tofu t = droppedTofu.GetComponent<Tofu>();
            if (t != null && t.IsPlaced)
            {
                droppedTofu = null;
                if (shadowObject != null) shadowObject.SetActive(false);
            }
            else
            {
                UpdateShadow(droppedTofu.transform.position);
            }
        }
        else
        {
            // !isMoving && droppedTofu == null && currentTofu == null
            // = NotifyWhenSettled() の完了を待っている状態
            // 安全網②: この状態が NoTofuTimeout 秒以上続いたら強制スポーン
            // （NotifyWhenSettled が iOS で完了しなかったケースをカバー）
            noTofuTimer += Time.deltaTime;
            if (noTofuTimer >= NoTofuTimeout)
            {
                Debug.LogWarning("[Spawner] 豆腐なし " + NoTofuTimeout + "秒超過 → 強制スポーン");
                SpawnNewTofu();
            }
        }
    }

    // 豆腐の真下にレイキャストして影を更新（豆腐自身には当たらないよう少し下から飛ばす）
    void UpdateShadow(Vector3 tofuPos)
    {
        if (shadowObject == null) return;
        RaycastHit hit;
        // 豆腐の高さ半分(0.4)+余裕で0.6f 下からキャスト
        Vector3 rayStart = new Vector3(tofuPos.x, tofuPos.y - 0.6f, tofuPos.z);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 30f))
        {
            shadowObject.SetActive(true);
            shadowObject.transform.position = hit.point + Vector3.up * 0.05f;
        }
        else
            shadowObject.SetActive(false);
    }

    private bool IsInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame)                  return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)                return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
#else
        if (Input.GetMouseButtonDown(0))                                               return true;
        if (Input.GetKeyDown(KeyCode.Space))                                           return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)      return true;
#endif
        return false;
    }

    public void SpawnNewTofu()
    {
        noTofuTimer = 0f; // 生成時は必ずタイマーをリセット

        // 浮遊中の豆腐が残っていれば破棄（ゲームリセット時など）
        // ※ Unity の == は破棄済みオブジェクトを null 扱いするため二重破棄は起きない
        if (currentTofu != null)
            Destroy(currentTofu);
        currentTofu = null; // 破棄済み参照も必ずクリア

        droppedTofu = null;
        if (shadowObject != null) shadowObject.SetActive(false);

        // prefab に依存せず毎回直接生成（DDOL テンプレート問題を根本排除）
        currentTofu = CreateNewTofu();
        // ★ spawner の子にしない ★
        // 子にすると SetActive(false) で非アクティブになり
        // FindGameObjectsWithTag が見つけられなくなるため DestroyActiveTofus が機能しない。
        // Update() で毎フレーム位置を上書きするので親子関係は不要。
        isMoving = true;

        var col = currentTofu.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        randomOffsetX = Random.Range(0f, 100f);
        randomOffsetZ = Random.Range(0f, 100f);
    }

    /// <summary>豆腐 GameObject を毎回スクラッチで生成する。DontDestroyOnLoad テンプレート不要。</summary>
    GameObject CreateNewTofu()
    {
        // ── ルート: 物理専用 (BoxCollider は絶対スケール変形しない) ──
        var go = new GameObject("Tofu");
        go.tag = "Tofu";
        go.transform.position   = transform.position;
        go.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);

        go.AddComponent<BoxCollider>();

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic            = true;
        rb.useGravity             = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // ── 子: 見た目専用 (wobble スケールをここに適用、物理に影響しない) ──
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        // Visual の BoxCollider を即時無効化してから削除（1 フレームの干渉を防止）
        var vbc = visual.GetComponent<BoxCollider>();
        if (vbc != null) { vbc.enabled = false; Destroy(vbc); }
        var mr = visual.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one;

        // Tofu コンポーネント最後に追加（Awake 内で Visual を検出するため）
        go.AddComponent<Tofu>();
        return go;
    }

    private void DropTofu()
    {
        if (currentTofu == null) return;

        droppedTofu = currentTofu; // 落下中も影を追跡するために保持
        currentTofu = null;        // 参照をクリア（SpawnNewTofu が誤って破棄しないよう）
        isMoving = false;
        droppedTofu.name = "DroppedTofu";
        // ★ SetParent(null) 不要 — spawner の子にしていないため

        Tofu tofu = droppedTofu.GetComponent<Tofu>();
        if (tofu != null)
            tofu.Drop();
        else
        {
            var rb = droppedTofu.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
        }
    }

    public void MoveUp(float nextY)
    {
        transform.position = new Vector3(transform.position.x, nextY, transform.position.z);
    }
}
