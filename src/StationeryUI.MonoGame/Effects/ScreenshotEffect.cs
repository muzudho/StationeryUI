namespace StationeryUI.MonoGame.Effects;

using Microsoft.Xna.Framework;
using System;

/// <summary>GUIとインストーラーで共有するスクリーンショット撮影時の演出です。</summary>
public sealed class ScreenshotEffect
{
    public void Draw(StationeryDrawingTools drawingContext, float progress)
    {
        drawingContext.Begin();
        Draw(progress, new ScreenshotEffectDrawingCallbacks(
            drawingContext.ScreenWidth, drawingContext.ScreenHeight, drawingContext.FillRectangle));
        drawingContext.End();
    }

    public void Draw(float progress, ScreenshotEffectDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        progress = Math.Clamp(progress, 0f, 1f);

        var flashOpacity = progress < 0.30f ? (byte)(210f * (1f - progress / 0.30f)) : (byte)0;
        if (flashOpacity > 0)
            draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(255, 255, 245, (int)flashOpacity));

        var shutterPhase = progress < 0.42f ? progress / 0.42f : 1f - (progress - 0.42f) / 0.58f;
        shutterPhase = Math.Clamp(shutterPhase, 0f, 1f);
        var shutterHeight = (int)(draw.VirtualScreenHeight * 0.075f * shutterPhase);
        if (shutterHeight > 0)
        {
            var shutterColor = new Color(8, 10, 14, (int)(215f * shutterPhase));
            draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, shutterHeight), shutterColor);
            draw.FillRectangle(new Rectangle(0, draw.VirtualScreenHeight - shutterHeight, draw.VirtualScreenWidth, shutterHeight), shutterColor);
        }

        var lineOpacity = (byte)(255f * (1f - progress));
        if (lineOpacity > 0)
            draw.FillRectangle(new Rectangle(0, draw.VirtualScreenHeight / 2 - 2, draw.VirtualScreenWidth, 4), new Color(255, 255, 255, (int)lineOpacity));
    }
}

public sealed record ScreenshotEffectDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle);
