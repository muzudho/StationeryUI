namespace StationeryUI.MonoGame.SpinButton;

using StationeryUI.MonoGame.Controls.Button;
using Microsoft.Xna.Framework;
using System;

/// <summary>上三角・ステップ数値・下三角を縦にまとめた数値調整コントロールです。</summary>
public sealed class SpinButton
{
    private const int ButtonHeight = 36;
    private const int StepHeight = 28;

    public SpinButton(Rectangle bounds, string stepValue)
    {
        Bounds = bounds;
        StepValue = stepValue ?? throw new ArgumentNullException(nameof(stepValue));
        UpButton = new Button(new Rectangle(bounds.X, bounds.Y, bounds.Width, ButtonHeight), "▲", 0.38f);
        StepValueBounds = new Rectangle(bounds.X, bounds.Y + ButtonHeight, bounds.Width, StepHeight);
        DownButton = new Button(new Rectangle(bounds.X, StepValueBounds.Bottom, bounds.Width, ButtonHeight), "▼", 0.38f);
    }

    public Rectangle Bounds { get; }
    public Button UpButton { get; }
    public Button DownButton { get; }
    public Rectangle StepValueBounds { get; }
    public string StepValue { get; private set; }

    public void SetStepValue(string stepValue) => StepValue = stepValue ?? throw new ArgumentNullException(nameof(stepValue));

    public void Draw(Point mousePoint, SpinButtonDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        DrawTriangle(UpButton.Bounds, pointingUp: true, UpButton.IsHit(mousePoint), draw.DrawLine);
        draw.DrawCenteredText(StepValue, StepValueBounds, new Color(180, 195, 195), 0.30f);
        DrawTriangle(DownButton.Bounds, pointingUp: false, DownButton.IsHit(mousePoint), draw.DrawLine);
    }

    private static void DrawTriangle(Rectangle bounds, bool pointingUp, bool hovered, Action<Vector2, Vector2, float, Color> drawLine)
    {
        var color = hovered ? new Color(185, 196, 255) : new Color(118, 139, 143);
        var centerX = bounds.Center.X;
        var halfWidth = Math.Max(8, bounds.Width / 4);
        var halfHeight = Math.Max(6, bounds.Height / 4);
        var tip = pointingUp
            ? new Vector2(centerX, bounds.Center.Y - halfHeight)
            : new Vector2(centerX, bounds.Center.Y + halfHeight);
        var left = pointingUp
            ? new Vector2(centerX - halfWidth, bounds.Center.Y + halfHeight)
            : new Vector2(centerX - halfWidth, bounds.Center.Y - halfHeight);
        var right = pointingUp
            ? new Vector2(centerX + halfWidth, bounds.Center.Y + halfHeight)
            : new Vector2(centerX + halfWidth, bounds.Center.Y - halfHeight);
        drawLine(tip, left, 2f, color);
        drawLine(left, right, 2f, color);
        drawLine(right, tip, 2f, color);
    }
}

public sealed record SpinButtonDrawingCallbacks(
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<string, Rectangle, Color, float> DrawCenteredText);
