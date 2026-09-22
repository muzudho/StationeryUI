namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework.Graphics;
using StationeryUI.Canvas;
using StationeryUI.Styling;

public sealed partial class StationeryUiHost
{
    /// <summary>Draws diagnostic headers reserved by recovered layouts. Call after drawing child controls.</summary>
    public void DrawLayoutErrors(StationeryLayoutResult layout, Func<string, bool>? include = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = graphics.ScissorRectangle;
        try
        {
            foreach (var error in layout.Errors)
            {
                if (include is not null && !include(error.ModelPath)) continue;
                graphics.ScissorRectangle = Microsoft.Xna.Framework.Rectangle.Intersect(RectangleOf(error.Bounds), graphics.Viewport.Bounds);
                if (graphics.ScissorRectangle.Width <= 0 || graphics.ScissorRectangle.Height <= 0) continue;
                var bounds = FromWindow(error.Bounds);
                var theme = Theme with { FontSize = Math.Max(1, (int)Math.Round(14 / Viewport.Scale)), Padding = Math.Max(0, (int)Math.Round(4 / Viewport.Scale)),
                    Surface = new(90, 25, 25), Text = new(255, 235, 235) };
                var element = new Element(Root, bounds, "レイアウトエラー：" + error.Message);
                sprites.Begin(blendState: BlendState.NonPremultiplied, rasterizerState: clipState);
                try { DrawTextBlock(element, theme); }
                finally { sprites.End(); }
            }
        }
        finally { graphics.ScissorRectangle = previous; }
    }

    /// <summary>Draw panel borders over the padding side of their allocated bounds. Call after Draw;
    /// include can restrict drawing to the currently visible model paths.</summary>
    public void DrawPanelBorders(StationeryLayoutResult layout, Func<string, bool>? include = null, ScreenRectangle? clip = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = graphics.ScissorRectangle;
        graphics.ScissorRectangle = clip is { } area ? Microsoft.Xna.Framework.Rectangle.Intersect(RectangleOf(area), graphics.Viewport.Bounds) : graphics.Viewport.Bounds;
        sprites.Begin(blendState: BlendState.NonPremultiplied, rasterizerState: clipState);
        try
        {
            var outlines = layout.LayoutBorderBounds.Count > 0
                ? layout.LayoutBorderBounds.Select(pair => (Owner: pair.Key.Split(':')[0], Outside: pair.Value, Inside: layout.LayoutBounds[pair.Key]))
                : layout.BorderBounds.Select(pair => (Owner: pair.Key, Outside: pair.Value, Inside: layout.Bounds[pair.Key]));
            foreach (var (path, outside, inside) in outlines)
            {
                if (include is not null && !include(path)) continue;
                void Strip(ScreenRectangle bounds)
                {
                    if (bounds.Width > 0 && bounds.Height > 0) Fill(FromWindow(bounds), Theme.Border);
                }
                // LayoutEngine records the configured border thickness as the
                // difference between outside and inside. Paint it inward from
                // the allocated edge so it overlays padding and never enters
                // the margin band.
                var top = Math.Max(0, inside.Y - outside.Y);
                var right = Math.Max(0, outside.X + outside.Width - inside.X - inside.Width);
                var bottom = Math.Max(0, outside.Y + outside.Height - inside.Y - inside.Height);
                var left = Math.Max(0, inside.X - outside.X);
                Strip(new(inside.X, inside.Y, inside.Width, top));
                Strip(new(inside.X, inside.Y + inside.Height - bottom, inside.Width, bottom));
                Strip(new(inside.X, inside.Y, left, inside.Height));
                Strip(new(inside.X + inside.Width - right, inside.Y, right, inside.Height));
            }
        }
        finally { sprites.End(); graphics.ScissorRectangle = previous; }
    }
}
