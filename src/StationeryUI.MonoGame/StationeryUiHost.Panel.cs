namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework.Graphics;
using StationeryUI.Canvas;
using StationeryUI.Styling;

public sealed partial class StationeryUiHost
{
    /// <summary>Draw panel outlines outside their allocated bounds. Call after Draw;
    /// include can restrict drawing to the currently visible model paths.</summary>
    public void DrawPanelBorders(StationeryLayoutResult layout, Func<string, bool>? include = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = graphics.ScissorRectangle;
        graphics.ScissorRectangle = graphics.Viewport.Bounds;
        sprites.Begin(blendState: BlendState.NonPremultiplied, rasterizerState: clipState);
        try
        {
            foreach (var (path, outside) in layout.BorderBounds)
            {
                if (include is not null && !include(path)) continue;
                var inside = layout.Bounds[path];
                void Strip(ScreenRectangle bounds)
                {
                    if (bounds.Width > 0 && bounds.Height > 0) Fill(FromWindow(bounds), Theme.Border);
                }
                Strip(new(outside.X, outside.Y, outside.Width, inside.Y - outside.Y));
                Strip(new(outside.X, inside.Y + inside.Height, outside.Width, outside.Y + outside.Height - inside.Y - inside.Height));
                Strip(new(outside.X, inside.Y, inside.X - outside.X, inside.Height));
                Strip(new(inside.X + inside.Width, inside.Y, outside.X + outside.Width - inside.X - inside.Width, inside.Height));
            }
        }
        finally { sprites.End(); graphics.ScissorRectangle = previous; }
    }
}
