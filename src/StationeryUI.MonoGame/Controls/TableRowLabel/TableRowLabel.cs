namespace StationeryUI.MonoGame.Controls.TableRowLabel;

using Microsoft.Xna.Framework;
using System;

/// <summary>表形式 UI の1行を識別するラベルです。</summary>
public sealed class TableRowLabel
{
    public TableRowLabel(string text, Rectangle bounds, Color color, float scale = 0.36f)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Bounds = bounds;
        Color = color;
        Scale = scale;
    }

    public string Text { get; set; }
    public Rectangle Bounds { get; set; }
    public Color Color { get; set; }
    public float Scale { get; set; }

    public void Draw(Action<string, Vector2, Color, float> drawText) =>
        (drawText ?? throw new ArgumentNullException(nameof(drawText)))(Text, new Vector2(Bounds.X, Bounds.Y), Color, Scale);

    public void DrawFitted(Action<string, Rectangle, Color, float> drawFittedText) =>
        (drawFittedText ?? throw new ArgumentNullException(nameof(drawFittedText)))(Text, Bounds, Color, Scale);
}
