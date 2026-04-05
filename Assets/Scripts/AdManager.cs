using System;
using UnityEngine;
// ── 前提: Google Mobile Ads Unity Plugin (v9.x) を導入後、
//         Edit > Project Settings > Player > Scripting Define Symbols に
//         「ADMOB_ENABLED」を追加してください ──
#if ADMOB_ENABLED
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMob リワード広告管理（本番）
/// App ID     : ca-app-pub-8388601065600220~1548673627
/// スタミナ回復: ca-app-pub-8388601065600220/3217124301
/// リベンジ   : ca-app-pub-8388601065600220/5807958197
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // ===== 広告ユニット ID（本番）=====
    const string AD_STAMINA = "ca-app-pub-8388601065600220/3217124301";
    const string AD_REVENGE = "ca-app-pub-8388601065600220/5807958197";

#if ADMOB_ENABLED
    RewardedAd staminaAd;
    RewardedAd revengeAd;
#endif

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

#if ADMOB_ENABLED
        MobileAds.Initialize(_ =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                LoadStaminaAd();
                LoadRevengeAd();
            });
        });
#endif
    }

#if ADMOB_ENABLED
    // ─────────────────────────────────
    // ロード
    // ─────────────────────────────────

    // 非パーソナライズ広告リクエスト（ポリシー準拠: npa=1）
    static AdRequest NonPersonalizedRequest()
    {
        var req = new AdRequest();
        req.Extras.Add("npa", "1");
        return req;
    }

    void LoadStaminaAd()
    {
        RewardedAd.Load(AD_STAMINA, NonPersonalizedRequest(), (ad, err) =>
        {
            if (err != null)
            {
                Debug.LogWarning("[Ad] スタミナ広告ロード失敗: " + err);
                return;
            }
            staminaAd = ad;
            staminaAd.OnAdFullScreenContentClosed += () =>
            {
                staminaAd?.Destroy();
                staminaAd = null;
                LoadStaminaAd();
            };
        });
    }

    void LoadRevengeAd()
    {
        RewardedAd.Load(AD_REVENGE, NonPersonalizedRequest(), (ad, err) =>
        {
            if (err != null)
            {
                Debug.LogWarning("[Ad] リベンジ広告ロード失敗: " + err);
                return;
            }
            revengeAd = ad;
            revengeAd.OnAdFullScreenContentClosed += () =>
            {
                revengeAd?.Destroy();
                revengeAd = null;
                LoadRevengeAd();
            };
        });
    }
#endif

    // ─────────────────────────────────
    // 公開 API
    // ─────────────────────────────────

    /// <summary>
    /// スタミナ回復広告を表示。onComplete(adShown) を呼ぶ。
    /// adShown=true: 広告が実際に表示された / false: 広告なし（無料付与）
    /// </summary>
    public void ShowStaminaAd(Action<bool> onComplete)
    {
#if ADMOB_ENABLED
        if (staminaAd != null && staminaAd.CanShowAd())
        {
            staminaAd.Show(_ => onComplete?.Invoke(true));
        }
        else
        {
            Debug.Log("[Ad] スタミナ広告未準備 — フォールバック付与");
            LoadStaminaAd();
            onComplete?.Invoke(false);
        }
#else
        onComplete?.Invoke(false);
#endif
    }

    /// <summary>
    /// リベンジ広告を表示。onComplete(adShown) を呼ぶ。
    /// adShown=true: 広告が実際に表示された / false: 広告なし（無料付与）
    /// </summary>
    public void ShowRevengeAd(Action<bool> onComplete)
    {
#if ADMOB_ENABLED
        if (revengeAd != null && revengeAd.CanShowAd())
        {
            revengeAd.Show(_ => onComplete?.Invoke(true));
        }
        else
        {
            Debug.Log("[Ad] リベンジ広告未準備 — フォールバック付与");
            LoadRevengeAd();
            onComplete?.Invoke(false);
        }
#else
        onComplete?.Invoke(false);
#endif
    }
}

// ─────────────────────────────────────────────────────────
// UnityMainThreadDispatcher — AdMob コールバックをメインスレッドで実行
// ─────────────────────────────────────────────────────────
public class UnityMainThreadDispatcher : MonoBehaviour
{
    static readonly System.Collections.Generic.Queue<Action> queue =
        new System.Collections.Generic.Queue<Action>();
    static UnityMainThreadDispatcher inst;

    public static void Enqueue(Action action)
    {
        if (inst == null)
        {
            var go = new GameObject("MainThreadDispatcher");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<UnityMainThreadDispatcher>();
        }
        lock (queue) { queue.Enqueue(action); }
    }

    void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
                queue.Dequeue()?.Invoke();
        }
    }
}
