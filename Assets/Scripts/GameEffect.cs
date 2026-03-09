using System.Collections;
using UnityEngine;

/// <summary>
/// 着地エフェクト（パーティクル・スコアポップアップ・カメラシェイク）を一元管理。
/// シーンに1つ置くだけ。AutoSetupTofuGame から自動追加される。
/// </summary>
public class GameEffect : MonoBehaviour
{
    public static GameEffect Instance;

    ParticleSystem landPS;
    Camera         mainCam;
    Vector3        camLocalOrigin;
    float          shakeTimer;
    float          shakeMagnitude;

    // ===== Lifecycle =====

    void Awake()
    {
        Instance = this;
        BuildParticleSystem();
    }

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null)
            camLocalOrigin = mainCam.transform.localPosition;
    }

    void Update()
    {
        if (mainCam == null) return;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float mag = shakeMagnitude * Mathf.Clamp01(shakeTimer / 0.18f);
            mainCam.transform.localPosition = camLocalOrigin
                + (Vector3)(Random.insideUnitCircle * mag);
        }
        else
        {
            mainCam.transform.localPosition = Vector3.MoveTowards(
                mainCam.transform.localPosition, camLocalOrigin, Time.deltaTime * 12f);
        }
    }

    // ===== Public API =====

    /// <summary>豆腐が着地したときに呼ぶ</summary>
    public void PlayLandEffect(Vector3 worldPos)
    {
        if (landPS != null)
        {
            landPS.transform.position = worldPos;
            landPS.Play();
        }

        shakeTimer     = 0.18f;
        shakeMagnitude = 0.07f;

        SpawnScorePopup(worldPos);
    }

    // ===== Score Popup =====

    void SpawnScorePopup(Vector3 worldPos)
    {
        var go = new GameObject("ScorePopup");
        go.transform.position = worldPos + Vector3.up * 1.0f;

        // カメラを向かせる
        if (mainCam != null)
        {
            go.transform.LookAt(mainCam.transform.position);
            go.transform.Rotate(0f, 180f, 0f);
        }

        var tm = go.AddComponent<TextMesh>();
        tm.text          = "+1";
        tm.fontSize      = 80;
        tm.characterSize = 0.010f;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.fontStyle     = FontStyle.Bold;
        tm.color         = new Color(0.95f, 0.78f, 0.10f, 1f); // ゴールド

        go.AddComponent<ScorePopupBehaviour>();
    }

    // ===== Particle System =====

    void BuildParticleSystem()
    {
        var go = new GameObject("LandParticles");
        go.transform.SetParent(transform);
        landPS = go.AddComponent<ParticleSystem>();

        var main = landPS.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.45f, 0.90f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f,  3.8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1.00f, 0.95f, 0.80f, 1f),
            new Color(0.95f, 0.82f, 0.55f, 1f));
        main.gravityModifier    = 1.4f;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;
        main.maxParticles       = 50;
        main.stopAction         = ParticleSystemStopAction.None;

        var emission = landPS.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        var shape = landPS.shape;
        shape.shapeType       = ParticleSystemShapeType.Circle;
        shape.radius          = 0.55f;
        shape.radiusThickness = 0.6f;

        // 透明度フェードアウト
        var col = landPS.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // サイズ縮小
        var size = landPS.sizeOverLifetime;
        size.enabled = true;
        var curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // マテリアル
        var ren = go.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        Shader pShader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Standard Unlit") ??
            Shader.Find("Sprites/Default");
        if (pShader != null)
        {
            ren.material       = new Material(pShader);
            ren.material.color = Color.white;
        }
    }
}

// ===== スコアポップアップ挙動（MonoBehaviour） =====

public class ScorePopupBehaviour : MonoBehaviour
{
    const float Life = 1.1f;
    float    t;
    TextMesh tm;
    Vector3  startPos;

    void Start()
    {
        tm       = GetComponent<TextMesh>();
        startPos = transform.position;
    }

    void Update()
    {
        t += Time.deltaTime;
        float ratio = t / Life;

        // イーズアウトで上昇
        transform.position = startPos
            + Vector3.up * Mathf.Sin(ratio * Mathf.PI * 0.5f) * 1.4f;

        // 後半40%でフェードアウト
        float alpha = ratio < 0.6f ? 1f
            : Mathf.Lerp(1f, 0f, (ratio - 0.6f) / 0.4f);

        if (tm != null)
            tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, alpha);

        if (t >= Life) Destroy(gameObject);
    }
}
