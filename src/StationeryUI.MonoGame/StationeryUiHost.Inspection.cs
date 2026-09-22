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

    /// <summary>Draws the selected model/layout outline, thin margin boundary and dashed partitions in window pixels.</summary>
    public void DrawInspectionSelection(StationeryUI.Inspection.StationeryInspectionEntry entry)
    {
        if (!entry.Visible) return;
        if (entry.WindowBounds is { Width: > 0, Height: > 0 } bounds) DrawInspectionOutline(bounds);
        var lines = entry.PartitionLines ?? [];
        var margin = entry.MarginBounds;
        if (lines.Count == 0 && margin is null) return;
        sprites.Begin(blendState: BlendState.NonPremultiplied);
        try
        {
            var color = new Color(255, 105, 180);
            // One physical pixel, unaffected by UI zoom. Use the original allocation,
            // not expanded content bounds: large margins may have been clamped.
            if (margin is { Width: > 0, Height: > 0 } m && margin != entry.WindowBounds)
            {
                void Strip(double x, double y, double w, double h)
                    => sprites.Draw(pixel, RectangleOf(new(x, y, w, h)), color);
                var inset = entry.BoxModel?.Margin;
                var inner = entry.WindowBounds;
                if (inset?.Top > 0 || inset is null && inner is { } top && top.Y > m.Y)
                    Strip(m.X, m.Y, m.Width, Math.Min(1, m.Height));
                if (inset?.Bottom > 0 || inset is null && inner is { } bottom && bottom.Y + bottom.Height < m.Y + m.Height)
                    Strip(m.X, m.Y + Math.Max(0, m.Height - 1), m.Width, Math.Min(1, m.Height));
                if (inset?.Left > 0 || inset is null && inner is { } left && left.X > m.X)
                    Strip(m.X, m.Y, Math.Min(1, m.Width), m.Height);
                if (inset?.Right > 0 || inset is null && inner is { } right && right.X + right.Width < m.X + m.Width)
                    Strip(m.X + Math.Max(0, m.Width - 1), m.Y, Math.Min(1, m.Width), m.Height);
            }
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

    /// <summary>Draws the style designer's component, margin and parent-layout guides.</summary>
    public void DrawPreviewGuides(ScreenRectangle? marginBounds, ScreenRectangle? componentBounds,
        IReadOnlyList<StationeryUI.Inspection.StationeryInspectionLine> partitions)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        sprites.Begin(blendState: BlendState.NonPremultiplied);
        try
        {
            var pink = new Color(255, 105, 180);
            var cyan = new Color(80, 200, 255);
            void Strip(ScreenRectangle bounds, Color color)
            {
                if (bounds.Width > 0 && bounds.Height > 0) sprites.Draw(pixel, RectangleOf(bounds), color);
            }
            if (componentBounds is { Width: > 0, Height: > 0 } component)
            {
                const double thickness = 3;
                Strip(new(component.X, component.Y, component.Width, thickness), cyan);
                Strip(new(component.X, component.Y + component.Height - thickness, component.Width, thickness), cyan);
                Strip(new(component.X, component.Y, thickness, component.Height), cyan);
                Strip(new(component.X + component.Width - thickness, component.Y, thickness, component.Height), cyan);
            }
            if (marginBounds is { Width: > 0, Height: > 0 } margin && componentBounds is { } inner && margin != inner)
            {
                const double thickness = 1;
                if (inner.Y > margin.Y) Strip(new(margin.X, margin.Y, margin.Width, thickness), cyan);
                if (inner.Y + inner.Height < margin.Y + margin.Height)
                    Strip(new(margin.X, margin.Y + margin.Height - thickness, margin.Width, thickness), cyan);
                if (inner.X > margin.X) Strip(new(margin.X, margin.Y, thickness, margin.Height), cyan);
                if (inner.X + inner.Width < margin.X + margin.Width)
                    Strip(new(margin.X + margin.Width - thickness, margin.Y, thickness, margin.Height), cyan);
            }
            foreach (var line in partitions)
            {
                var vertical = line.Start.X == line.End.X;
                var length = vertical ? line.End.Y - line.Start.Y : line.End.X - line.Start.X;
                for (double offset = 0; offset < length; offset += 10)
                {
                    var dash = Math.Min(5, length - offset);
                    var rect = vertical ? new ScreenRectangle(line.Start.X - 1, line.Start.Y + offset, 2, dash)
                        : new ScreenRectangle(line.Start.X + offset, line.Start.Y - 1, dash, 2);
                    Strip(rect, pink);
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
