namespace StationeryUI.MonoGame.Controls.PopupTimeUnderline;

using StationeryUI.MonoGame;

using StationeryUI.MonoGame.Controls.Button;
using StationeryUI.MonoGame.SpinButton;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>時・分・秒を 2 桁ずつ入力する、タイム用ポップアップです。</summary>
public sealed class PopupTimeUnderline
{
    public void Draw(StationeryDrawingTools drawingContext, Point mousePosition, string[] values,
        int[] carets, int activePart, string message)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        Draw(mousePoint, values, carets, activePart, message,
            new PopupTimeUnderlineDrawingCallbacks(
                drawingContext.ScreenWidth, drawingContext.ScreenHeight,
                drawingContext.FillRectangle, drawingContext.DrawRectangle,
                drawingContext.DrawText, drawingContext.DrawFittedText,
                value => drawingContext.MeasureText(value).X, drawingContext,
                drawingContext.DrawLine, drawingContext.DrawCenteredFittedText));
        drawingContext.End();
    }

    public int GetCaretIndex(StationeryDrawingTools drawingContext, int part, Point point, string text) =>
        GetCaretIndex(part, point, text, drawingContext.GetTextCaretIndex);

    private static readonly Rectangle DialogBounds = new(610, 300, 700, 390);
    private static readonly Rectangle[] ValueBounds =
    [
        new(690, 410, 150, 70), new(885, 410, 150, 70), new(1080, 410, 150, 70),
    ];

    public Button CancelButton { get; } = new(new Rectangle(DialogBounds.Right - 360, DialogBounds.Y + 22, 150, 54), "CANCEL", 0.34f);
    public Button OkButton { get; } = new(new Rectangle(DialogBounds.Right - 190, DialogBounds.Y + 22, 150, 54), "OK", 0.42f);
    public IReadOnlyList<SpinButton> SpinButtons { get; } =
    [
        new SpinButton(new Rectangle(724, 516, 82, 100), "h"),
        new SpinButton(new Rectangle(919, 516, 82, 100), "m"),
        new SpinButton(new Rectangle(1114, 516, 82, 100), "s"),
    ];

    public bool IsTextBoxHit(Point point, out int part)
    {
        for (part = 0; part < ValueBounds.Length; part++)
            if (ValueBounds[part].Contains(point)) return true;
        part = -1;
        return false;
    }

    public int GetCaretIndex(int part, Point point, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, ContentBounds(part), 0.55f);

    public void Draw(Point mousePoint, string[] values, int[] carets, int activePart, string message, PopupTimeUnderlineDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(carets);
        ArgumentNullException.ThrowIfNull(draw);
        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 130));
        draw.FillRectangle(new Rectangle(DialogBounds.X + 14, DialogBounds.Y + 16, DialogBounds.Width, DialogBounds.Height), new Color(0, 0, 0, 155));
        draw.FillRectangle(DialogBounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(DialogBounds, 2, new Color(116, 145, 146));
        draw.DrawText("TIME INPUT", new Vector2(DialogBounds.X + 34, DialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);

        for (var index = 0; index < 3; index++)
        {
            var bounds = ValueBounds[index];
            var text = values[index] ?? string.Empty;
            draw.FillRectangle(bounds, new Color(15, 20, 26));
            draw.DrawRectangle(bounds, 2, index == activePart ? new Color(99, 223, 185) : new Color(100, 110, 145));
            draw.DrawFittedText(text, ContentBounds(index), Color.White, 0.55f);
            if (index == activePart)
            {
                var prefix = text[..Math.Clamp(carets[index], 0, text.Length)];
                var caretX = ContentBounds(index).X + (int)(draw.MeasureTextWidth(prefix) * 0.55f);
                draw.FillRectangle(new Rectangle(Math.Min(caretX, bounds.Right - 24), bounds.Y + 14, 2, 42), new Color(147, 244, 200));
            }
            if (index < 2) draw.DrawText(":", new Vector2(bounds.Right + 18, bounds.Y + 18), new Color(180, 195, 195), 0.55f);
        }

        foreach (var spinButton in SpinButtons)
            spinButton.Draw(mousePoint, new SpinButtonDrawingCallbacks(draw.DrawLine, draw.DrawCenteredText));
        draw.DrawFittedText(message ?? string.Empty, new Rectangle(DialogBounds.X + 80, 642, DialogBounds.Width - 160, 28), new Color(255, 205, 140), 0.32f);
        CancelButton.Draw(mousePoint, draw.ButtonSurface);
        OkButton.Draw(mousePoint, draw.ButtonSurface);
    }

    private static Rectangle ContentBounds(int part) => new(ValueBounds[part].X + 22, ValueBounds[part].Y + 12, ValueBounds[part].Width - 44, 46);
}

public sealed record PopupTimeUnderlineDrawingCallbacks(
    int VirtualScreenWidth, int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle, Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText, Action<string, Rectangle, Color, float> DrawFittedText,
    Func<string, float> MeasureTextWidth, StationeryDrawingTools ButtonSurface,
    Action<Vector2, Vector2, float, Color> DrawLine, Action<string, Rectangle, Color, float> DrawCenteredText);
