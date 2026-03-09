using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// おとうふぷよぷよゲーム — アプリアイコン自動生成
/// TofuGame > Generate App Icon を実行すると 1024×1024 PNG を Assets/AppIcon.png に保存します。
/// 生成後: Player Settings > iOS > Icons でこの画像を設定してください。
/// </summary>
public class TofuIconGenerator : EditorWindow
{
    [MenuItem("TofuGame/Generate App Icon (1024x1024)")]
    static void Generate()
    {
        const int S = 1024;
        var px = new Color[S * S];

        // ━━ 背景：暖かみのある青グラデーション ━━
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float t = (float)y / S;
            px[y * S + x] = Color.Lerp(
                new Color(0.22f, 0.50f, 0.84f),   // 上：ディープブルー
                new Color(0.56f, 0.82f, 0.96f),   // 下：スカイブルー
                1f - t
            );
        }

        // ━━ 豆腐ブロックの影（楕円） ━━
        int scx = S / 2, scy = (int)(S * 0.605f);
        int srx = (int)(S * 0.30f), sry = (int)(S * 0.06f);
        DrawEllipse(px, S, scx, scy, srx, sry, new Color(0.10f, 0.20f, 0.40f, 0.40f));

        // ━━ 豆腐ブロック本体 ━━
        int bx = S / 2, by = (int)(S * 0.48f);
        int bw = (int)(S * 0.60f), bh = (int)(S * 0.42f);
        int corner = (int)(S * 0.065f);
        DrawRoundRect(px, S, bx - bw/2, by - bh/2, bw, bh, corner,
                      new Color(0.80f, 0.76f, 0.66f));                     // 外枠クリーム

        DrawRoundRect(px, S, bx - bw/2 + 8, by - bh/2 + 8, bw - 16, bh - 16, corner - 4,
                      new Color(0.97f, 0.96f, 0.93f));                     // 豆腐本体

        // 上面ハイライト（上半分を少し白く）
        DrawRoundRectAlpha(px, S, bx - bw/2 + 8, by, bw - 16, bh/2 - 8, corner - 4,
                           new Color(1f, 1f, 1f, 0.32f));

        // 横の断面ライン
        int lineY = by + 4;
        for (int x = bx - bw/2 + 22; x < bx + bw/2 - 22; x++)
        for (int dy = -4; dy <= 4; dy++)
        {
            int py2 = lineY + dy;
            if (py2 < 0 || py2 >= S) continue;
            float a = 0.45f * (1f - Mathf.Abs(dy) / 5f);
            BlendPixel(ref px[py2 * S + x], new Color(0.60f, 0.56f, 0.48f), a);
        }

        // ━━ ぷよぷよ感：白いバブル ━━
        DrawBubble(px, S, (int)(S*0.18f), (int)(S*0.28f), (int)(S*0.07f), 0.55f);
        DrawBubble(px, S, (int)(S*0.84f), (int)(S*0.22f), (int)(S*0.05f), 0.45f);
        DrawBubble(px, S, (int)(S*0.80f), (int)(S*0.74f), (int)(S*0.055f), 0.40f);
        DrawBubble(px, S, (int)(S*0.14f), (int)(S*0.70f), (int)(S*0.04f), 0.35f);

        // ━━ テクスチャ保存 ━━
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        tex.Apply();

        string dir  = Application.dataPath;
        string path = dir + "/AppIcon.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.Refresh();
        Debug.Log("★ アイコン生成完了 → Assets/AppIcon.png");
        Debug.Log("次のステップ: Edit > Project Settings > Player > iOS > Icons でこの画像を設定してください。");
        EditorUtility.DisplayDialog("アイコン生成完了",
            "Assets/AppIcon.png を作成しました。\n\n" +
            "Edit > Project Settings > Player > iOS > Icons\n" +
            "でこの画像を設定してください。", "OK");
    }

    // ─────────────────────────────────────────
    // 描画ユーティリティ
    // ─────────────────────────────────────────

    static void DrawRoundRect(Color[] px, int S, int x0, int y0, int w, int h, int r, Color col)
    {
        for (int y = y0; y < y0 + h; y++)
        for (int x = x0; x < x0 + w; x++)
        {
            if (x < 0 || x >= S || y < 0 || y >= S) continue;
            if (!InRoundRect(x, y, x0, y0, w, h, r)) continue;
            px[y * S + x] = col;
        }
    }

    static void DrawRoundRectAlpha(Color[] px, int S, int x0, int y0, int w, int h, int r, Color col)
    {
        for (int y = y0; y < y0 + h; y++)
        for (int x = x0; x < x0 + w; x++)
        {
            if (x < 0 || x >= S || y < 0 || y >= S) continue;
            if (!InRoundRect(x, y, x0, y0, w, h, r)) continue;
            BlendPixel(ref px[y * S + x], col, col.a);
        }
    }

    static bool InRoundRect(int px, int py, int x0, int y0, int w, int h, int r)
    {
        int lx = px - x0, rx = (x0 + w - 1) - px;
        int ly = py - y0, ry = (y0 + h - 1) - py;
        if (lx < 0 || rx < 0 || ly < 0 || ry < 0) return false;
        if (lx < r && ly < r) return Dist(r - lx, r - ly) <= r;
        if (rx < r && ly < r) return Dist(r - rx, r - ly) <= r;
        if (lx < r && ry < r) return Dist(r - lx, r - ry) <= r;
        if (rx < r && ry < r) return Dist(r - rx, r - ry) <= r;
        return true;
    }

    static float Dist(float a, float b) => Mathf.Sqrt(a * a + b * b);

    static void DrawEllipse(Color[] px, int S, int cx, int cy, int rx, int ry, Color col)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
        for (int x = cx - rx; x <= cx + rx; x++)
        {
            if (x < 0 || x >= S || y < 0 || y >= S) continue;
            float nx = (float)(x - cx) / rx, ny = (float)(y - cy) / ry;
            float d  = Mathf.Sqrt(nx * nx + ny * ny);
            if (d > 1f) continue;
            float a = col.a * (1f - d);
            BlendPixel(ref px[y * S + x], col, a);
        }
    }

    static void DrawBubble(Color[] px, int S, int cx, int cy, int r, float alpha)
    {
        for (int y = cy - r; y <= cy + r; y++)
        for (int x = cx - r; x <= cx + r; x++)
        {
            if (x < 0 || x >= S || y < 0 || y >= S) continue;
            float d = Dist(x - cx, y - cy);
            if (d > r) continue;
            // 縁リング状（ぷよのアウトライン風）
            float ring = Mathf.Clamp01(1f - Mathf.Abs(d / r - 0.78f) / 0.22f);
            BlendPixel(ref px[y * S + x], Color.white, ring * alpha);
        }
    }

    static void BlendPixel(ref Color dst, Color src, float a)
    {
        dst.r = dst.r * (1f - a) + src.r * a;
        dst.g = dst.g * (1f - a) + src.g * a;
        dst.b = dst.b * (1f - a) + src.b * a;
    }
}
