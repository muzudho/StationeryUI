namespace StationeryUI.MonoGame.Controls.Headline;

using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame;

/// <summary>
/// 画面内で最も強く伝えたいメッセージを表示する、大見出し用の文房具 UI です。
/// </summary>
public sealed class Headline
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    public Headline(string text, Vector2 position, Color color, float textScale)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Position = position;
        Color = color;
        TextScale = textScale;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    public string Text { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public float TextScale { get; set; }

    // ========================================
    // 機能
    // ========================================

    public void Draw(StationeryDrawingTools surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        surface.DrawText(Text, Position, Color, TextScale);
    }
}
