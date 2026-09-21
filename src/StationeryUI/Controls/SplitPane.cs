namespace StationeryUI.Controls;

using StationeryUI.Canvas;

/// <param name="Horizontal">True: horizontal divider, top/bottom panes. False: vertical divider, left/right panes.</param>
public sealed record SplitPaneOptions(bool Horizontal = false, double Ratio = .5, double DividerWidth = 8, double MinimumPaneSize = 40);
public sealed record SplitPaneBounds(ScreenRectangle First, ScreenRectangle Divider, ScreenRectangle Second);

public sealed class SplitPane
{
    public SplitPaneOptions Options { get; private set; } = new();
    public double Ratio { get; private set; } = .5;
    public void Configure(SplitPaneOptions options)
    {
        if (!double.IsFinite(options.Ratio) || options.Ratio < 0 || options.Ratio > 1 ||
            !double.IsFinite(options.DividerWidth) || options.DividerWidth <= 0 ||
            !double.IsFinite(options.MinimumPaneSize) || options.MinimumPaneSize < 0)
            throw new ArgumentOutOfRangeException(nameof(options));
        if (Options == options) return;
        Options = options;
        Ratio = options.Ratio;
    }
    public void SetRatio(double ratio)
    {
        if (!double.IsFinite(ratio)) throw new ArgumentOutOfRangeException(nameof(ratio));
        Ratio = Math.Clamp(ratio, 0, 1);
    }
    public SplitPaneBounds Arrange(ScreenRectangle bounds)
    {
        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) || bounds.Width < 0 || bounds.Height < 0)
            throw new ArgumentOutOfRangeException(nameof(bounds));
        var length = Math.Max(0, Options.Horizontal ? bounds.Height : bounds.Width);
        var divider = Math.Min(length, Options.DividerWidth);
        var available = length - divider;
        var minimum = Math.Min(Options.MinimumPaneSize, available / 2);
        var first = Math.Clamp(available * Ratio, minimum, available - minimum);
        return Options.Horizontal
            ? new(new(bounds.X, bounds.Y, bounds.Width, first), new(bounds.X, bounds.Y + first, bounds.Width, divider),
                new(bounds.X, bounds.Y + first + divider, bounds.Width, available - first))
            : new(new(bounds.X, bounds.Y, first, bounds.Height), new(bounds.X + first, bounds.Y, divider, bounds.Height),
                new(bounds.X + first + divider, bounds.Y, available - first, bounds.Height));
    }
    public void Drag(ScreenRectangle bounds, double coordinate, double grabOffset)
    {
        var available = (Options.Horizontal ? bounds.Height : bounds.Width) - Options.DividerWidth;
        if (available <= 0) return;
        var minimum = Math.Min(Options.MinimumPaneSize, available / 2);
        var start = Options.Horizontal ? bounds.Y : bounds.X;
        SetRatio(Math.Clamp(coordinate - start - grabOffset, minimum, available - minimum) / available);
    }
}
