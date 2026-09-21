namespace StationeryUI.Inspection;

/// <summary>Find the deepest visible component at a window-pixel position.</summary>
public static class DeveloperCapture
{
    public static StationeryInspectionEntry? HitTest(IReadOnlyList<StationeryInspectionEntry> entries,
        double x, double y, string? scope = null)
        => entries.Where(e => e.Visible && (scope is null || e.Path == scope || e.Path.StartsWith(scope + "/", StringComparison.Ordinal)) &&
            e.WindowBounds is { Width: > 0, Height: > 0 } b &&
            x >= b.X && y >= b.Y && x < b.X + b.Width && y < b.Y + b.Height)
            .Reverse().OrderByDescending(e => e.Path.Count(c => c == '/')).FirstOrDefault();
}
