using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Title, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Spawner spawner;
    public bool isEasyMode = true; // true=ノーマル(回転なし), false=ハード(回転あり)

    private int score        = 0;
    private int bestScore    = 0;
    private int continueCount = 0;          // 広告リベンジ使用回数
    public const int MaxContinues = 2;      // 1ゲームあたりの最大リベンジ回数
    public bool CanContinue => continueCount < MaxContinues;
    private GameState gameState = GameState.Title;

    public GameState State     => gameState;
    public int       Score     => score;
    public int       BestScore => bestScore;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        bestScore  = PlayerPrefs.GetInt("BestScore", 0);
        isEasyMode = PlayerPrefs.GetInt("GameMode",  1) == 1; // 1=ノーマル
    }

    void Start()
    {
        // 旧シーン設定の残骸（InitialTofu）を削除
        var oldTofu = GameObject.Find("InitialTofu");
        if (oldTofu != null) Destroy(oldTofu);

        if (spawner != null) spawner.gameObject.SetActive(false);
        SetState(GameState.Title);
    }

    public void SetState(GameState newState)
    {
        gameState = newState;
        UIManager.Instance?.UpdateUI();
    }

    public void StartGame()
    {
        // スタミナ消費（0なら開始しない）
        if (StaminaManager.Instance != null && !StaminaManager.Instance.UseStamina())
        {
            Debug.Log("[Game] スタミナ不足 — ゲーム開始できません");
            DebugOverlay.AddEvent("StartGame: スタミナ不足");
            return;
        }

        DebugOverlay.AddEvent("StartGame");
        continueCount = 0;
        score = 0;
        UIManager.Instance?.UpdateScore(0);
        ResetSpawnerAndCamera();
        if (spawner != null)
        {
            spawner.gameObject.SetActive(true);
            spawner.SpawnNewTofu(); // OnEnable に依存せず必ず呼ぶ
            DebugOverlay.AddEvent($"StartGame: SpawnNewTofu done cur={spawner.HasCurrentTofu}");
        }
        SetState(GameState.Playing);
    }

    public void OnTofuPlaced(Tofu tofu)
    {
        if (gameState != GameState.Playing || spawner == null) return;
        score++;
        UIManager.Instance?.UpdateScore(score);
        DebugOverlay.AddEvent($"OnTofuPlaced score={score}");

        // 積み上げた豆腐の最高点がスポーナーに近づいたら上昇させる
        float stackTop = tofu.transform.position.y + 0.5f; // 豆腐の高さの半分を加算
        float spawnY   = spawner.transform.position.y;
        if (stackTop > spawnY - GameConfig.SpawnClearance)
        {
            float newSpawnY = stackTop + GameConfig.SpawnClearance;
            spawner.MoveUp(newSpawnY);
            GameEffect.Instance?.SetCameraTargetY(newSpawnY + GameConfig.CamAboveSpawn);
        }

        spawner.SpawnNewTofu();
        DebugOverlay.AddEvent($"OnTofuPlaced: SpawnNewTofu done cur={spawner.HasCurrentTofu}");
    }

    public void GameOver()
    {
        if (gameState != GameState.Playing) return;
        DebugOverlay.AddEvent($"GameOver score={score}");

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
        }
        PlayerPrefs.Save();

        if (spawner != null) spawner.gameObject.SetActive(false);
        SetState(GameState.GameOver);
    }

    /// <summary>タイトル画面のモードボタンから呼ぶ</summary>
    public void SetMode(bool normalMode)
    {
        isEasyMode = normalMode;
        PlayerPrefs.SetInt("GameMode", normalMode ? 1 : 0);
        PlayerPrefs.Save();
        UIManager.Instance?.RefreshModeButtons();
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>ゲームオーバー後に即リスタート（タイトルに戻らず直接プレイ再開）</summary>
    public void QuickRestart()
    {
        // スタミナ消費（0なら開始しない）
        if (StaminaManager.Instance != null && !StaminaManager.Instance.UseStamina())
        {
            Debug.Log("[Game] スタミナ不足 — リスタートできません");
            DebugOverlay.AddEvent("QuickRestart: スタミナ不足");
            return;
        }

        DebugOverlay.AddEvent("QuickRestart");
        if (spawner != null) spawner.gameObject.SetActive(false);
        // アクティブな豆腐のみ削除。FindGameObjectsWithTag はアクティブ限定のため
        // 非アクティブ（DDOL テンプレート / スポーナー無効時の子）は返らず安全。
        DestroyActiveTofus(minY: float.NegativeInfinity);
        continueCount = 0;
        score = 0;
        UIManager.Instance?.UpdateScore(0);
        ResetSpawnerAndCamera();
        if (spawner != null)
        {
            spawner.gameObject.SetActive(true);
            spawner.SpawnNewTofu(); // OnEnable に依存せず必ず呼ぶ
        }
        SetState(GameState.Playing);
    }

    /// <summary>広告を見た後に続きからプレイ（ゲームオーバー画面から）</summary>
    public void ContinueGame()
    {
        if (gameState != GameState.GameOver) return;
        continueCount++;

        // 落下した豆腐（y < -2）のみ削除。テンプレートは非アクティブなので対象外。
        DestroyActiveTofus(minY: float.NegativeInfinity, maxY: -2f);

        if (spawner != null)
        {
            spawner.gameObject.SetActive(true);
            spawner.SpawnNewTofu(); // OnEnable に依存せず必ず呼ぶ
        }
        SetState(GameState.Playing);
    }

    /// <summary>プレイ中にタイトルへ戻る</summary>
    public void ReturnToTitle()
    {
        DebugOverlay.AddEvent("ReturnToTitle");
        if (spawner != null) spawner.gameObject.SetActive(false);
        // 豆腐は spawner の子にしていないため SetActive(false) の影響を受けない。
        // FindGameObjectsWithTag でアクティブ豆腐を全削除できる。
        DestroyActiveTofus(minY: float.NegativeInfinity);
        continueCount = 0;
        score = 0;
        ResetSpawnerAndCamera();
        SetState(GameState.Title);
    }

    /// <summary>
    /// "Tofu" タグのアクティブな GameObject を削除する。
    /// 豆腐は spawner の子にしていないので spawner の SetActive に関係なく常にアクティブ。
    /// FindGameObjectsWithTag で確実に全豆腐を検出できる。
    /// maxY を指定するとその Y 座標より低いものだけ削除（ContinueGame 用）。
    /// </summary>
    void DestroyActiveTofus(float minY = float.NegativeInfinity, float maxY = float.PositiveInfinity)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Tofu"))
        {
            if (go.transform.position.y >= minY && go.transform.position.y <= maxY)
            {
                // SetActive(false) を先に呼ぶことで、Destroy() が遅延実行されるまでの間に
                // Tofu.Update() が GameOver() を誤発動するのを同フレーム内で防ぐ
                go.SetActive(false);
                Destroy(go);
            }
        }
    }

    /// <summary>
    /// iOS でアプリがバックグラウンドから復帰したとき（ホームボタン → 再起動）に呼ばれる。
    /// Playing 状態なのに豆腐がない場合は再スポーンして正常に戻す。
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return; // フォーカスを失った時は何もしない
        if (gameState != GameState.Playing) return;
        if (spawner == null) return;
        if (!spawner.HasTofu)
        {
            Debug.Log("[GameManager] OnApplicationFocus: Playing 中に tofu なし → 再スポーン");
            spawner.SpawnNewTofu();
        }
    }

    void ResetSpawnerAndCamera()
    {
        if (spawner != null)
            spawner.transform.position = new Vector3(
                spawner.transform.position.x, GameConfig.SpawnHeight,
                spawner.transform.position.z);
        GameEffect.Instance?.ResetCamera();
    }
}
