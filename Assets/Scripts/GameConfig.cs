/// <summary>
/// ゲームの基本パラメータをここで一元管理する。
/// このファイルを見れば「ゲームの基本内容」が確認・変更できる。
/// </summary>
public static class GameConfig
{
    // ===== 豆腐の動き =====
    public const float MoveSpeed    = 2.0f;   // 横移動の速さ（Perlin ノイズの周波数）
    public const float MoveRange    = 2.0f;   // 横移動の幅（±m）
    public const float DropCooldown = 0.5f;   // ゲーム開始直後の誤タップ防止（秒）
    public const float SpawnHeight  = 6.0f;   // 豆腐の出現 Y 座標

    // ===== 豆腐の揺れ =====
    public const float WobbleLand      = 0.30f;  // 着地時の揺れ量（物理分離したので大きくても跳ね影響なし）
    public const float WobbleDrop      = 0.22f;  // ドロップ時の揺れ量
    public const float WobbleDecayRate = 1.8f;   // 揺れの減衰速度（ゆっくり収まる）
    public const float WobbleFrequency = 8f;     // 揺れの振動周波数（低いほどゆっくりぷるん）
    public const float WobbleImpact    = 0.20f;  // 衝突を受けた豆腐に伝わる揺れ量
    public const float WobbleIdleAmp   = 0.15f;  // 空中待機中の微振動量
    public const float WobbleIdleFreq  = 12f;    // 空中待機中の微振動周波数

    // ===== スタミナ =====
    public const int   MaxStamina          = 5;   // 最大スタミナ数
    public const float RecoveryMinutes     = 10f; // 1 個回復にかかる時間（分）

    // ===== カメラ =====
    public const float CamFOV = 60f;
    // カメラ位置・角度は AutoSetupTofuGame.cs で設定（pos: 0,8,-15 / rot: 15,0,0）

    // ===== スポーナー・カメラ高さ追従 =====
    public const float SpawnClearance  = 3.0f;  // 積み上げ豆腐 top から何m 空けてスポーンするか
    public const float CamAboveSpawn   = 2.0f;  // スポーナー Y からカメラ Y までのオフセット（cam=8, spawn=6 → 差=2）
    public const float CamFollowSpeed  = 2.0f;  // カメラが上昇目標に追従する速さ

    // ===== 物理 =====
    // 地面: Cube pos(0,-1,0) scale(10,1,10)
    // スポーン pos: (0, SpawnHeight, 0)
    // ゲームオーバー判定 Y: -5f（Tofu.cs）
}
