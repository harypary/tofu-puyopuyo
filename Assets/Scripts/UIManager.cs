using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // ===== カラーパレット =====
    // タイトル背景: クリーム
    static readonly Color ColBg        = new Color(0.95f, 0.93f, 0.88f, 1f);
    // カード: 温かみのある白
    static readonly Color ColCard      = new Color(1.00f, 0.98f, 0.94f, 0.97f);
    // カード枠
    static readonly Color ColCardBorder= new Color(0.88f, 0.80f, 0.70f, 1f);
    // 朱色
    static readonly Color ColAccent    = new Color(0.85f, 0.25f, 0.12f, 1f);
    static readonly Color ColAccentDk  = new Color(0.65f, 0.15f, 0.05f, 1f);
    static readonly Color ColTextDark  = new Color(0.18f, 0.14f, 0.11f, 1f);
    static readonly Color ColTextMid   = new Color(0.50f, 0.42f, 0.36f, 1f);
    static readonly Color ColTextLight = Color.white;
    static readonly Color ColOverlay   = new Color(0f, 0f, 0f, 0.58f);
    static readonly Color ColGold      = new Color(0.92f, 0.72f, 0.08f, 1f);
    // スタミナ（タイトルのクリームに馴染む温かみ系）
    static readonly Color ColStCard    = new Color(0.99f, 0.96f, 0.90f, 0.98f);
    static readonly Color ColStBorder  = new Color(0.85f, 0.75f, 0.62f, 1f);
    static readonly Color ColHrtFull   = new Color(0.88f, 0.22f, 0.32f, 1f);  // 温かみ赤
    static readonly Color ColHrtEmpty  = new Color(0.80f, 0.73f, 0.65f, 1f);  // 温かみグレージュ
    static readonly Color ColTimer     = new Color(0.58f, 0.48f, 0.38f, 1f);  // 温かみ茶
    static readonly Color ColAdBtn     = new Color(0.75f, 0.55f, 0.18f, 1f);  // くすみゴールド
    static readonly Color ColAdBtnDk   = new Color(0.55f, 0.38f, 0.08f, 1f);
    // モードボタン
    static readonly Color ColModeOn    = new Color(0.85f, 0.25f, 0.12f, 1f);   // 選択中=朱
    static readonly Color ColModeOff   = new Color(0.82f, 0.76f, 0.68f, 0.45f); // 未選択=薄いベージュ

    // ===== 参照 =====
    Canvas canvas;

    // タイトル
    GameObject titlePanel;
    Text[]  heartTexts;
    Text    staminaTimerText;
    Text    staminaCountText;
    Button  modeNormalBtn;
    Button  modeHardBtn;
    Image   modeNormalImg;
    Image   modeHardImg;
    Text    modeNormalTxt;
    Text    modeHardTxt;

    // タイトル（動的更新）
    Text    titleBestText;

    // HUD
    GameObject hudPanel;
    Text    scoreValueText;
    Text    modeBadgeText;

    // ゲームオーバー
    GameObject gameOverPanel;
    Text    goScoreText;
    Text    goBestText;
    Button  goAdBtn;
    Button  goRetryBtn;
    Text    goRetryText;
    Image   goRetryImg;

    // ===== Lifecycle =====

    void Awake()
    {
        Instance = this;
        EnsureEventSystem();
        BuildCanvas();
        BuildTitlePanel();
        BuildHUDPanel();
        BuildGameOverPanel();
    }

    void Update()
    {
        if (staminaTimerText == null || StaminaManager.Instance == null) return;
        if (!StaminaManager.Instance.IsFull)
        {
            float s = StaminaManager.Instance.SecondsUntilNextRecovery;
            staminaTimerText.text = string.Format("{0:00}:{1:00} で 1 個回復",
                Mathf.FloorToInt(s / 60f), Mathf.FloorToInt(s % 60f));
        }
        else
        {
            staminaTimerText.text = "スタミナ満タン！";
        }
        if (staminaCountText != null)
            staminaCountText.text = StaminaManager.Instance.Current + " / " + StaminaManager.MaxStamina;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        esGo.AddComponent<InputSystemUIInputModule>();
#else
        esGo.AddComponent<StandaloneInputModule>();
#endif
    }

    // ===== Public API =====

    public void UpdateUI()
    {
        var state = GameManager.Instance?.State ?? GameState.Title;
        titlePanel.SetActive(state   == GameState.Title);
        hudPanel.SetActive(state     == GameState.Playing);
        gameOverPanel.SetActive(state == GameState.GameOver);

        if (state == GameState.Title && titleBestText != null)
        {
            int bsc = GameManager.Instance?.BestScore ?? PlayerPrefs.GetInt("BestScore", 0);
            titleBestText.text = "ベストスコア  " + bsc + " 個";
        }

        if (state == GameState.GameOver)
        {
            int sc  = GameManager.Instance?.Score     ?? 0;
            int bsc = GameManager.Instance?.BestScore  ?? 0;
            if (goScoreText != null) goScoreText.text = sc  + " 個";
            if (goBestText  != null) goBestText.text  = "ベスト  " + bsc + " 個";
            if (goAdBtn != null) goAdBtn.gameObject.SetActive(true);

            // スタミナに応じてリトライボタンの表示を切り替え
            bool hasStamina = StaminaManager.Instance == null || StaminaManager.Instance.Current > 0;
            if (goRetryText != null)
                goRetryText.text = hasStamina ? "もう一回" : "スタミナ不足";
            if (goRetryImg != null)
            {
                Color retryCol = hasStamina ? ColAccent : new Color(0.60f, 0.55f, 0.50f, 1f);
                goRetryImg.color = retryCol;
                if (goRetryBtn != null)
                {
                    var cb = goRetryBtn.colors;
                    cb.normalColor      = retryCol;
                    cb.highlightedColor = Color.Lerp(retryCol, Color.white, 0.15f);
                    cb.selectedColor    = retryCol;
                    goRetryBtn.colors   = cb;
                }
            }
        }

        RefreshModeButtons();
    }

    public void UpdateScore(int score)
    {
        if (scoreValueText != null) scoreValueText.text = score.ToString();
    }

    public void UpdateStamina()
    {
        if (heartTexts == null || StaminaManager.Instance == null) return;
        int cur = StaminaManager.Instance.Current;
        for (int i = 0; i < heartTexts.Length; i++)
            if (heartTexts[i] != null)
                heartTexts[i].color = i < cur ? ColHrtFull : ColHrtEmpty;
    }

    /// <summary>モードボタンのハイライトを現在の設定に合わせる</summary>
    public void RefreshModeButtons()
    {
        if (modeNormalImg == null || modeHardImg == null) return;
        bool normal = GameManager.Instance?.isEasyMode ?? true;

        ApplyModeButtonState(modeNormalBtn, modeNormalImg, modeNormalTxt, normal);
        ApplyModeButtonState(modeHardBtn,   modeHardImg,   modeHardTxt,   !normal);

        // HUDバッジ更新
        if (modeBadgeText != null)
            modeBadgeText.text = normal ? "NORMAL" : "HARD";
    }

    void ApplyModeButtonState(Button btn, Image img, Text txt, bool selected)
    {
        Color bg = selected ? ColModeOn : ColModeOff;
        Color tc = selected ? ColTextLight
                            : new Color(ColTextDark.r, ColTextDark.g, ColTextDark.b, 0.45f);
        img.color = bg;
        txt.color = tc;
        // ColorBlock も更新（ホバー時に Unity が normalColor で上書きするのを防ぐ）
        var cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.selectedColor    = bg;
        btn.colors = cb;
    }

    // ===== Canvas =====

    void BuildCanvas()
    {
        var go = new GameObject("UICanvas");
        go.transform.SetParent(transform);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        go.AddComponent<GraphicRaycaster>();
    }

    // ===== Title Panel =====

    void BuildTitlePanel()
    {
        titlePanel = MakePanel("TitlePanel", canvas.transform, ColBg);
        Stretch(titlePanel.GetComponent<RectTransform>());

        // ─ 上部（デコ・タイトル・サブ・説明）─
        // 各要素をピクセル単位で下から積み上げ設計（844px基準）
        // Deco center=523px→0.620, Title 440-495px, Subtitle 402-430px, Desc 338-390px

        // ─── 豆腐ブロックアイコン ───
        var deco = MakeRect("Deco", titlePanel.transform);
        AP(deco, new Vector2(0.5f, 0.780f));
        deco.sizeDelta = new Vector2(132, 86);
        // 外枠（豆腐の淡いクリーム縁）
        deco.gameObject.AddComponent<Image>().color = new Color(0.83f, 0.80f, 0.72f, 1f);
        Shadow(deco.gameObject, new Color(0.12f, 0.08f, 0.04f, 0.40f), new Vector2(3, -5));

        // 本体（豆腐の白い面）
        var tofuFace = MakeRect("TFace", deco);
        tofuFace.anchorMin = new Vector2(0.03f, 0.05f);
        tofuFace.anchorMax = new Vector2(0.97f, 0.95f);
        tofuFace.offsetMin = tofuFace.offsetMax = Vector2.zero;
        tofuFace.anchoredPosition = Vector2.zero;
        tofuFace.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.97f, 0.94f, 1f);

        // 上面ハイライト（光が当たった感）
        var topHL = MakeRect("THL", tofuFace);
        AR(topHL, new Vector2(0f, 0.55f), new Vector2(1f, 1f));
        topHL.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.45f);

        // 横の切り目（断面ライン）
        var cutLine = MakeRect("Cut", tofuFace);
        AR(cutLine, new Vector2(0.05f, 0.525f), new Vector2(0.95f, 0.540f));
        cutLine.gameObject.AddComponent<Image>().color = new Color(0.70f, 0.67f, 0.60f, 0.50f);

        // 「豆腐」文字（下半分、茶色で控えめに）
        var decoT = MakeText("DT", tofuFace, "豆腐", 30,
            new Color(0.55f, 0.42f, 0.25f, 0.75f), TextAnchor.MiddleCenter, true);
        AR(decoT.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.52f));

        var tT = MakeText("Title", titlePanel.transform, "とうふぷよぷよ",
            44, ColTextDark, TextAnchor.MiddleCenter, true);
        AR(tT.GetComponent<RectTransform>(), new Vector2(0.05f, 0.626f), new Vector2(0.95f, 0.692f));
        Shadow(tT.gameObject);

        var tS = MakeText("Sub", titlePanel.transform, "おとうふぷよぷよゲーム",
            16, ColTextMid, TextAnchor.MiddleCenter);
        AR(tS.GetComponent<RectTransform>(), new Vector2(0.05f, 0.581f), new Vector2(0.95f, 0.615f));

        var tD = MakeText("Desc", titlePanel.transform,
            "画面をタップして豆腐を積もう！\nどこまで積み上げられるかな？",
            14, new Color(0.48f, 0.40f, 0.34f, 1f), TextAnchor.MiddleCenter);
        AR(tD.GetComponent<RectTransform>(), new Vector2(0.05f, 0.506f), new Vector2(0.95f, 0.567f));

        // ─── 中部（モード・あそぶ）───
        BuildModeSelector(titlePanel.transform);

        var playBtn = MakeButton("Play", titlePanel.transform,
            "あ そ ぶ", 24, ColAccent, ColAccentDk, ColTextLight,
            new Vector2(0.5f, 0.337f), new Vector2(170, 52));
        Shadow(playBtn.gameObject, new Color(0.4f, 0.05f, 0f, 0.45f), new Vector2(0, -4));
        playBtn.onClick.AddListener(() => GameManager.Instance?.StartGame());

        // ─── ベストスコア：スタミナカード上に 66px 余白を取って配置 ───
        int bsc = PlayerPrefs.GetInt("BestScore", 0);
        var tB = MakeText("Best", titlePanel.transform, "ベストスコア  " + bsc + " 個",
            15, ColTextMid, TextAnchor.MiddleCenter);
        AR(tB.GetComponent<RectTransform>(), new Vector2(0.1f, 0.254f), new Vector2(0.9f, 0.283f));
        titleBestText = tB.GetComponent<Text>();

        // スタミナカード
        BuildStaminaCard(titlePanel.transform);

        titlePanel.SetActive(false);
    }

    void BuildModeSelector(Transform parent)
    {
        // ラベル
        // モードラベル: center=309px→0.366 / モードボタン: center=262px→0.310
        var lbl = MakeText("ModeLbl", parent, "モードを選択", 13, ColTextMid, TextAnchor.MiddleCenter);
        AR(lbl.GetComponent<RectTransform>(), new Vector2(0.1f, 0.456f), new Vector2(0.9f, 0.487f));

        modeNormalBtn = MakeButton("ModeNormal", parent,
            "ノーマル", 17, ColModeOn, ColAccentDk, ColTextLight,
            new Vector2(0.30f, 0.415f), new Vector2(120, 44));
        modeNormalImg = modeNormalBtn.GetComponent<Image>();
        modeNormalTxt = modeNormalBtn.GetComponentInChildren<Text>();
        modeNormalBtn.onClick.AddListener(() => GameManager.Instance?.SetMode(true));

        modeHardBtn = MakeButton("ModeHard", parent,
            "ハード", 17, ColModeOff, ColAccentDk, ColTextDark,
            new Vector2(0.70f, 0.415f), new Vector2(120, 44));
        modeHardImg = modeHardBtn.GetComponent<Image>();
        modeHardTxt = modeHardBtn.GetComponentInChildren<Text>();
        modeHardBtn.onClick.AddListener(() => GameManager.Instance?.SetMode(false));

        RefreshModeButtons();
    }

    void BuildStaminaCard(Transform parent)
    {
        var card = MakeRect("StCard", parent);
        AP(card, new Vector2(0.5f, 0f));
        card.pivot           = new Vector2(0.5f, 0f);
        card.sizeDelta       = new Vector2(340, 138);
        card.anchoredPosition = new Vector2(0, 52);

        // 外枠（温かみベージュ）
        var border = card.gameObject.AddComponent<Image>();
        border.color = ColStBorder;
        Shadow(card.gameObject, new Color(0, 0, 0, 0.18f), new Vector2(0, -3));

        // 内側（クリーム）
        var inner = MakeRect("Inner", card);
        inner.anchorMin = new Vector2(0.01f, 0.05f);
        inner.anchorMax = new Vector2(0.99f, 0.95f);
        inner.offsetMin = inner.offsetMax = Vector2.zero;
        inner.anchoredPosition = Vector2.zero;
        inner.gameObject.AddComponent<Image>().color = ColStCard;

        // ラベル行（上部）
        var stLbl = MakeText("SL", inner, "STAMINA", 11, ColTimer, TextAnchor.UpperLeft, true);
        var slRt  = stLbl.GetComponent<RectTransform>();
        slRt.anchorMin = new Vector2(0f,    0.78f);
        slRt.anchorMax = new Vector2(0.55f, 1f);
        slRt.offsetMin = new Vector2(10, 0);
        slRt.offsetMax = new Vector2(0, -4);
        slRt.anchoredPosition = Vector2.zero;

        int cur = StaminaManager.Instance?.Current ?? StaminaManager.MaxStamina;
        staminaCountText = MakeText("SC", inner, cur + " / " + StaminaManager.MaxStamina,
            12, ColTimer, TextAnchor.UpperRight, true).GetComponent<Text>();
        var scRt = staminaCountText.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.55f, 0.78f);
        scRt.anchorMax = new Vector2(1f, 1f);
        scRt.offsetMin = new Vector2(0, 0);
        scRt.offsetMax = new Vector2(-10, -4);
        scRt.anchoredPosition = Vector2.zero;

        // ハート ×5（中上段）
        heartTexts = new Text[StaminaManager.MaxStamina];
        float iW = 38f, gap = 7f;
        float total = iW * StaminaManager.MaxStamina + gap * (StaminaManager.MaxStamina - 1);
        float sx    = -total / 2f + iW / 2f;
        for (int i = 0; i < StaminaManager.MaxStamina; i++)
        {
            var hRt = MakeRect("H" + i, inner);
            hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.62f);
            hRt.sizeDelta        = new Vector2(iW, iW);
            hRt.anchoredPosition = new Vector2(sx + i * (iW + gap), 0);

            bool filled = i < cur;
            var ht = MakeText("HT", hRt, "♥", 30,
                filled ? ColHrtFull : ColHrtEmpty, TextAnchor.MiddleCenter, true);
            Stretch(ht.GetComponent<RectTransform>());
            if (filled) Shadow(ht.gameObject, new Color(0.7f, 0f, 0.1f, 0.3f), new Vector2(0, -1));
            heartTexts[i] = ht.GetComponent<Text>();
        }

        // タイマー（中段・ハートの下・広告ボタンの上）
        staminaTimerText = MakeText("Timer", inner, "", 13, ColTimer, TextAnchor.MiddleCenter)
            .GetComponent<Text>();
        var tRt = staminaTimerText.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.34f);
        tRt.anchorMax = new Vector2(1f, 0.56f);
        tRt.offsetMin = new Vector2(8, 0);
        tRt.offsetMax = new Vector2(-8, 0);
        tRt.anchoredPosition = Vector2.zero;

        // 広告ボタン（下段）
        var adBtn = MakeButton("Ad", inner, "▶  広告を見てフル回復", 13,
            ColAdBtn, ColAdBtnDk, new Color(0.15f, 0.10f, 0.04f, 1f),
            new Vector2(0.5f, 0f), new Vector2(222, 30));
        var adRt = adBtn.GetComponent<RectTransform>();
        adRt.anchorMin = adRt.anchorMax = new Vector2(0.5f, 0f);
        adRt.pivot           = new Vector2(0.5f, 0f);
        adRt.sizeDelta       = new Vector2(222, 30);
        adRt.anchoredPosition = new Vector2(0, 5);
        adBtn.onClick.AddListener(() =>
        {
            AdManager.Instance?.ShowStaminaAd(() =>
            {
                StaminaManager.Instance?.RecoverFull();
                UpdateStamina();
            });
        });
    }

    // ===== HUD Panel =====

    void BuildHUDPanel()
    {
        hudPanel = MakePanel("HUDPanel", canvas.transform, Color.clear);
        Stretch(hudPanel.GetComponent<RectTransform>());

        // スコアカード（上中央）
        var sc = MakeRect("SC", hudPanel.transform);
        AP(sc, new Vector2(0.5f, 1f));
        sc.pivot           = new Vector2(0.5f, 1f);
        sc.sizeDelta       = new Vector2(195, 86);
        sc.anchoredPosition = new Vector2(0, -SafeTop() - 90);
        sc.gameObject.AddComponent<Image>().color = new Color(1f, 0.98f, 0.94f, 0.97f);
        Shadow(sc.gameObject, new Color(0, 0, 0, 0.18f));

        var lbl = MakeText("L", sc, "積み上げた豆腐", 13, ColTextMid, TextAnchor.UpperCenter);
        AR(lbl.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 1f));
        lbl.GetComponent<RectTransform>().offsetMin = new Vector2(6, 4);
        lbl.GetComponent<RectTransform>().offsetMax = new Vector2(-6, -4);

        var val = MakeText("V", sc, "0", 36, ColAccent, TextAnchor.LowerCenter, true);
        AR(val.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.54f));
        val.GetComponent<RectTransform>().offsetMin = new Vector2(6, 4);
        val.GetComponent<RectTransform>().offsetMax = new Vector2(-6, -4);
        scoreValueText = val.GetComponent<Text>();

        // モードバッジ（左上）
        var badge = MakeRect("Badge", hudPanel.transform);
        AP(badge, new Vector2(0f, 1f));
        badge.pivot           = new Vector2(0f, 1f);
        badge.sizeDelta       = new Vector2(78, 34);
        badge.anchoredPosition = new Vector2(12, -SafeTop() - 90);
        badge.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.11f, 0.82f);

        bool normal = GameManager.Instance?.isEasyMode ?? true;
        modeBadgeText = MakeText("BT", badge, normal ? "NORMAL" : "HARD",
            13, ColTextLight, TextAnchor.MiddleCenter, true).GetComponent<Text>();
        Stretch(modeBadgeText.GetComponent<RectTransform>());

        // ホームボタン（右上）
        var homeBtn = MakeButton("Home", hudPanel.transform, "HOME", 12,
            new Color(0.18f, 0.14f, 0.11f, 0.75f), new Color(0.08f, 0.06f, 0.04f, 0.9f), ColTextLight,
            new Vector2(1f, 1f), new Vector2(66, 34));
        var homeRt = homeBtn.GetComponent<RectTransform>();
        homeRt.anchorMin = homeRt.anchorMax = new Vector2(1f, 1f);
        homeRt.pivot     = new Vector2(1f, 1f);
        homeRt.sizeDelta = new Vector2(66, 34);
        homeRt.anchoredPosition = new Vector2(-12, -SafeTop() - 90);
        homeBtn.onClick.AddListener(() => GameManager.Instance?.ReturnToTitle());

        hudPanel.SetActive(false);
    }

    // ===== GameOver Panel =====

    void BuildGameOverPanel()
    {
        gameOverPanel = MakePanel("GOPanel", canvas.transform, Color.clear);
        Stretch(gameOverPanel.GetComponent<RectTransform>());

        // オーバーレイ
        var ov = MakeRect("OV", gameOverPanel.transform);
        Stretch(ov);
        ov.gameObject.AddComponent<Image>().color = ColOverlay;

        // カード
        var card = MakeRect("Card", gameOverPanel.transform);
        AP(card, new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(315, 470);
        card.gameObject.AddComponent<Image>().color = ColCard;
        Shadow(card.gameObject, new Color(0, 0, 0, 0.35f), new Vector2(0, -8));

        // タイトル
        var gt = MakeText("GT", card, "ゲームオーバー", 32, ColAccent, TextAnchor.MiddleCenter, true);
        AR(gt.GetComponent<RectTransform>(), new Vector2(0.04f, 0.83f), new Vector2(0.96f, 0.97f));
        Shadow(gt.gameObject, new Color(0.4f, 0.05f, 0f, 0.35f), new Vector2(1, -1));

        // 区切り線
        var line = MakeRect("Line", card);
        AR(line, new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.814f));
        line.gameObject.AddComponent<Image>().color = ColCardBorder;

        // スコアラベル
        var sl = MakeText("SL", card, "今回のスコア", 15, ColTextMid, TextAnchor.MiddleCenter);
        AR(sl.GetComponent<RectTransform>(), new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.79f));

        // スコア値
        var sv = MakeText("SV", card, "0 個", 44, ColTextDark, TextAnchor.MiddleCenter, true);
        goScoreText = sv.GetComponent<Text>();
        AR(sv.GetComponent<RectTransform>(), new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.71f));

        // ベスト
        var bv = MakeText("BV", card, "ベスト  0 個", 19, ColGold, TextAnchor.MiddleCenter, true);
        goBestText = bv.GetComponent<Text>();
        AR(bv.GetComponent<RectTransform>(), new Vector2(0.04f, 0.47f), new Vector2(0.96f, 0.57f));

        // 広告を見て続きから（ゴールドボタン）
        goAdBtn = MakeButton("AdContinue", card, "▶  広告を見て続きから", 16,
            ColAdBtn, ColAdBtnDk, new Color(0.10f, 0.07f, 0.02f, 1f),
            new Vector2(0.5f, 0.365f), new Vector2(242, 46));
        Shadow(goAdBtn.gameObject, new Color(0.3f, 0.15f, 0f, 0.35f), new Vector2(0, -2));
        goAdBtn.onClick.AddListener(() =>
        {
            AdManager.Instance?.ShowRevengeAd(() =>
            {
                goAdBtn.gameObject.SetActive(false);
                GameManager.Instance?.ContinueGame();
            });
        });

        // もう一回
        var retry = MakeButton("Retry", card, "もう一回", 26,
            ColAccent, ColAccentDk, ColTextLight, new Vector2(0.5f, 0.21f), new Vector2(210, 58));
        Shadow(retry.gameObject, new Color(0.4f, 0.05f, 0f, 0.4f), new Vector2(0, -3));
        goRetryBtn  = retry;
        goRetryText = retry.GetComponentInChildren<Text>();
        goRetryImg  = retry.GetComponent<Image>();
        retry.onClick.AddListener(() => GameManager.Instance?.QuickRestart());

        // タイトルへ
        var toTitle = MakeButton("ToT", card, "タイトルへ", 16,
            new Color(0.82f, 0.76f, 0.68f, 1f), new Color(0.65f, 0.58f, 0.50f, 1f), ColTextDark,
            new Vector2(0.5f, 0.042f), new Vector2(152, 42));
        toTitle.onClick.AddListener(() => GameManager.Instance?.RetryGame());

        gameOverPanel.SetActive(false);
    }

    // ===== Helpers =====

    float SafeTop()
    {
#if UNITY_IOS
        return Mathf.Max(0f, Screen.height - Screen.safeArea.height - Screen.safeArea.y);
#else
        return 0f;
#endif
    }

    GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    Text MakeText(string name, Transform parent, string content,
                  int size, Color color, TextAnchor anchor, bool bold = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text               = content;
        t.fontSize           = size;
        t.color              = color;
        t.alignment          = anchor;
        t.fontStyle          = bold ? FontStyle.Bold : FontStyle.Normal;
        t.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return t;
    }

    Button MakeButton(string name, Transform parent, string label, int size,
                      Color bg, Color pressed, Color textCol, Vector2 anchor, Vector2 sz)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = sz;
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.pressedColor     = pressed;
        cb.selectedColor    = bg;
        btn.colors          = cb;
        btn.targetGraphic   = img;

        var t = MakeText(name + "T", go.transform, label, size, textCol, TextAnchor.MiddleCenter, true);
        Stretch(t.GetComponent<RectTransform>());
        return btn;
    }

    void Shadow(GameObject go, Color? col = null, Vector2? offset = null)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectColor    = col    ?? new Color(0f, 0f, 0f, 0.22f);
        sh.effectDistance = offset ?? new Vector2(2f, -2f);
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // アンカーポイント（sizeDelta で大きさを決める）
    void AP(RectTransform rt, Vector2 point)
    {
        rt.anchorMin = rt.anchorMax = point;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // アンカー範囲（子が親に対して割合で伸縮）
    void AR(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
