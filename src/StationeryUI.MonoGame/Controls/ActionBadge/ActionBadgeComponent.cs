namespace StationeryUI.MonoGame.Controls.ActionBadge;

using StationeryUI.MonoGame;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// アンダーライン項目にホバーしたとき、右端へ表示するアクション名のバッジです。
/// </summary>
public sealed class ActionBadgeComponent
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　ファクトリーメソッド］
    /// <summary>項目の右端に合わせた標準位置でバッジを表示します。</summary>
    public static ActionBadgeComponent Create(string label, Rectangle anchorBounds, float textScale = 0.34f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new ActionBadgeComponent(label, anchorBounds, textScale);
    }
    #endregion

    #region ［生成　＞　コンストラクター］
    private ActionBadgeComponent(string label, Rectangle anchorBounds, float textScale)
    {
        Label = label;
        AnchorBounds = anchorBounds;
        Bounds = CalculateBounds(label, anchorBounds);
        TextScale = textScale;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    public string Label { get; }

    public Rectangle AnchorBounds { get; private set; }

    public Rectangle Bounds { get; private set; }

    public bool IsVisible { get; private set; }

    /// <summary>バッジ内ラベルの文字倍率です。</summary>
    public float TextScale { get; }

    // ========================================
    // 機能
    // ========================================

    public void Show() => IsVisible = true;

    public void Hide() => IsVisible = false;

    /// <summary>アンカー領域に合わせてバッジを再配置します。</summary>
    public void SetAnchorBounds(Rectangle anchorBounds)
    {
        AnchorBounds = anchorBounds;
        Bounds = CalculateBounds(Label, anchorBounds);
    }

    public void Draw(StationeryDrawingTools drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        if (!IsVisible) return;

        drawingContext.FillRoundedRectangle(Bounds, 6, new Color(185, 196, 255));
        drawingContext.DrawCenteredFittedText(Label, Bounds, new Color(15, 20, 31), TextScale);
    }

    /// <summary>標準バッジの大きさと、アンダーライン右端に対する配置を返します。</summary>
    private static Rectangle CalculateBounds(string label, Rectangle anchorBounds)
    {
        var width = label switch
        {
            "EDIT" => 70,
            "SELECT" or "TOGGLE" => 88,
            _ => 100,
        };
        var height = label == "EDIT" ? 23 : 26;
        var rightMargin = label == "EDIT" ? 6 : 8;
        var bottomMargin = label == "EDIT" ? 2 : 2;
        return new Rectangle(anchorBounds.Right - width - rightMargin, anchorBounds.Bottom - height - bottomMargin, width, height);
    }
}
