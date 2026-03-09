using UnityEngine;
using System;

/// <summary>
/// スタミナシステム。最大5個、1個につき10分で自動回復。
/// PlayerPrefs でアプリを閉じても経過時間を保持する。
/// </summary>
public class StaminaManager : MonoBehaviour
{
    public static StaminaManager Instance;

    public const int MaxStamina        = 5;
    public const float RecoveryMinutes = 10f;   // 1個回復にかかる分数

    private int currentStamina;
    private DateTime nextRecoveryTime;  // 次に1個回復する時刻

    public int Current => currentStamina;
    public bool IsFull  => currentStamina >= MaxStamina;

    // 次の1個回復まで残り何秒か（スタミナ満タンなら0）
    public float SecondsUntilNextRecovery
    {
        get
        {
            if (IsFull) return 0f;
            double secs = (nextRecoveryTime - DateTime.UtcNow).TotalSeconds;
            return Mathf.Max(0f, (float)secs);
        }
    }

    // ===== Lifecycle =====

    void Awake()
    {
        Instance = this;
        Load();
    }

    void Update()
    {
        // 毎フレーム回復チェック（満タンでなければ）
        if (!IsFull && DateTime.UtcNow >= nextRecoveryTime)
        {
            RecoverTick();
        }
    }

    // ===== Public API =====

    /// <summary>スタミナを1消費。消費できれば true を返す。</summary>
    public bool UseStamina()
    {
        if (currentStamina <= 0) return false;

        bool wasFull = IsFull;
        currentStamina--;

        // 満タンから減った瞬間に回復タイマー開始
        if (wasFull)
            nextRecoveryTime = DateTime.UtcNow.AddMinutes(RecoveryMinutes);

        Save();
        UIManager.Instance?.UpdateStamina();
        return true;
    }

    /// <summary>広告視聴後などにフル回復。</summary>
    public void RecoverFull()
    {
        currentStamina = MaxStamina;
        nextRecoveryTime = DateTime.MaxValue;
        Save();
        UIManager.Instance?.UpdateStamina();
    }

    // ===== Internal =====

    void RecoverTick()
    {
        // 経過時間から一度に複数回復できる場合も考慮
        while (!IsFull && DateTime.UtcNow >= nextRecoveryTime)
        {
            currentStamina++;
            nextRecoveryTime = nextRecoveryTime.AddMinutes(RecoveryMinutes);
        }

        if (IsFull) nextRecoveryTime = DateTime.MaxValue;

        Save();
        UIManager.Instance?.UpdateStamina();
    }

    void Save()
    {
        PlayerPrefs.SetInt("Stamina", currentStamina);
        // DateTime を long (binary) で保存
        PlayerPrefs.SetString("NextRecovery", nextRecoveryTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    void Load()
    {
        currentStamina = PlayerPrefs.GetInt("Stamina", MaxStamina);
        currentStamina = Mathf.Clamp(currentStamina, 0, MaxStamina);

        if (PlayerPrefs.HasKey("NextRecovery") &&
            long.TryParse(PlayerPrefs.GetString("NextRecovery"), out long bin))
        {
            nextRecoveryTime = DateTime.FromBinary(bin);
        }
        else
        {
            nextRecoveryTime = DateTime.MaxValue;
        }

        // アプリを閉じていた間の回復を一括処理
        if (!IsFull)
        {
            while (!IsFull && DateTime.UtcNow >= nextRecoveryTime)
            {
                currentStamina++;
                nextRecoveryTime = nextRecoveryTime.AddMinutes(RecoveryMinutes);
            }
            if (IsFull) nextRecoveryTime = DateTime.MaxValue;
            Save();
        }
    }
}
