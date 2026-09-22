namespace StationeryUI.Styling;

using StationeryUI.Canvas;

/// <summary>Carves edge children in declaration order; center always receives the final remainder.</summary>
public static class StationeryDockLayout
{
    public static IReadOnlyDictionary<string, ScreenRectangle> Arrange(ScreenRectangle bounds, IReadOnlyList<StationeryDockBinding> children)
    {
        if (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y) || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)
            || bounds.Width < 0 || bounds.Height < 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        if (children.Count(c => c.Dock == "center") > 1) throw new ArgumentException("dock-layout allows at most one center.", nameof(children));
        var result = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var remaining = bounds;
        foreach (var child in children)
        {
            if (!double.IsFinite(child.Size) || child.Size < 0) throw new ArgumentOutOfRangeException(nameof(children));
            var horizontal = child.Dock is "top" or "bottom";
            var size = Math.Min(child.Size, horizontal ? remaining.Height : remaining.Width);
            var area = remaining;
            switch (child.Dock)
            {
                case "top": area = remaining with { Height = size }; remaining = remaining with { Y = remaining.Y + size, Height = remaining.Height - size }; break;
                case "bottom": area = remaining with { Y = remaining.Y + remaining.Height - size, Height = size }; remaining = remaining with { Height = remaining.Height - size }; break;
                case "left": area = remaining with { Width = size }; remaining = remaining with { X = remaining.X + size, Width = remaining.Width - size }; break;
                case "right": area = remaining with { X = remaining.X + remaining.Width - size, Width = size }; remaining = remaining with { Width = remaining.Width - size }; break;
                case "center": continue;
                default: throw new ArgumentException($"Unknown dock direction: {child.Dock}.", nameof(children));
            }
            result.Add(child.ModelPath, area);
        }
        foreach (var child in children.Where(c => c.Dock == "center")) result.Add(child.ModelPath, remaining);
        return result;
    }
}
