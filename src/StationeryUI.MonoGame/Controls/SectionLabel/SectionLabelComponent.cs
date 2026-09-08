namespace StationeryUI.MonoGame.Controls.SectionLabel;

using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame;

/// <summary>対象区画とラベル自身の位置、サイズ、表示方向を生成時から所有する文房具UIです。</summary>
public sealed class SectionLabelComponent
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　ファクトリーメソッド］
    public static SectionLabelComponent CreateVertical(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        StationeryDrawingTools drawingContext,
        int labelWidth = 38,
        int labelGap = 8)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var usesSplitHorizontalText = drawingContext.MeasureText(text).X * VerticalScale > targetSectionBounds.Height - 12;
        var actualWidth = usesSplitHorizontalText ? Math.Max(88, labelWidth * 2) : labelWidth;
        var bounds = new Rectangle(
            targetSectionBounds.X - actualWidth - labelGap,
            targetSectionBounds.Y,
            actualWidth,
            targetSectionBounds.Height);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Vertical, usesSplitHorizontalText);
    }

    public static SectionLabelComponent CreateHorizontal(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        int labelHeight = 38,
        int labelGap = 8,
        byte surfaceOpacity = 150)
    {
        var bounds = new Rectangle(
            targetSectionBounds.X,
            targetSectionBounds.Y - labelHeight - labelGap,
            targetSectionBounds.Width,
            labelHeight);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Horizontal, usesSplitHorizontalText: false, surfaceOpacity);
    }

    public static SectionLabelComponent CreateVerticalOverlay(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        StationeryDrawingTools drawingContext,
        int labelWidth = 38,
        int leftProtrusion = 4)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var usesSplitHorizontalText = drawingContext.MeasureText(text).X * VerticalScale > targetSectionBounds.Height - 12;
        var actualWidth = usesSplitHorizontalText ? Math.Max(88, labelWidth * 2) : labelWidth;
        var bounds = new Rectangle(
            targetSectionBounds.X - leftProtrusion,
            targetSectionBounds.Y,
            actualWidth,
            targetSectionBounds.Height);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Vertical, usesSplitHorizontalText);
    }

    public static SectionLabelComponent CreateVerticalAt(
        Rectangle targetSectionBounds,
        Rectangle bounds,
        string text,
        Color accentColor,
        Color textColor,
        StationeryDrawingTools drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var usesSplitHorizontalText = drawingContext.MeasureText(text).X * VerticalScale > bounds.Height - 12;
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Vertical, usesSplitHorizontalText);
    }
    #endregion

    #region ［生成　＞　コンストラクター］
    private SectionLabelComponent(
        Rectangle targetSectionBounds,
        Rectangle bounds,
        string text,
        Color accentColor,
        Color textColor,
        SectionLabelDirection direction,
        bool usesSplitHorizontalText,
        byte surfaceOpacity = 150)
    {
        TargetSectionBounds = targetSectionBounds;
        Bounds = bounds;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        AccentColor = accentColor;
        TextColor = textColor;
        Direction = direction;
        _usesSplitHorizontalText = usesSplitHorizontalText;
        _surfaceOpacity = surfaceOpacity;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    private const float VerticalScale = 0.38f;
    private readonly bool _usesSplitHorizontalText;
    private readonly byte _surfaceOpacity;

    public Rectangle TargetSectionBounds { get; }
    public Rectangle Bounds { get; }
    public string Text { get; }
    public Color AccentColor { get; }
    public Color TextColor { get; }
    public SectionLabelDirection Direction { get; }
    public bool HasVisibilityPin { get; private set; }
    public bool IsPinned { get; private set; }

    public Rectangle VisibilityPinBounds => Direction == SectionLabelDirection.Vertical
        ? new(Bounds.X, Bounds.Y, Bounds.Width, Math.Min(Bounds.Width, Bounds.Height))
        : new(Bounds.X, Bounds.Y, Math.Min(Bounds.Height, Bounds.Width), Bounds.Height);

    // ========================================
    // 機能
    // ========================================

    public void Draw(StationeryDrawingTools draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        DrawSurface(draw);
        if (Direction == SectionLabelDirection.Horizontal)
        {
            var pinInset = HasVisibilityPin ? VisibilityPinBounds.Width : 0;
            draw.DrawFittedText(Text, new Rectangle(Bounds.X + pinInset + 8, Bounds.Y + 4, Bounds.Width - pinInset - 16, Bounds.Height - 8), TextColor, 0.36f);
            DrawVisibilityPin(draw);
            return;
        }

        if (!_usesSplitHorizontalText)
        {
            var pinInset = HasVisibilityPin ? VisibilityPinBounds.Height : 0;
            var center = new Vector2(Bounds.Center.X, Bounds.Y + pinInset + (Bounds.Height - pinInset) / 2f);
            draw.DrawRotatedCenteredText(Text, center + new Vector2(2, 2), new Color(0, 0, 0, 125), VerticalScale);
            draw.DrawRotatedCenteredText(Text, center, TextColor, VerticalScale);
            DrawVisibilityPin(draw);
            return;
        }

        var (firstLine, secondLine) = Split(Text);
        var lineHeight = Math.Max(1, (Bounds.Height - 12) / 2);
        draw.DrawFittedText(firstLine, new Rectangle(Bounds.X + 6, Bounds.Y + 5, Bounds.Width - 12, lineHeight), TextColor, 0.30f);
        draw.DrawFittedText(secondLine, new Rectangle(Bounds.X + 6, Bounds.Y + 7 + lineHeight, Bounds.Width - 12, lineHeight), TextColor, 0.30f);
        DrawVisibilityPin(draw);
    }

    public void EnableVisibilityPin(bool isPinned)
    {
        HasVisibilityPin = true;
        IsPinned = isPinned;
    }

    public bool IsVisibilityPinHit(Point point) => HasVisibilityPin && VisibilityPinBounds.Contains(point);

    private void DrawVisibilityPin(StationeryDrawingTools draw)
    {
        if (!HasVisibilityPin) return;
        var pin = VisibilityPinBounds;
        draw.FillRectangle(pin, new Color(9, 17, 25, 235));
        draw.DrawRectangle(pin, 1, new Color(78, 105, 112, 220));
        var color = IsPinned ? new Color(105, 247, 232) : new Color(145, 160, 164);
        var center = new Vector2(pin.Center.X, pin.Center.Y);
        var head = new Rectangle((int)center.X - 8, (int)center.Y - 9, 16, 6);
        draw.FillRectangle(head, color);
        draw.DrawRectangle(head, 1, new Color(color, 245));
        draw.FillRectangle(new Rectangle((int)center.X - 3, (int)center.Y - 3, 6, 8), color);
        draw.DrawLine(center + new Vector2(0, 4), center + new Vector2(0, 11), 3, color);
        draw.DrawLine(center + new Vector2(-3, 8), center + new Vector2(0, 12), 2, color);
        draw.DrawLine(center + new Vector2(3, 8), center + new Vector2(0, 12), 2, color);
        if (!IsPinned)
            draw.DrawLine(center + new Vector2(-9, 9), center + new Vector2(9, -9), 3, new Color(220, 150, 145));
    }

    private void DrawSurface(StationeryDrawingTools draw)
    {
        if (_surfaceOpacity == 0) return;
        draw.FillRectangle(Bounds, new Color(AccentColor, _surfaceOpacity));
        draw.DrawRectangle(Bounds, 1, new Color(AccentColor, Math.Max(_surfaceOpacity, (byte)180)));
    }

    private static (string FirstLine, string SecondLine) Split(string title)
    {
        var splitAt = title.LastIndexOf(' ');
        if (splitAt > 0 && splitAt < title.Length - 1) return (title[..splitAt], title[(splitAt + 1)..]);
        var middle = Math.Max(1, title.Length / 2);
        return (title[..middle], title[middle..]);
    }
}

public enum SectionLabelDirection
{
    Horizontal,
    Vertical,
}
