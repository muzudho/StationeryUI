namespace StationeryUI.MonoGame.Controls.MultilineTextUnderline;

using StationeryUI.MonoGame.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame.Controls.ActionBadge;
using StationeryUI.MonoGame;

/// <summary>複数行テキスト入力欄の罫線を描画する独立コンポーネントです。</summary>
public sealed class MultilineTextUnderline
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    public MultilineTextUnderline(IUnderline underline, string? actionBadgeLabel = null, float actionBadgeTextScale = 0.34f)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
        _actionBadgeLabel = actionBadgeLabel;
        _actionBadgeTextScale = actionBadgeTextScale;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    private readonly string? _actionBadgeLabel;
    private readonly float _actionBadgeTextScale;
    private Rectangle _bounds;

    public IUnderline Underline { get; }

    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            ActionBadge = _actionBadgeLabel is null ? null : ActionBadgeComponent.Create(_actionBadgeLabel, value, _actionBadgeTextScale);
        }
    }

    public bool IsEditing { get; private set; }

    public bool IsHovered { get; private set; }

    public ActionBadgeComponent? ActionBadge { get; private set; }
    public int LineHeight { get; set; } = 31;
    public int BaselineOffset { get; set; } = 26;
    public int HorizontalInset { get; set; } = 18;
    public int TextTopInset { get; set; } = 18;

    // ========================================
    // 機能
    // ========================================

    public void SetEditing(bool isEditing) => IsEditing = isEditing;

    public void UpdatePointer(Point point)
    {
        IsHovered = Bounds.Contains(point);
        if (IsHovered)
            ActionBadge?.Show();
        else
            ActionBadge?.Hide();
    }

    public void Draw(StationeryDrawingTools surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var underlineColor = IsEditing ? new Color(147, 244, 200, 180) : IsHovered ? new Color(185, 196, 255, 180) : new Color(99, 223, 185, 180);
        var firstBaselineY = Bounds.Y + TextTopInset + BaselineOffset;
        for (var y = firstBaselineY; y < Bounds.Bottom - TextTopInset; y += LineHeight)
        {
            Underline.ContentBounds = new Rectangle(Bounds.X + HorizontalInset, y, Bounds.Width - HorizontalInset * 2, 0);
            Underline.TopOffset = 0;
            Underline.Color = underlineColor;
            Underline.Draw(surface);
        }
        ActionBadge?.Draw(surface);
    }
}
