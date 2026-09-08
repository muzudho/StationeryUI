namespace StationeryUI.MonoGame.Controls.Shared.Underline;

using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame;

/// <summary>
/// 下線
/// </summary>
public abstract class AbstractUnderline : IUnderline
{
    /// <summary>
    /// 位置とサイズ
    /// </summary>
    public Rectangle ContentBounds { get; set; }

    /// <summary>
    /// 上からのオフセット
    /// </summary>
    public int TopOffset { get; set; }

    /// <summary>
    /// 太さ
    /// </summary>
    public int Thickness { get; set; } = 1;

    /// <summary>
    /// 色
    /// </summary>
    public Color Color { get; set; } = Color.White;

    protected Rectangle UnderlineBounds => new(
        ContentBounds.X,
        ContentBounds.Bottom + TopOffset,
        ContentBounds.Width,
        Thickness);

    /// <summary>
    /// 描画
    /// </summary>
    /// <param name="surface">描画先</param>
    public void Draw(StationeryDrawingTools surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        DrawCore(surface);
    }

    protected abstract void DrawCore(StationeryDrawingTools surface);
}
