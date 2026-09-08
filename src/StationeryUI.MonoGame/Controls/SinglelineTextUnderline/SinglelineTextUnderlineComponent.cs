namespace StationeryUI.MonoGame.Controls.SinglelineTextUnderline;

using StationeryUI.MonoGame.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame.Controls.ActionBadge;
using StationeryUI.MonoGame;

/// <summary>単一行テキスト入力のアンダーライン表示を担当します。</summary>
public sealed class SinglelineTextUnderline
{
    private readonly string? _actionBadgeLabel;
    private readonly float _actionBadgeTextScale;
    private Rectangle _bounds;

    public SinglelineTextUnderline(IUnderline underline, string? actionBadgeLabel = null, float actionBadgeTextScale = 0.34f)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
        _actionBadgeLabel = actionBadgeLabel;
        _actionBadgeTextScale = actionBadgeTextScale;
    }

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
        Underline.ContentBounds = Bounds;
        Underline.Color = IsEditing
            ? new Color(147, 244, 200)
            : IsHovered ? new Color(185, 196, 255) : new Color(100, 110, 145);
        Underline.Draw(surface);
        ActionBadge?.Draw(surface);
    }
}
