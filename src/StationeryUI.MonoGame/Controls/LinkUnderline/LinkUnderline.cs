namespace StationeryUI.MonoGame.Controls.LinkUnderline;

using Microsoft.Xna.Framework;
using StationeryUI.MonoGame.Controls.Shared.Underline;
using System;
using StationeryUI.MonoGame.Controls.ActionBadge;
using StationeryUI.MonoGame;

/// <summary>
/// 非同期アクションへ接続する文房具 UI のリンクアンダーラインです。
/// 描画方法は共通UI道具を利用するため、画面固有Rendererには依存しません。
/// </summary>
public sealed class LinkUnderline
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    public LinkUnderline(IUnderline underline)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    /// <summary>
    /// 下線
    /// </summary>
    public IUnderline Underline { get; }

    /// <summary>このリンク下線が表示・クリック判定に使う領域です。</summary>
    public Rectangle Bounds { get; set; }

    /// <summary>実値が空のときだけ表示する文字列です。実値には保存されません。</summary>
    public string Placeholder { get; set; } = string.Empty;

    public string GetDisplayText(string? value) =>
        string.IsNullOrEmpty(value) ? Placeholder : value;

    /// <summary>最後に更新されたポインター位置が領域内にあるかを示します。</summary>
    public bool IsHovered { get; private set; }

    /// <summary>このリンクが選択状態かを示します。</summary>
    public bool IsSelected { get; private set; }

    public Color SelectedColor { get; set; } = new(147, 244, 200);

    /// <summary>このリンクをホバーしたときに表示するアクションバッジです。</summary>
    public ActionBadgeComponent? ActionBadge { get; private set; }

    // ========================================
    // 機能
    // ========================================

    /// <summary>このリンクが所有するアクションバッジを設定します。</summary>
    public void SetActionBadge(ActionBadgeComponent actionBadge) => ActionBadge = actionBadge ?? throw new ArgumentNullException(nameof(actionBadge));

    /// <summary>指定座標がこのリンク領域内か判定します。</summary>
    public bool IsHit(Point point) => Bounds.Contains(point);

    /// <summary>ポインター位置からホバー状態を更新します。</summary>
    public void UpdatePointer(Point point)
    {
        IsHovered = IsHit(point);
        if (IsHovered)
            ActionBadge?.Show();
        else
            ActionBadge?.Hide();
    }

    /// <summary>選択状態を設定します。</summary>
    public void SetSelected(bool selected) => IsSelected = selected;

    /// <summary>指定位置がこのリンクなら選択状態にします。</summary>
    public bool TrySelect(Point point)
    {
        UpdatePointer(point);
        if (!IsHovered) return false;
        IsSelected = true;
        return true;
    }

    /// <summary>選択状態を解除します。</summary>
    public void ClearSelection() => IsSelected = false;

    /// <summary>同期リンク向けに、ホバー状態だけで所有する Underline を描画します。</summary>
    public void Draw(StationeryDrawingTools surface)
    {
        Underline.ContentBounds = Bounds;
        Underline.Color = IsSelected
            ? SelectedColor
            : IsHovered ? new Color(185, 196, 255) : new Color(100, 110, 145);
        Underline.Draw(surface);
        ActionBadge?.Draw(surface);
    }

    /// <summary>選択済みのリンクを、ホバー色より優先する色で描画します。</summary>
    /// <summary>状態に応じた色を決め、下線と必要ならスピナーを描画します。</summary>
    /// <param name="drawUnderline">日本語ラベルと下線を描画する処理です。</param>
    /// <param name="drawSpinner">指定位置にスピナーを描画する処理です。</param>
    public void Draw(
        string label,
        Point mousePoint,
        LinkUnderlineController controller,
        double nowSeconds,
        Action<Rectangle, string, Color> drawText,
        Action<Vector2, Color> drawSpinner,
        StationeryDrawingTools surface)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(drawText);
        ArgumentNullException.ThrowIfNull(drawSpinner);

        UpdatePointer(mousePoint);
        var hovered = controller.CanActivate && IsHovered;
        var underlineColor = controller.State switch
        {
            LinkUnderlineState.Executing => new Color(255, 210, 128),
            LinkUnderlineState.Failed or LinkUnderlineState.Interrupted => new Color(255, 145, 151),
            _ when hovered => new Color(185, 196, 255),
            _ => new Color(100, 110, 145),
        };
        var textColor = controller.IsExecuting ? new Color(255, 225, 160) : Color.White;

        drawText(Bounds, label, textColor);
        Underline.ContentBounds = Bounds;
        Underline.Color = underlineColor;
        Underline.Draw(surface);
        if (controller.IsSpinnerVisible(nowSeconds))
            drawSpinner(new Vector2(Bounds.Right - 14, Bounds.Center.Y), underlineColor);
    }
}
