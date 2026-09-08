namespace StationeryUI.Controls;

using StationeryUI.Canvas;

public readonly record struct ButtonColor(byte R, byte G, byte B, byte A = 255);

/// <summary>文房具ボタンの外観を描き、ラベルや既存アイコンの描画はホストへ委譲します。</summary>
public static class StationeryButtonRenderer
{
    public static void Draw(
        IconButtonModel model,
        Action<ScreenRectangle, ButtonColor> fillRectangle,
        Action<ScreenRectangle, double, ButtonColor> drawOutline,
        Action<ScreenRectangle, ButtonColor> drawContent,
        StationeryUI.Theming.StationeryTheme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(fillRectangle);
        ArgumentNullException.ThrowIfNull(drawOutline);
        ArgumentNullException.ThrowIfNull(drawContent);

        theme = model.Theme ?? theme ?? StationeryUI.Theming.StationeryTheme.Dark;
        var offset = model.IsPressed && model.IsEnabled ? 2d : 0d;
        var bounds = model.Bounds with { X = model.Bounds.X + offset, Y = model.Bounds.Y + offset };
        var fill = theme.ButtonFill(model.IsEnabled, model.IsPressed, model.IsSelected, model.IsPointerOver);
        var border = model.IsFocused || model.IsSelected ? theme.Accent : theme.Border;

        fillRectangle(bounds with { X = bounds.X + theme.ShadowOffset, Y = bounds.Y + theme.ShadowOffset }, theme.Shadow);
        fillRectangle(bounds, fill);
        drawOutline(bounds, theme.BorderWidth, border);
        if (model.IsEnabled && bounds.Width > 4d && bounds.Height > 4d)
            drawOutline(new ScreenRectangle(bounds.X + 2d, bounds.Y + 2d, bounds.Width - 4d, bounds.Height - 4d), 1d,
                theme.Accent with { A = model.IsPointerOver ? (byte)70 : (byte)36 });

        drawContent(bounds, model.IsEnabled ? theme.Text : theme.DisabledText);
    }
}
