namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>MonoGame の描画資源を隠蔽し、画面向けの低水準描画を提供します。</summary>
public sealed class ScreenCanvas : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly Texture2D _softCircle;
    private bool _disposed;

    public ScreenCanvas(GraphicsDevice graphicsDevice, ContentManager content)
        : this(graphicsDevice, content.Load<SpriteFont>("Fonts/Ui")) { }

    public ScreenCanvas(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _pixel = CreateTexture(1, 1, (_, _) => Color.White);
        _softCircle = CreateCircleTexture(128, new Color(255, 255, 255, 255), softEdge: true);
    }

    public int ScreenWidth => VirtualScreen.Width;
    public int ScreenHeight => VirtualScreen.Height;
    public GraphicsDevice GraphicsDevice => _graphicsDevice;
    public SpriteBatch SpriteBatch => _spriteBatch;
    public SpriteFont Font => _font;
    public Texture2D SoftCircle => _softCircle;

    public void Begin() => _spriteBatch.Begin(samplerState: SamplerState.LinearClamp,
        transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
    public void End() => _spriteBatch.End();
    public Point ToVirtualPoint(Point point) => VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, point);

    public void FillRectangle(Rectangle bounds, Color color) => _spriteBatch.Draw(_pixel, bounds, color);

    public void FillRoundedRectangle(Rectangle bounds, int radius, Color color)
    {
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        FillRectangle(new Rectangle(bounds.X + radius, bounds.Y, bounds.Width - radius * 2, bounds.Height), color);
        FillRectangle(new Rectangle(bounds.X, bounds.Y + radius, bounds.Width, bounds.Height - radius * 2), color);
        DrawCircle(new Vector2(bounds.X + radius, bounds.Y + radius), radius, color);
        DrawCircle(new Vector2(bounds.Right - radius, bounds.Y + radius), radius, color);
        DrawCircle(new Vector2(bounds.X + radius, bounds.Bottom - radius), radius, color);
        DrawCircle(new Vector2(bounds.Right - radius, bounds.Bottom - radius), radius, color);
    }

    public void DrawRectangle(Rectangle bounds, int thickness, Color color)
    {
        FillRectangle(new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        FillRectangle(new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        FillRectangle(new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        FillRectangle(new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        var angle = MathF.Atan2(direction.Y, direction.X);
        _spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero,
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    public void DrawCircle(Vector2 center, float radius, Color color)
    {
        var size = (int)(radius * 2);
        _spriteBatch.Draw(_softCircle,
            new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size), color);
    }

    public void DrawCircleSurface(Rectangle bounds, Color color) => _spriteBatch.Draw(_softCircle, bounds, color);

    public void DrawGlow(Vector2 center, float radius, Color color) =>
        _spriteBatch.Draw(_softCircle,
            new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2)), color);

    public void DrawText(string text, Vector2 position, Color color, float scale)
    {
        var shadowAlpha = (int)MathF.Round(125f * color.A / 255f);
        _spriteBatch.DrawString(_font, text, position + new Vector2(2, 2), new Color(0, 0, 0, shadowAlpha),
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public void DrawFittedText(string text, Rectangle bounds, Color color, float scale)
    {
        var measured = MeasureText(text);
        var fittedScale = MathF.Min(scale,
            MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(text, new Vector2(bounds.X, bounds.Center.Y - size.Y / 2), color, fittedScale);
    }

    public void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float scale)
    {
        var measured = MeasureText(text);
        var fittedScale = MathF.Min(scale,
            MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        _spriteBatch.DrawString(_font, text,
            new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color,
            0f, Vector2.Zero, fittedScale, SpriteEffects.None, 0f);
    }

    public void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _spriteBatch.DrawString(_font, text, center, color, -MathHelper.PiOver2,
            MeasureText(text) / 2f, scale, SpriteEffects.None, 0f);

    public Vector2 MeasureText(string text) => _font.MeasureString(text);
    public bool CanDrawText(string text)
    {
        foreach (var character in text)
            if (!_font.Characters.Contains(character)) return false;
        return true;
    }

    public int FontLineSpacing => _font.LineSpacing;

    public void DrawTexture(Texture2D texture, Rectangle bounds, Color color) => _spriteBatch.Draw(texture, bounds, color);

    public Texture2D CreateTexture(int width, int height, Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                data[y * width + x] = colorFactory(x, y);
        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateCircleTexture(int size, Color color, bool softEdge) =>
        CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var distance = MathF.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
            var radius = size * 0.48f;
            if (distance > radius) return Color.Transparent;
            var alpha = softEdge ? MathHelper.Clamp((radius - distance) / (radius * 0.45f), 0f, 1f) : 1f;
            return color * alpha;
        });

    public void DrawEllipseWire(Vector2 center, float width, float height, Color color, int thickness, float rotation) =>
        DrawInscribedEllipseArc(center, width, height, color, thickness, rotation, 0f, MathHelper.TwoPi);

    public void DrawCircumscribedCircleArc(Vector2 center, float width, float height, Color color, int thickness,
        float rotation, float startAngle, float endAngle)
    {
        var diameter = MathF.Sqrt(width * width + height * height);
        DrawInscribedEllipseArc(center, diameter, diameter, color, thickness, rotation, startAngle, endAngle);
    }

    public void DrawInscribedEllipseArc(Vector2 center, float width, float height, Color color, int thickness,
        float rotation, float startAngle, float endAngle)
    {
        const int segments = 128;
        var cosRotation = MathF.Cos(rotation);
        var sinRotation = MathF.Sin(rotation);
        Vector2 Transform(float angle)
        {
            var x = MathF.Cos(angle) * width * 0.5f;
            var y = MathF.Sin(angle) * height * 0.5f;
            return center + new Vector2(x * cosRotation - y * sinRotation, x * sinRotation + y * cosRotation);
        }
        var whole = MathF.Abs(endAngle - startAngle) >= MathHelper.TwoPi - 0.0001f;
        var start = NormalizeAngle(startAngle);
        var end = NormalizeAngle(endAngle);
        for (var index = 0; index < segments; index++)
        {
            var segmentStart = MathHelper.TwoPi * index / segments;
            var segmentEnd = MathHelper.TwoPi * (index + 1) / segments;
            var middle = (segmentStart + segmentEnd) * 0.5f;
            if (!whole && !IsAngleVisible(middle, start, end)) continue;
            DrawLine(Transform(segmentStart), Transform(segmentEnd), thickness, color);
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= MathHelper.TwoPi;
        return angle < 0f ? angle + MathHelper.TwoPi : angle;
    }

    private static bool IsAngleVisible(float angle, float start, float end)
    {
        angle = NormalizeAngle(angle);
        return start <= end ? angle >= start && angle <= end : angle >= start || angle <= end;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _pixel.Dispose();
        _softCircle.Dispose();
        _spriteBatch.Dispose();
        _disposed = true;
    }
}
