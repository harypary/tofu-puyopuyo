using UnityEngine;

/// <summary>
/// iOS 実機デバッグ用オーバーレイ。
/// GameBootstrap から自動追加される。
/// 原因特定後は GameBootstrap の呼び出しごと削除すること。
/// </summary>
public class DebugOverlay : MonoBehaviour
{
    GUIStyle style;
    string log = "";
    int logCount = 0;
    const int MaxLines = 20;

    void Awake()
    {
        style = new GUIStyle();
        style.fontSize    = 28;
        style.normal.textColor = Color.yellow;
        // 背景を半透明黒に
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
        tex.Apply();
        style.normal.background = tex;
        style.wordWrap  = true;
        style.padding   = new RectOffset(8, 8, 8, 8);
    }

    void Update()
    {
        // 毎フレーム状態を表示（上書き）
        var gm = GameManager.Instance;
        var sp = gm?.spawner;

        string state    = gm  != null ? gm.State.ToString()       : "GM=null";
        string isMoving = sp  != null ? sp.IsMovingDebug.ToString(): "SP=null";
        string hasCur   = sp  != null ? sp.HasCurrentTofu.ToString(): "?";
        string hasDrop  = sp  != null ? sp.HasDroppedTofu.ToString(): "?";
        string timer    = sp  != null ? sp.NoTofuTimerDebug.ToString("F1") : "?";

        log = $"State: {state}\n"
            + $"isMoving: {isMoving}\n"
            + $"currentTofu: {hasCur}\n"
            + $"droppedTofu: {hasDrop}\n"
            + $"noTofuTimer: {timer}s\n"
            + $"--- events ---\n"
            + eventLog;
    }

    // イベントログ（外部から追記）
    static string eventLog = "";
    public static void AddEvent(string msg)
    {
        string[] lines = eventLog.Split('\n');
        if (lines.Length > 8)
            eventLog = string.Join("\n", lines, 0, 8);
        eventLog = $"[{Time.time:F1}] {msg}\n" + eventLog;
    }

    void OnGUI()
    {
        float w = Screen.width * 0.95f;
        GUI.Label(new Rect(10, 10, w, Screen.height * 0.5f), log, style);
    }
}
