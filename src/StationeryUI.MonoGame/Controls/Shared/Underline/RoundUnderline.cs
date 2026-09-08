namespace StationeryUI.MonoGame.Controls.Shared.Underline;

using StationeryUI.MonoGame;

/// <summary>
/// 角丸の下線
/// </summary>
public sealed class RoundUnderline : AbstractUnderline
{
    /// <summary>
    /// 角丸の半径
    /// </summary>
    public int Radius { get; set; } = 2;

    protected override void DrawCore(StationeryDrawingTools surface) =>
        surface.FillRoundedRectangle(UnderlineBounds, Radius, Color);
}
