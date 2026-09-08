namespace StationeryUI.MonoGame.Controls.Button;

using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame;

/// <summary>位置、ラベル、有効状態、ヒット判定を所有する文房具 UI のボタンです。</summary>
public sealed class Button
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    public Button(Rectangle bounds, string label, float labelScale)
    {
        Bounds = bounds;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        LabelScale = labelScale;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    public Rectangle Bounds { get; set; }
    public string Label { get; set; }
    public float LabelScale { get; set; }
    public bool AutoExpandLabel { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsPointerOver { get; private set; }
    public bool IsSelected { get; set; }
    private Color? fillColor;
    private Color? hoverColor;
    public StationeryUI.Theming.StationeryTheme? Theme { get; set; }
    public bool IsFocused { get; set; }
    public bool IsPressed { get; set; }
    public string AccessibleName => Label;
    public Color FillColor { get => fillColor ?? new Color(36, 48, 58); set => fillColor = value; }
    public Color PointerOverFillColor { get => hoverColor ?? new Color(58, 82, 94); set => hoverColor = value; }

    public bool IsHit(Point point) => IsEnabled && Bounds.Contains(point);

    public void UpdatePointer(Point point) => IsPointerOver = IsHit(point);

    // ========================================
    // 機能
    // ========================================

    public void Draw(Point mousePoint, StationeryDrawingTools surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        UpdatePointer(mousePoint);
        var theme = Theme ?? surface.Theme;
        static Color Convert(StationeryUI.Controls.ButtonColor c) => new(c.R, c.G, c.B, c.A);
        var fill = Convert(theme.ButtonFill(IsEnabled, IsPressed, IsSelected, IsPointerOver));
        if (IsEnabled && !IsSelected && !IsPressed)
            fill = IsPointerOver ? hoverColor ?? fill : fillColor ?? fill;
        var border = Convert(IsFocused || IsSelected ? theme.Accent : theme.Border);
        surface.FillRectangle(new Rectangle(Bounds.X + theme.ShadowOffset, Bounds.Y + theme.ShadowOffset, Bounds.Width, Bounds.Height), Convert(theme.Shadow));
        surface.FillRoundedRectangle(Bounds, Math.Max(0, theme.CornerRadius), fill);
        surface.DrawRectangle(Bounds, theme.BorderWidth, border);

        var readableScale = Math.Clamp(Bounds.Height / 140f, 0.30f, 0.46f);
        var requestedScale = AutoExpandLabel ? Math.Max(LabelScale, readableScale) : LabelScale;
        surface.DrawCenteredFittedText(
            Label,
            new Rectangle(Bounds.X + theme.Padding, Bounds.Y + 5, Math.Max(1, Bounds.Width - theme.Padding * 2), Bounds.Height - 10),
            Convert(IsEnabled ? theme.Text : theme.DisabledText),
            requestedScale);
    }
}
