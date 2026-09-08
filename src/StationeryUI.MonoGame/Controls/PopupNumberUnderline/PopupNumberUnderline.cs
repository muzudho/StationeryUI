namespace StationeryUI.MonoGame.Controls.PopupNumberUnderline;

using StationeryUI.MonoGame;

using StationeryUI.MonoGame.Controls.Button;
using StationeryUI.MonoGame.SpinButton;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>リンクから開く整数入力用のモーダル画面です。</summary>
public sealed class PopupNumberUnderline
{
    public void Draw(StationeryDrawingTools drawingContext, Point mousePosition, string title, string text,
        int caretIndex, int selectionStart, int selectionLength, string message,
        PopupNumberUnderlineOptions options = default)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message,
            new PopupNumberUnderlineDrawingCallbacks(
                drawingContext.ScreenWidth, drawingContext.ScreenHeight,
                drawingContext.FillRectangle, drawingContext.DrawRectangle,
                drawingContext.DrawText, drawingContext.DrawFittedText,
                drawingContext.DrawTextSelection, value => drawingContext.MeasureText(value).X,
                drawingContext, drawingContext.DrawLine, drawingContext.DrawCenteredFittedText), options);
        drawingContext.End();
    }

    public int GetCaretIndex(StationeryDrawingTools drawingContext, Point point, string text) =>
        GetCaretIndex(point, text, drawingContext.GetTextCaretIndex);

    #region Layout

    private static readonly Rectangle DialogBounds = new(610, 300, 700, 390);
    private static readonly Rectangle TextBounds = new(690, 430, 540, 60);
    private static readonly Rectangle TextContentBounds = new(TextBounds.X + 12, TextBounds.Y + 6, TextBounds.Width - 24, 42);

    #endregion

    #region Buttons

    /// <summary>入力を取り消します。</summary>
    public Button CancelButton { get; } = new(new Rectangle(DialogBounds.Right - 360, DialogBounds.Y + 22, 150, 54), "CANCEL", 0.34f);

    /// <summary>入力した数値を確定します。</summary>
    public Button OkButton { get; } = new(new Rectangle(DialogBounds.Right - 190, DialogBounds.Y + 22, 150, 54), "OK", 0.42f);

    public Button ClearButton { get; } = new(new Rectangle(DialogBounds.Right - 530, DialogBounds.Y + 22, 150, 54), "CLEAR", 0.32f);

    /// <summary>
    /// 下げるボタン
    /// </summary>
    private readonly List<SpinButton> _spinButtons =
    [
        new SpinButton(new Rectangle(700, 516, 82, 100), "1"),
    ];

    /// <summary>この数値入力画面に配置するスピンボタンです。</summary>
    public IReadOnlyList<SpinButton> SpinButtons => _spinButtons;

    /// <summary>スピンボタンを 0 個以上、任意の数だけ配置します。</summary>
    public void SetSpinButtons(IEnumerable<SpinButton> spinButtons)
    {
        ArgumentNullException.ThrowIfNull(spinButtons);
        _spinButtons.Clear();
        _spinButtons.AddRange(spinButtons);
    }

    /// <summary>後方互換のため、先頭スピンボタンの下向きボタンを公開します。</summary>
    public Button? StepDownButton => _spinButtons.Count == 0 ? null : _spinButtons[0].DownButton;

    /// <summary>
    /// 上げるボタン
    /// </summary>
    /// <summary>後方互換のため、先頭スピンボタンの上向きボタンを公開します。</summary>
    public Button? StepUpButton => _spinButtons.Count == 0 ? null : _spinButtons[0].UpButton;

    #endregion

    #region Hit testing and caret

    public bool IsTextBoxHit(Point point) => TextBounds.Contains(point);

    public int GetCaretIndex(Point point, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, TextContentBounds, 0.55f);

    #endregion

    #region Drawing

    /// <summary>整数入力ダイアログを描画します。</summary>
    public void Draw(
        Point mousePoint,
        string title,
        string text,
        int caretIndex,
        int selectionStart,
        int selectionLength,
        string message,
        PopupNumberUnderlineDrawingCallbacks draw,
        PopupNumberUnderlineOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(draw);
        title ??= string.Empty;
        text ??= string.Empty;
        message ??= string.Empty;
        if (options.SpinButtons is not null)
            SetSpinButtons(options.SpinButtons);

        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 130));
        draw.FillRectangle(new Rectangle(DialogBounds.X + 14, DialogBounds.Y + 16, DialogBounds.Width, DialogBounds.Height), new Color(0, 0, 0, 155));
        draw.FillRectangle(DialogBounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(DialogBounds, 2, new Color(116, 145, 146));
        draw.DrawText(options.Caption ?? "NUMBER INPUT", new Vector2(DialogBounds.X + 34, DialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        if (options.ShowTitle)
            draw.DrawFittedText(title, new Rectangle(DialogBounds.X + 36, DialogBounds.Y + 88, DialogBounds.Width - 72, 30), new Color(180, 195, 195), 0.38f);

        draw.FillRectangle(new Rectangle(TextBounds.X, TextBounds.Bottom - 4, TextBounds.Width, 4), new Color(99, 223, 185));
        draw.DrawTextSelection(text, selectionStart, selectionLength, TextContentBounds, 0.55f);
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? " " : text, TextContentBounds, Color.White, 0.55f);
        var prefix = text[..Math.Clamp(caretIndex, 0, text.Length)];
        var caretX = TextContentBounds.X + (int)(draw.MeasureTextWidth(prefix) * 0.55f);
        draw.FillRectangle(new Rectangle(Math.Min(caretX, TextBounds.Right - 14), TextBounds.Y + 7, 2, 40), new Color(147, 244, 200));

        draw.DrawFittedText(message, new Rectangle(DialogBounds.X + 80, 642, DialogBounds.Width - 160, 28), new Color(255, 205, 140), 0.32f);

        // ステップアップ・ステップダウンボタンの描画
        if (options.ShowStepControls)
        {
            if (options.SpinButtons is null && _spinButtons.Count > 0)
                _spinButtons[0].SetStepValue(options.StepLabel ?? "1");
            foreach (var spinButton in SpinButtons)
                spinButton.Draw(mousePoint, new SpinButtonDrawingCallbacks(draw.DrawLine, draw.DrawCenteredText));
        }
        CancelButton.Draw(mousePoint, draw.ButtonSurface);
        OkButton.Draw(mousePoint, draw.ButtonSurface);
        if (options.ShowClearButton) ClearButton.Draw(mousePoint, draw.ButtonSurface);
    }

    #endregion
}

/// <summary>
/// PopupNumberUnderline の描画オプションです。
/// </summary>
/// <param name="ShowStepControls"></param>
/// <param name="StepLabel"></param>
/// <param name="Caption"></param>
public readonly record struct PopupNumberUnderlineOptions(
    bool ShowStepControls = false,
    string? StepLabel = null,
    string? Caption = null,
    bool ShowTitle = true,
    IReadOnlyList<SpinButton>? SpinButtons = null,
    bool AllowEmpty = false,
    bool ShowClearButton = false);

/// <summary>PopupNumberUnderline に渡す描画機能です。</summary>
public sealed record PopupNumberUnderlineDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<string, int, int, Rectangle, float> DrawTextSelection,
    Func<string, float> MeasureTextWidth,
    StationeryDrawingTools ButtonSurface,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<string, Rectangle, Color, float> DrawCenteredText);
