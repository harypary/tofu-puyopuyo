using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Spawner : MonoBehaviour
{
    public GameObject tofuPrefab;
    public GameObject shadowObject;
    // moveSpeed / moveRange は GameConfig から直接読む（serialized 値に依存しない）

    private GameObject currentTofu;
    private GameObject droppedTofu;   // 落下中の豆腐を追跡（影用）
    private bool  isMoving    = false;
    private float dropCooldown = 0f;   // 起動直後の誤タップ防止
    private float randomOffsetX;
    private float randomOffsetZ;

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
        SpawnNewTofu();
    }

    void OnDisable()
    {
        if (shadowObject != null) shadowObject.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance?.State != GameState.Playing) return;

        if (isMoving && currentTofu != null)
        {
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
        if (tofuPrefab == null) return;

        droppedTofu = null;
        if (shadowObject != null) shadowObject.SetActive(false);

        currentTofu = Instantiate(tofuPrefab, transform.position, Quaternion.identity);
        if (!currentTofu.activeSelf) currentTofu.SetActive(true); // テンプレートが非アクティブでも確実に有効化
        currentTofu.transform.SetParent(transform);
        isMoving = true;

        Collider col = currentTofu.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        randomOffsetX = Random.Range(0f, 100f);
        randomOffsetZ = Random.Range(0f, 100f);
    }

    private void DropTofu()
    {
        if (currentTofu == null) return;

        droppedTofu = currentTofu; // 落下中も影を追跡するために保持
        isMoving = false;
        currentTofu.transform.SetParent(null);
        currentTofu.name = "DroppedTofu";

        Tofu tofu = currentTofu.GetComponent<Tofu>();
        if (tofu != null)
            tofu.Drop();
        else
        {
            var rb = currentTofu.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
        }
    }

    public void MoveUp(float nextY)
    {
        transform.position = new Vector3(transform.position.x, nextY, transform.position.z);
    }
}
