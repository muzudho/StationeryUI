namespace StationeryUI.MonoGame.Controls;

using Microsoft.Xna.Framework;
using System;

/// <summary>アプリケーション群で共有する歯車形の設定ボタンです。</summary>
public sealed class GearButton(Rectangle bounds)
{
    public Rectangle Bounds { get; } = bounds;
    public bool IsHit(Point point) => Bounds.Contains(point);

    public void Draw(StationeryDrawingTools drawingContext, Point mousePoint)
    {
        var hovered = Bounds.Contains(mousePoint);
        drawingContext.FillRectangle(Bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        drawingContext.DrawRectangle(Bounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var center = new Vector2(Bounds.Center.X, Bounds.Center.Y);
        var color = hovered ? new Color(99, 223, 185) : new Color(180, 195, 195);
        drawingContext.DrawCircle(center, 16, color);
        drawingContext.DrawCircle(center, 7, new Color(24, 31, 37));
        for (var index = 0; index < 8; index++)
        {
            var angle = MathHelper.TwoPi * index / 8f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            drawingContext.DrawLine(center + direction * 15, center + direction * 24, 6, color);
        }
    }
}
