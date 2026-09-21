namespace StationeryUI.Styling;

using System.Collections.ObjectModel;
using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Controls;

public sealed record StationeryLayoutResult(IReadOnlyDictionary<string, ScreenRectangle> Bounds,
    IReadOnlyDictionary<string, ScreenRectangle> ContentBounds)
{
    public IReadOnlyDictionary<string, ScreenRectangle> BorderBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
}

/// <summary>Computes window-pixel rectangles without changing the model tree or using a graphics device.</summary>
public static class StationeryLayoutEngine
{
    public static StationeryLayoutResult Arrange(StationeryStyleSettings settings, double width, double height)
    {
        if (!double.IsFinite(width) || width < 0 || !double.IsFinite(height) || height < 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Window dimensions must be finite and nonnegative.");
        var layouts = settings.Layouts.ToDictionary(layout => layout.Id, StringComparer.Ordinal);
        var panels = settings.Bindings.Where(binding => layouts[binding.Layout].Type == "panel")
            .ToDictionary(binding => binding.ModelPath, binding => layouts[binding.Layout], StringComparer.Ordinal);
        var grids = settings.Bindings.Where(binding => layouts[binding.Layout].Type == "floating-layout")
            .ToDictionary(binding => binding.ModelPath, StringComparer.Ordinal);
        var pages = settings.Bindings.Where(b => b.InspectorModel is not null).ToDictionary(b => b.ModelPath);
        var positions = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var splits = settings.Bindings.Where(binding => layouts[binding.Layout].Type == "split-pane").ToDictionary(binding => binding.ModelPath);
        var bounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var contents = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var borders = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);

        void Visit(StationeryNode node, ScreenRectangle inherited)
        {
            var outer = positions.GetValueOrDefault(node.Path, inherited);
            if (panels.TryGetValue(node.Path, out var box))
            {
                var inset = box.Margin.GetContentBounds(outer.Width, outer.Height);
                outer = inset with { X = outer.X + inset.X, Y = outer.Y + inset.Y };
                borders[node.Path] = new(outer.X - box.Border.Left, outer.Y - box.Border.Top,
                    outer.Width + box.Border.Left + box.Border.Right, outer.Height + box.Border.Top + box.Border.Bottom);
            }
            bounds.Add(node.Path, outer);
            var content = outer;
            if (pages.TryGetValue(node.Path, out var page))
            {
                var inspectorHeight = Math.Min(outer.Height, layouts[page.Layout].InspectorHeight);
                content = outer with { Height = outer.Height - inspectorHeight };
                positions.Add(page.InspectorModel!, new(outer.X, outer.Y + content.Height, outer.Width, inspectorHeight));
            }
            if (panels.TryGetValue(node.Path, out var panel))
            {
                var inset = panel.Padding.GetContentBounds(content.Width, content.Height);
                content = inset with { X = outer.X + inset.X, Y = outer.Y + inset.Y };
            }
            contents.Add(node.Path, content);
            if (splits.TryGetValue(node.Path, out var splitBinding))
            {
                var state = new SplitPane(); state.Configure(layouts[splitBinding.Layout].Split!);
                var splitBounds = state.Arrange(content);
                positions.Add(splitBinding.FirstModel!, splitBounds.First);
                positions.Add(splitBinding.SecondModel!, splitBounds.Second);
            }
            if (grids.TryGetValue(node.Path, out var binding))
            {
                var layout = layouts[binding.Layout];
                var rows = TrackEdges(layout.Rows, content.Height);
                var columns = TrackEdges(layout.Columns, content.Width);
                foreach (var child in binding.Children)
                    positions.Add(child.ModelPath, new(content.X + columns[child.Column], content.Y + rows[child.Row],
                        columns[child.Column + 1] - columns[child.Column], rows[child.Row + 1] - rows[child.Row]));
            }
            foreach (var child in node.Children) Visit(child, content);
        }

        foreach (var model in settings.Models) Visit(model.CreateTree(), new(0, 0, width, height));
        return new(new ReadOnlyDictionary<string, ScreenRectangle>(bounds), new ReadOnlyDictionary<string, ScreenRectangle>(contents))
        { BorderBounds = new ReadOnlyDictionary<string, ScreenRectangle>(borders) };
    }

    private static double[] TrackEdges(IReadOnlyList<LayoutTrack> tracks, double available)
    {
        // Normalize weights before summing so even large finite rates cannot overflow.
        var pixelMax = tracks.Where(track => !track.IsRate).Select(track => track.Value).DefaultIfEmpty(0).Max();
        var pixelWeights = pixelMax == 0 ? 0 : tracks.Where(track => !track.IsRate).Sum(track => track.Value / pixelMax);
        var pixelTotal = pixelMax * pixelWeights;
        var shrinkPixels = pixelTotal > available;
        var remaining = Math.Max(0, available - pixelTotal);
        var rateMax = tracks.Where(track => track.IsRate).Select(track => track.Value).DefaultIfEmpty(0).Max();
        var rateWeights = rateMax == 0 ? 0 : tracks.Where(track => track.IsRate).Sum(track => track.Value / rateMax);
        var edges = new double[tracks.Count + 1];
        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            var size = track.IsRate
                ? (rateWeights == 0 ? 0 : remaining * (track.Value / rateMax) / rateWeights)
                : (shrinkPixels ? available * (track.Value / pixelMax) / pixelWeights : track.Value);
            edges[i + 1] = Math.Min(available, edges[i] + size);
        }
        // Prevent rounding accumulation from leaving a seam at the viewport edge.
        if (rateWeights > 0 || shrinkPixels) edges[^1] = available;
        return edges;
    }
}
