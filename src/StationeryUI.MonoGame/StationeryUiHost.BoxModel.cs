namespace StationeryUI.MonoGame;

using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Styling;
using StationeryUI.Theming;

public sealed partial class StationeryUiHost
{
    private static double BoxModelHeight(Element element) => element.BoxModel is null ? 0 : 250;

    // A schematic: labels show configured px, independent of the illustrative band widths.
    // It belongs to the text block's scrollable content and shares its scissor rectangle.
    private void DrawBoxModel(Element element, StationeryTheme theme)
    {
        if (element.BoxModel is not { } model) return;
        var width = Math.Max(0, element.Bounds.Width - theme.Padding * 2 - 17 / Viewport.Scale);
        if (width <= 0) return;
        var x = element.Bounds.X + theme.Padding;
        var y = element.Bounds.Y + theme.Padding - element.Scroll;
        var font = theme with { FontSize = Math.Max(8, Math.Min(14, (int)(width / 26))) };
        void Center(string text, double cx, double cy) => DrawText(text, cx - Measure(text, font) / 2, cy, font, theme.Text);
        void Band(ScreenRectangle area, string name, ViewportPadding edges, bool outer)
        {
            Fill(area, outer ? theme.Hover : theme.Background);
            DrawButtonOutline(area, 1, theme.Border);
            Center(name + "  top: " + Px(edges.Top), area.X + area.Width / 2, area.Y + 4);
            Center("bottom: " + Px(edges.Bottom), area.X + area.Width / 2, area.Y + area.Height - 24);
            var side = width * .09;
            Center("left", area.X + side, area.Y + area.Height / 2 - 18);
            Center(Px(edges.Left), area.X + side, area.Y + area.Height / 2);
            Center("right", area.X + area.Width - side, area.Y + area.Height / 2 - 18);
            Center(Px(edges.Right), area.X + area.Width - side, area.Y + area.Height / 2);
        }
        Band(new(x, y, width, 218), "margin", model.Margin, true);
        var padding = new ScreenRectangle(x + width * .18, y + 36, width * .64, 146);
        Band(padding, "padding", model.Padding, false);
        DrawBorderInside(padding, model.Border, theme.Border);
        var content = new ScreenRectangle(x + width * .36, y + 76, width * .28, 66);
        Fill(content, theme.Surface);
        DrawButtonOutline(content, 1, new ButtonColor(80, 200, 255));
        Center("content", x + width / 2, y + 99);
        Center("px / schematic", x + width / 2, y + 224);
    }

    // Border is painted over the padding band and grows inward when it is
    // thicker than the available padding. It never expands into the margin.
    private void DrawBorderInside(ScreenRectangle area, ViewportPadding border, ButtonColor color)
    {
        var top = Math.Clamp(border.Top, 0, area.Height);
        var right = Math.Clamp(border.Right, 0, area.Width);
        var bottom = Math.Clamp(border.Bottom, 0, area.Height);
        var left = Math.Clamp(border.Left, 0, area.Width);
        if (top > 0) Fill(new(area.X, area.Y, area.Width, top), color);
        if (bottom > 0) Fill(new(area.X, area.Y + area.Height - bottom, area.Width, bottom), color);
        if (left > 0) Fill(new(area.X, area.Y, left, area.Height), color);
        if (right > 0) Fill(new(area.X + area.Width - right, area.Y, right, area.Height), color);
    }

    private static string Px(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "px";
}
