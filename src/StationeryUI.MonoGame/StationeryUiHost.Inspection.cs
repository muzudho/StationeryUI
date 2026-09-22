namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.Canvas;

public sealed partial class StationeryUiHost
{
    /// <summary>Draw an inspector selection in window pixels, independent of UI scale.</summary>
    public void DrawInspectionOutline(ScreenRectangle bounds)
    {
        sprites.Begin(blendState: BlendState.NonPremultiplied);
        try
        {
            var color = new Color(255, 105, 180);
            void Strip(double x, double y, double w, double h) => sprites.Draw(pixel, RectangleOf(new(x, y, w, h)), color);
            Strip(bounds.X, bounds.Y, bounds.Width, 3);
            Strip(bounds.X, bounds.Y + bounds.Height - 3, bounds.Width, 3);
            Strip(bounds.X, bounds.Y, 3, bounds.Height);
            Strip(bounds.X + bounds.Width - 3, bounds.Y, 3, bounds.Height);
        }
        finally { sprites.End(); }
    }

    /// <summary>Draws the selected model/layout outline and dashed partitions in window pixels.</summary>
    public void DrawInspectionSelection(StationeryUI.Inspection.StationeryInspectionEntry entry)
    {
        if (!entry.Visible || entry.WindowBounds is not { Width: > 0, Height: > 0 } bounds) return;
        DrawInspectionOutline(bounds);
        if (entry.PartitionLines is not { Count: > 0 } lines) return;
        sprites.Begin(blendState: BlendState.NonPremultiplied);
        try
        {
            var color = new Color(255, 105, 180);
            foreach (var line in lines)
            {
                var vertical = line.Start.X == line.End.X;
                var length = vertical ? line.End.Y - line.Start.Y : line.End.X - line.Start.X;
                for (double offset = 0; offset < length; offset += 10)
                {
                    var dash = Math.Min(5, length - offset);
                    var rect = vertical ? new ScreenRectangle(line.Start.X - 1, line.Start.Y + offset, 2, dash)
                        : new ScreenRectangle(line.Start.X + offset, line.Start.Y - 1, dash, 2);
                    sprites.Draw(pixel, RectangleOf(rect), color);
                }
            }
        }
        finally { sprites.End(); }
    }

    /// <summary>A font-independent pinching hand icon with a latched pink background.</summary>
    public void DrawCaptureIcon(ScreenRectangle bounds, bool enabled, bool focused = false)
    {
        sprites.Begin(blendState: BlendState.NonPremultiplied);
        try
        {
            var window = Viewport.ToWindow(bounds);
            sprites.Draw(pixel, RectangleOf(window), enabled ? new Color(125, 42, 85) : new Color(45, 49, 57));
            if (focused) DrawButtonOutline(bounds, 2, Theme.Accent);
            var ink = enabled ? new Color(255, 145, 200) : Color.White;
            // Index finger curling toward the thumb, with three folded fingers and a wrist.
            Vector2[] points = [new(16, 39), new(11, 29), new(10, 22), new(13, 20),
                new(19, 27), new(20, 22), new(16, 13), new(17, 8), new(21, 8),
                new(27, 18), new(32, 18), new(37, 23), new(36, 31), new(31, 39), new(16, 39)];
            for (var i = 1; i < points.Length; i++)
            {
                var a = new Vector2((float)window.X, (float)window.Y) + points[i - 1] * (float)Viewport.Scale;
                var delta = (points[i] - points[i - 1]) * (float)Viewport.Scale;
                sprites.Draw(pixel, a, null, ink, MathF.Atan2(delta.Y, delta.X), Vector2.Zero,
                    new Vector2(delta.Length(), 2 * (float)Viewport.Scale), SpriteEffects.None, 0);
            }
        }
        finally { sprites.End(); }
    }
}
