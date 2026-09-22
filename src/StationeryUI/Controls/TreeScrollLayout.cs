namespace StationeryUI.Controls;

using StationeryUI.Canvas;

/// <summary>Shared content viewport and scrollbar geometry for a tree.</summary>
public sealed record TreeScrollLayout(ScreenRectangle Content, ScreenRectangle HorizontalTrack, ScreenRectangle HorizontalThumb,
    ScreenRectangle VerticalTrack, ScreenRectangle VerticalThumb, double MaximumX, double MaximumY, double OffsetX, double OffsetY)
{
    public static TreeScrollLayout Create(ScreenRectangle bounds, double contentWidth, double contentHeight, double thickness, double x, double y)
    {
        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)
            || bounds.Width < 0 || bounds.Height < 0 || !double.IsFinite(contentWidth) || contentWidth < 0
            || !double.IsFinite(contentHeight) || contentHeight < 0 || !double.IsFinite(thickness) || thickness <= 0
            || !double.IsFinite(x) || !double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(bounds));
        var width = Math.Max(0, bounds.Width); var height = Math.Max(0, bounds.Height);
        var horizontal = false; var vertical = false;
        var barWidth = Math.Min(width, thickness); var barHeight = Math.Min(height, thickness);
        for (var i = 0; i < 3; i++)
        {
            horizontal = contentWidth > width - (vertical ? barWidth : 0);
            vertical = contentHeight > height - (horizontal ? barHeight : 0);
        }
        var content = bounds with { Width = Math.Max(0, width - (vertical ? barWidth : 0)), Height = Math.Max(0, height - (horizontal ? barHeight : 0)) };
        var maxX = Math.Max(0, contentWidth - content.Width); var maxY = Math.Max(0, contentHeight - content.Height);
        x = Math.Clamp(x, 0, maxX); y = Math.Clamp(y, 0, maxY);
        var hTrack = horizontal ? new ScreenRectangle(bounds.X, bounds.Y + content.Height, content.Width, barHeight) : default;
        var vTrack = vertical ? new ScreenRectangle(bounds.X + content.Width, bounds.Y, barWidth, content.Height) : default;
        var thumbWidth = horizontal ? Math.Min(hTrack.Width, Math.Max(thickness, hTrack.Width * content.Width / contentWidth)) : 0;
        var thumbHeight = vertical ? Math.Min(vTrack.Height, Math.Max(thickness, vTrack.Height * content.Height / contentHeight)) : 0;
        var hThumb = hTrack with { X = hTrack.X + (maxX > 0 ? (hTrack.Width - thumbWidth) * x / maxX : 0), Width = thumbWidth };
        var vThumb = vTrack with { Y = vTrack.Y + (maxY > 0 ? (vTrack.Height - thumbHeight) * y / maxY : 0), Height = thumbHeight };
        return new(content, hTrack, hThumb, vTrack, vThumb, maxX, maxY, x, y);
    }
}
