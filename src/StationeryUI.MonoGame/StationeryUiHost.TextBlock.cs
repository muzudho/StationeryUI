namespace StationeryUI.MonoGame;

using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Theming;
using Microsoft.Xna.Framework.Input;
using System.Globalization;

public sealed partial class StationeryUiHost
{
    /// <summary>Read-only, wrapping text with wheel/keyboard scrolling and Ctrl+C to copy its contents.</summary>
    public Element AddTextBlock(StationeryNode node, ScreenRectangle bounds, string text)
    {
        ValidateNode(node, "textBlock");
        var element = new Element(node, bounds, text);
        Focus.Register(element.Path); elements.Add(element); return element;
    }

    private List<string> TextBlockLines(Element element, StationeryTheme theme)
    {
        var width = Math.Max(1, element.Bounds.Width - theme.Padding * 2 - 17 / Viewport.Scale);
        if (element.WrappedText == element.Label && element.WrapWidth == width && element.WrapTheme == theme) return element.WrappedLines;
        var lines = new List<string>();
        foreach (var paragraph in element.Label.Replace("\r", "").Split('\n'))
        {
            var line = "";
            var characters = StringInfo.GetTextElementEnumerator(paragraph);
            while (characters.MoveNext())
            {
                var character = characters.GetTextElement();
                if (line.Length > 0 && Measure(line + character, theme) > width) { lines.Add(line); line = ""; }
                line += character;
            }
            lines.Add(line);
        }
        element.WrappedText = element.Label; element.WrapWidth = width; element.WrapTheme = theme; element.WrappedLines = lines;
        return lines;
    }
    private void UpdateTextBlock(Element element, ScreenPoint pointer, bool hovered, bool pressed, bool down, int wheel, Func<Keys, bool> key, bool control)
    {
        var theme = element.Theme ?? Theme;
        var height = theme.FontSize * 1.5 + 4;
        var total = TextBlockLines(element, theme).Count * height + theme.Padding * 2;
        element.Scroll = Math.Clamp(element.Scroll, 0, Math.Max(0, total - element.Bounds.Height));
        var bar = Scrollbar(element, total, element.Scroll);
        if (Focus.CapturedId != element.Path) element.DraggingTextScroll = false;
        if (hovered && pressed && Focus.CapturedId == element.Path && Contains(bar.Track, pointer))
        {
            if (Contains(bar.Thumb, pointer))
            {
                element.DraggingTextScroll = true;
                element.TextThumbGrab = (pointer.Y - bar.Thumb.Y) / bar.Thumb.Height;
            }
            else element.Scroll += pointer.Y < bar.Thumb.Y ? -element.Bounds.Height : element.Bounds.Height;
        }
        if (element.DraggingTextScroll)
        {
            var travel = bar.Track.Height - bar.Thumb.Height;
            if (travel > 0) element.Scroll = Math.Clamp((pointer.Y - bar.Track.Y - element.TextThumbGrab * bar.Thumb.Height) / travel, 0, 1) * bar.Maximum;
            if (!down || travel <= 0) element.DraggingTextScroll = false;
            return;
        }
        if (hovered) element.Scroll -= wheel / 120.0 * height * 3;
        if (Focus.FocusedId == element.Path)
        {
            if (key(Keys.Up)) element.Scroll -= height;
            if (key(Keys.Down)) element.Scroll += height;
            if (key(Keys.PageUp)) element.Scroll -= element.Bounds.Height;
            if (key(Keys.PageDown)) element.Scroll += element.Bounds.Height;
            if (key(Keys.Home)) element.Scroll = 0;
            if (key(Keys.End)) element.Scroll = double.MaxValue;
            if (control && key(Keys.C)) input.WriteClipboard(element.Label);
        }
        element.Scroll = Math.Clamp(element.Scroll, 0, Math.Max(0, total - element.Bounds.Height));
    }
    private void DrawTextBlock(Element element, StationeryTheme theme)
    {
        var lines = TextBlockLines(element, theme);
        var height = theme.FontSize * 1.5 + 4;
        var total = lines.Count * height + theme.Padding * 2;
        element.Scroll = Math.Clamp(element.Scroll, 0, Math.Max(0, total - element.Bounds.Height));
        Fill(element.Bounds, theme.Surface);
        for (var i = 0; i < lines.Count; i++)
        {
            var y = element.Bounds.Y + theme.Padding + i * height - element.Scroll;
            if (y + height > element.Bounds.Y && y < element.Bounds.Y + element.Bounds.Height)
                DrawText(lines[i], element.Bounds.X + theme.Padding, y, theme, theme.Text);
        }
        if (total > element.Bounds.Height)
        {
            var bar = Scrollbar(element, total, element.Scroll);
            Fill(bar.Track, theme.Background);
            Fill(bar.Thumb, element.DraggingTextScroll ? theme.Accent : theme.Border);
        }
    }
}
