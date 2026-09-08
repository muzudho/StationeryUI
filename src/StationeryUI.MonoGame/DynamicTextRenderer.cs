namespace StationeryUI.MonoGame;

using StationeryUI.Platform;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>SpriteFont にない文字を含む動的テキストを描画し、生成テクスチャを再利用します。</summary>
internal sealed class DynamicTextRenderer : IDisposable
{
    private readonly ScreenCanvas _canvas;
    private readonly ITextRasterizer _textRasterizer;
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly List<Texture2D> _retired = [];

    public DynamicTextRenderer(ScreenCanvas canvas, ITextRasterizer textRasterizer)
    {
        _canvas = canvas;
        _textRasterizer = textRasterizer;
    }

    public void Draw(string text, Rectangle bounds, Color color, float scale)
    {
        if (_canvas.CanDrawText(text))
        {
            _canvas.DrawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_textures.TryGetValue(text, out var texture))
        {
            if (_textures.Count >= 128)
            {
                var oldest = _textures.First();
                _retired.Add(oldest.Value);
                _textures.Remove(oldest.Key);
            }
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_canvas.GraphicsDevice, stream, rgba =>
            {
                for (var i = 0; i < rgba.Length; i += 4)
                {
                    var alpha = rgba[i + 3];
                    rgba[i] = (byte)(rgba[i] * alpha / 255);
                    rgba[i + 1] = (byte)(rgba[i + 1] * alpha / 255);
                    rgba[i + 2] = (byte)(rgba[i + 2] * alpha / 255);
                }
            });
            _textures[text] = texture;
        }

        var targetHeight = MathF.Min(bounds.Height, _canvas.FontLineSpacing * scale);
        var fittedScale = MathF.Min(bounds.Width / (float)texture.Width, targetHeight / texture.Height);
        _canvas.DrawTexture(texture, new Rectangle(bounds.X,
            bounds.Y + (bounds.Height - (int)(texture.Height * fittedScale)) / 2,
            (int)(texture.Width * fittedScale), (int)(texture.Height * fittedScale)), color);
    }

    public void EndFrame()
    {
        foreach (var texture in _retired) texture.Dispose();
        _retired.Clear();
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values) texture.Dispose();
        _textures.Clear();
        EndFrame();
    }
}
