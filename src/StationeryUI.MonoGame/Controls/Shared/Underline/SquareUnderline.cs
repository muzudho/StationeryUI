namespace StationeryUI.MonoGame.Controls.Shared.Underline;

using StationeryUI.MonoGame;

/// <summary>
/// 角が四角の下線
/// </summary>
public sealed class SquareUnderline : AbstractUnderline
{
    protected override void DrawCore(StationeryDrawingTools surface) =>
        surface.FillRectangle(UnderlineBounds, Color);
}
