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
            return;
        }

        continueCount = 0;
        score = 0;
        UIManager.Instance?.UpdateScore(0);
        if (spawner != null)
            spawner.gameObject.SetActive(true);
        SetState(GameState.Playing);
    }

    public void OnTofuPlaced(Tofu tofu)
    {
        if (gameState != GameState.Playing || spawner == null) return;
        score++;
        UIManager.Instance?.UpdateScore(score);
        spawner.SpawnNewTofu();
    }

    public void GameOver()
    {
        if (gameState != GameState.Playing) return;

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
            return;
        }

        if (spawner != null) spawner.gameObject.SetActive(false);
        // 非アクティブ含む全豆腐を削除（Spawner子の非アクティブ豆腐も確実に削除）
        foreach (var t in FindObjectsByType<Tofu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(t.gameObject);
        continueCount = 0;
        score = 0;
        UIManager.Instance?.UpdateScore(0);
        if (spawner != null) spawner.gameObject.SetActive(true);
        SetState(GameState.Playing);
    }

    /// <summary>広告を見た後に続きからプレイ（ゲームオーバー画面から）</summary>
    public void ContinueGame()
    {
        if (gameState != GameState.GameOver) return;
        continueCount++;

        // 画面外に落ちた豆腐を削除（残ったままだと次フレームで即 GameOver が再トリガーされる）
        foreach (var t in FindObjectsByType<Tofu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.transform.position.y < -2f) Destroy(t.gameObject);

        if (spawner != null) spawner.gameObject.SetActive(true);
        SetState(GameState.Playing);
    }

    /// <summary>プレイ中にタイトルへ戻る</summary>
    public void ReturnToTitle()
    {
        if (spawner != null) spawner.gameObject.SetActive(false);
        // 積まれた豆腐を全て削除（非アクティブも含む）
        foreach (var t in FindObjectsByType<Tofu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(t.gameObject);
        continueCount = 0;
        score = 0;
        SetState(GameState.Title);
    }
}
