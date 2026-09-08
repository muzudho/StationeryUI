namespace StationeryUI.MonoGame.Controls.Shared.Underline;

using Microsoft.Xna.Framework;
using StationeryUI.MonoGame;

/// <summary>
/// 下線
/// </summary>
public interface IUnderline
{
    /// <summary>
    /// 位置とサイズ
    /// </summary>
    Rectangle ContentBounds { get; set; }

    /// <summary>
    /// 上からのオフセット
    /// </summary>
    int TopOffset { get; set; }

    /// <summary>
    /// 太さ
    /// </summary>
    int Thickness { get; set; }

    /// <summary>
    /// 色
    /// </summary>
    Color Color { get; set; }

    /// <summary>
    /// 描画
    /// </summary>
    /// <param name="surface">描画先</param>
    void Draw(StationeryDrawingTools surface);
}
