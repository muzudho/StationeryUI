namespace StationeryUI.Canvas;

/// <summary>Logical-to-window coordinates. Use the same transform for rendering, hit tests and IME.</summary>
public sealed class UiViewport
{
    private double scale = 1;
    public double Scale
    {
        get => scale;
        set => scale = double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
    public ScreenPoint Offset { get; set; }
    public ScreenPoint ToLogical(ScreenPoint point) => new((point.X - Offset.X) / Scale, (point.Y - Offset.Y) / Scale);
    public ScreenRectangle ToWindow(ScreenRectangle bounds) => new(bounds.X * Scale + Offset.X,
        bounds.Y * Scale + Offset.Y, bounds.Width * Scale, bounds.Height * Scale);
    public static ScreenRectangle Inset(ScreenRectangle bounds, double padding) => new(bounds.X + padding,
        bounds.Y + padding, Math.Max(0, bounds.Width - padding * 2), Math.Max(0, bounds.Height - padding * 2));
}
