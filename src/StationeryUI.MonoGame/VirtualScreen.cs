namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public static class VirtualScreen
{
    public const int Width = 1920;
    public const int Height = 1080;

    public static Matrix GetTransform(Viewport viewport)
    {
        var scale = GetScale(viewport);
        var offsetX = (viewport.Width - Width * scale) * 0.5f;
        var offsetY = (viewport.Height - Height * scale) * 0.5f;
        return Matrix.CreateScale(scale, scale, 1f) * Matrix.CreateTranslation(offsetX, offsetY, 0f);
    }

    public static Point ToVirtualPoint(Viewport viewport, Point screenPoint)
    {
        var scale = GetScale(viewport);
        var offsetX = (viewport.Width - Width * scale) * 0.5f;
        var offsetY = (viewport.Height - Height * scale) * 0.5f;
        return new Point((int)((screenPoint.X - offsetX) / scale), (int)((screenPoint.Y - offsetY) / scale));
    }

    public static float GetScale(Viewport viewport)
    {
        return Math.Min(viewport.Width / (float)Width, viewport.Height / (float)Height);
    }
}
