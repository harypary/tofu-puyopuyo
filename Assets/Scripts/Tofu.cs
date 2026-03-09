using System.Collections;
using UnityEngine;

public class Tofu : MonoBehaviour
{
    private Rigidbody rb;
    private bool isPlaced = false;
    public bool IsPlaced => isPlaced;

    private Vector3 originalScale;
    private float wobbleAmount = 0f;
    private float constantWobble = 0f;

    // 全インスタンスで共有する白マテリアル（実行時に生成）
    static Material s_mat;
    // 跳ねを抑える物理マテリアル（全インスタンスで共有）
    static PhysicsMaterial s_pm;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // 実行時にシェーダーを取得して確実に白く塗る（エディタのシェーダー問題を回避）
        if (s_mat == null)
        {
            // Simple Lit は灰色になりにくい軽量シェーダー
            Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
            if (sh != null)
            {
                s_mat = new Material(sh) { name = "TofuWhite" };
                if (s_mat.HasProperty("_BaseColor"))    s_mat.SetColor("_BaseColor", Color.white);
                if (s_mat.HasProperty("_Color"))        s_mat.color = Color.white;
                if (s_mat.HasProperty("_Smoothness"))   s_mat.SetFloat("_Smoothness", 0.1f);
                if (s_mat.HasProperty("_Metallic"))     s_mat.SetFloat("_Metallic",   0f);
                // エミッション追加：ライティングが暗くても白く見える
                if (s_mat.HasProperty("_EmissionColor"))
                {
                    s_mat.SetColor("_EmissionColor", new Color(0.18f, 0.18f, 0.18f));
                    s_mat.EnableKeyword("_EMISSION");
                }
            }
        }
        var mr = GetComponent<MeshRenderer>();
        if (mr != null && s_mat != null) mr.sharedMaterial = s_mat;

        // 物理マテリアル：跳ねをゼロに
        if (s_pm == null)
        {
            s_pm = new PhysicsMaterial("TofuPhysics")
            {
                bounciness       = 0f,
                dynamicFriction  = 0.6f,
                staticFriction   = 0.6f,
                bounceCombine    = PhysicsMaterialCombine.Minimum,
                frictionCombine  = PhysicsMaterialCombine.Average
            };
        }
        var col = GetComponent<Collider>();
        if (col != null) col.sharedMaterial = s_pm;
    }

    void Update()
    {
        float currentWobble = 0f;

        if (wobbleAmount > 0.001f)
        {
            currentWobble = Mathf.Sin(Time.time * 15f) * wobbleAmount;
            wobbleAmount  = Mathf.Lerp(wobbleAmount, 0, Time.deltaTime * GameConfig.WobbleDecayRate);
        }
        else
        {
            wobbleAmount = 0;
            if (!isPlaced && rb != null && rb.isKinematic)
                currentWobble = constantWobble;
        }

        transform.localScale = originalScale + new Vector3(currentWobble, -currentWobble, currentWobble);

        // 画面外（下に落ちた）= ゲームオーバー（Playingのときのみ）
        if (transform.position.y < -5f && GameManager.Instance?.State == GameState.Playing)
        {
            GameManager.Instance.GameOver();
        }
    }

    public void SetConstantWobble(float amount)
    {
        constantWobble = isPlaced ? 0f : amount;
    }

    public void Drop()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.isKinematic = false;
        rb.useGravity  = true;
        wobbleAmount   = GameConfig.WobbleDrop;

        if (GameManager.Instance != null && !GameManager.Instance.isEasyMode)
            rb.AddTorque(Random.onUnitSphere * 3f, ForceMode.Impulse);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    void OnCollisionEnter(Collision col)
    {
        if (!isPlaced &&
            (col.gameObject.CompareTag("Ground") || col.gameObject.CompareTag("Tofu")))
        {
            wobbleAmount = GameConfig.WobbleLand;
            isPlaced     = true;

            // 着地エフェクト
            GameEffect.Instance?.PlayLandEffect(transform.position);

            if (GameManager.Instance?.State == GameState.Playing)
                StartCoroutine(NotifyWhenSettled());
        }
    }

    // 物理的に静止してから次の豆腐をスポーンする
    IEnumerator NotifyWhenSettled()
    {
        // 着地直後の跳ね返りが終わるまで最低 0.4 秒待つ
        yield return new WaitForSeconds(0.4f);

        if (rb != null)
        {
            float timeout = 3f;
            while (timeout > 0f && rb.linearVelocity.sqrMagnitude > 0.04f)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }
        }

        if (GameManager.Instance?.State == GameState.Playing)
            GameManager.Instance.OnTofuPlaced(this);
    }
}
