namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using StationeryUI.Platform;
using StationeryUI.MonoGame.Controls.SectionLabel;

using System.Collections.Generic;
using System;

/// <summary>画面rendererと文房具UIを分離する共通描画境界です。</summary>
public class StationeryDrawingTools : IDisposable
{
    private readonly ScreenCanvas _canvas;
    private readonly DynamicTextRenderer _dynamicTextRenderer;

    public StationeryDrawingTools(
        ScreenCanvas canvas,
        ITextRasterizer textRasterizer)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _dynamicTextRenderer = new DynamicTextRenderer(canvas, textRasterizer);
    }

    public StationeryUI.Theming.StationeryTheme Theme { get; set; } = StationeryUI.Theming.StationeryTheme.Dark;

    public int ScreenWidth => _canvas.ScreenWidth;
    public int ScreenHeight => _canvas.ScreenHeight;
    public void FillRectangle(Rectangle bounds, Color color) => _canvas.FillRectangle(bounds, color);
    public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => _canvas.FillRoundedRectangle(bounds, radius, color);
    public void DrawRectangle(Rectangle bounds, int thickness, Color color) => _canvas.DrawRectangle(bounds, thickness, color);
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _canvas.DrawLine(start, end, thickness, color);
    public void DrawCircle(Vector2 center, float radius, Color color) => _canvas.DrawCircle(center, radius, color);

    public void DrawCircleSurface(Rectangle bounds, Color color) => _canvas.DrawCircleSurface(bounds, color);

















    public void DrawTextSelection(string text, int start, int length, Rectangle bounds, float scale)
    {
        if (length <= 0 || start < 0 || start >= text.Length) return;
        var end = Math.Min(text.Length, start + length);
        var fittedScale = GetFittedScale(text, bounds, scale);
        var startX = bounds.X + MeasureText(text[..start]).X * fittedScale;
        var endX = bounds.X + MeasureText(text[..end]).X * fittedScale;
        FillRectangle(new Rectangle((int)startX, bounds.Y + 3, Math.Max(2, (int)MathF.Ceiling(endX - startX)), bounds.Height - 6), new Color(50, 108, 139, 210));
    }

    public void DrawTextCaret(string text, int caret, Rectangle bounds, float scale)
    {
        var prefix = text[..Math.Clamp(caret, 0, text.Length)];
        var x = bounds.X + MathF.Min(bounds.Width - 2, MeasureText(prefix).X * GetFittedScale(text, bounds, scale));
        DrawLine(new Vector2(x, bounds.Y + 5), new Vector2(x, bounds.Bottom - 5), 2, new Color(147, 244, 200));
    }

    public void DrawDataRowFrame(Rectangle bounds, bool active = false, bool hovered = false)
    {
        var fill = active ? new Color(28, 41, 45) : hovered ? new Color(28, 36, 43) : new Color(21, 28, 34);
        var line = active ? new Color(104, 191, 165) : hovered ? new Color(58, 77, 85) : new Color(43, 56, 63);
        FillRectangle(bounds, fill);
        FillRectangle(new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), line);
        FillRectangle(new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), line);
        if (active) FillRectangle(new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), new Color(99, 223, 185));
    }











    public void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale) =>
        DrawDynamicText(text, bounds, color, scale);



    private float GetFittedScale(string text, Rectangle bounds, float scale)
    {
        var measured = MeasureText(text);
        return MathF.Min(scale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
    }


    public void DrawText(string text, Vector2 position, Color color, float scale) => _canvas.DrawText(text, position, color, scale);
    public void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _canvas.DrawFittedText(text, bounds, color, scale);
    public void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float scale) =>
        _canvas.DrawCenteredFittedText(text, bounds, color, scale);
    public void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _canvas.DrawRotatedCenteredText(text, center, color, scale);
    public Vector2 MeasureText(string text) => _canvas.MeasureText(text);
    public Point ToVirtualPoint(Point point) => _canvas.ToVirtualPoint(point);
    public void Begin() => _canvas.Begin();
    public void End() { _canvas.End(); _dynamicTextRenderer.EndFrame(); }


    /// <summary>
    /// 動的に内容が決まるボタンを文房具 UI の <see cref="Controls.Button.Button"/> として描画します。
    /// 固定ボタンは、画面モデルが Button インスタンスを保持して直接 Draw する方式を優先してください。
    /// </summary>
    public void DrawButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled, float scale)
    {
        var button = new Controls.Button.Button(bounds, label, scale)
        {
            IsSelected = selected,
            IsEnabled = enabled,
        };
        button.Draw(mousePoint, this);
    }

    public void DrawDynamicText(string text, Rectangle bounds, Color color, float scale) =>
        _dynamicTextRenderer.Draw(text, bounds, color, scale);

    public void DrawSelectionFinger(Vector2 origin, float scale)
    {
        var color = new Color(125, 225, 255);
        var thickness = 2f * scale;
        var points = new[]
        {
            origin + new Vector2(0, 2) * scale, origin + new Vector2(5, 2) * scale,
            origin + new Vector2(7, -3) * scale, origin + new Vector2(9, -3) * scale,
            origin + new Vector2(10, 0) * scale, origin + new Vector2(21, 0) * scale,
            origin + new Vector2(24, 3) * scale, origin + new Vector2(21, 6) * scale,
            origin + new Vector2(12, 6) * scale, origin + new Vector2(15, 9) * scale,
            origin + new Vector2(13, 12) * scale, origin + new Vector2(10, 10) * scale,
            origin + new Vector2(11, 14) * scale, origin + new Vector2(8, 16) * scale,
            origin + new Vector2(5, 12) * scale, origin + new Vector2(0, 10) * scale,
            origin + new Vector2(0, 2) * scale,
        };
        for (var index = 1; index < points.Length; index++)
            DrawLine(points[index - 1], points[index], thickness, color);
    }



    public int GetTextCaretIndex(int pointX, string text, Rectangle bounds, float scale)
    {
        if (string.IsNullOrEmpty(text) || pointX <= bounds.X) return 0;
        var fittedScale = GetFittedScale(text, bounds, scale);
        var previousX = (float)bounds.X;
        for (var index = 0; index < text.Length; index++)
        {
            var nextX = bounds.X + MathF.Min(bounds.Width - 2, MeasureText(text[..(index + 1)]).X * fittedScale);
            if (pointX < (previousX + nextX) * 0.5f) return index;
            previousX = nextX;
        }
        return text.Length;
    }

    public void Dispose() => _dynamicTextRenderer.Dispose();
}
