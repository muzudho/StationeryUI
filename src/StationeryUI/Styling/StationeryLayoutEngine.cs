namespace StationeryUI.Styling;

using System.Collections.ObjectModel;
using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Controls;

public sealed record StationeryLayoutResult(IReadOnlyDictionary<string, ScreenRectangle> Bounds,
    IReadOnlyDictionary<string, ScreenRectangle> ContentBounds)
{
    public IReadOnlyList<StationeryLayoutError> Errors { get; init; } = [];
    // Keyed by owner model path + ":" + complete layout path (layouts are reusable).
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutContentBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutBorderBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> BorderBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
}
public sealed record StationeryLayoutError(string ModelPath, string LayoutPath, string Message, ScreenRectangle Bounds);

/// <summary>Computes window-pixel rectangles without changing the model tree or using a graphics device.</summary>
public static class StationeryLayoutEngine
{
    /// <summary>All grid cells, including empty cells, in the supplied viewport-pixel content area.</summary>
    public static IReadOnlyList<(int Row, int Column, ScreenRectangle Bounds)> ArrangeGridCells(StationeryLayoutNode layout, ScreenRectangle content)
    {
        if (layout.Type != "grid-layout") throw new ArgumentException("Expected grid-layout.", nameof(layout));
        var rows = TrackEdges(layout.Rows, content.Height);
        var columns = TrackEdges(layout.Columns, content.Width);
        var result = new List<(int, int, ScreenRectangle)>();
        for (var r = 0; r < layout.Rows.Count; r++)
            for (var c = 0; c < layout.Columns.Count; c++)
                result.Add((r, c, new(content.X + columns[c], content.Y + rows[r], columns[c + 1] - columns[c], rows[r + 1] - rows[r])));
        return result;
    }
    public static StationeryLayoutResult Arrange(StationeryStyleSettings settings, double width, double height)
    {
        if (!double.IsFinite(width) || width < 0 || !double.IsFinite(height) || height < 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Window dimensions must be finite and nonnegative.");
        var layouts = settings.Layouts.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var owners = settings.Bindings.GroupBy(b => b.ModelPath).ToDictionary(g => g.Key, g => g.ToArray());
        var roots = owners.ToDictionary(pair => pair.Key,
            pair => pair.Value.Select(b => layouts[("/" + b.Layout.Split('/')[1])]).Distinct().Single());
        var panels = roots.Where(pair => pair.Value.Type == "box-layout").ToDictionary(pair => pair.Key, pair => pair.Value);
        var pages = settings.Bindings.Where(b => b.InspectorModel is not null).ToDictionary(b => b.ModelPath);
        var positions = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var splits = settings.Bindings.Where(binding => layouts[binding.Layout].Type == "split-pane").ToDictionary(binding => binding.ModelPath);
        var bounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var contents = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var borders = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var layoutBounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var layoutContents = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var layoutBorders = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var errors = new List<StationeryLayoutError>();

        ScreenRectangle Inset(ScreenRectangle area, ViewportPadding padding)
        {
            var inset = padding.GetContentBounds(area.Width, area.Height);
            return inset with { X = area.X + inset.X, Y = area.Y + inset.Y };
        }
        ScreenRectangle Cell(StationeryLayoutNode grid, ScreenRectangle area, int row, int col, int rowSpan, int colSpan)
        {
            var rows = TrackEdges(grid.Rows, area.Height);
            var columns = TrackEdges(grid.Columns, area.Width);
            return new(area.X + columns[col], area.Y + rows[row], columns[col + colSpan] - columns[col], rows[row + rowSpan] - rows[row]);
        }
        void ArrangeLayout(StationeryLayoutNode layout, string owner, ScreenRectangle area, ScreenRectangle? rootContent = null)
        {
            var outer = rootContent is not null ? area : Inset(area, layout.Margin);
            var content = rootContent ?? Inset(outer, layout.Padding);
            var key = owner + ":" + layout.Path;
            layoutBounds.Add(key, outer); layoutContents.Add(key, content);
            if (layout.Type == "box-layout") layoutBorders.Add(key, new(outer.X - layout.Border.Left, outer.Y - layout.Border.Top,
                outer.Width + layout.Border.Left + layout.Border.Right, outer.Height + layout.Border.Top + layout.Border.Bottom));
            if (layout.Type == "grid-layout")
                foreach (var binding in owners[owner].Where(b => b.Layout == layout.Path))
                    foreach (var child in binding.Children)
                        positions.Add(child.ModelPath, Inset(Cell(layout, content, child.Row, child.Column, child.RowSpan, child.ColumnSpan), child.Margin));
            if (layout.Type == "dock-layout")
                foreach (var binding in owners[owner].Where(b => b.Layout == layout.Path))
                {
                    var dockContent = content;
                    if (binding.LayoutError is { } error)
                    {
                        var headerHeight = Math.Min(64, content.Height);
                        errors.Add(new(owner, layout.Path, error, content with { Height = headerHeight }));
                        dockContent = content with { Y = content.Y + headerHeight, Height = content.Height - headerHeight };
                    }
                    if (binding.LayoutError is not null)
                    {
                        foreach (var (path, dockArea) in StationeryDockLayout.Arrange(dockContent, binding.DockChildren)) positions[path] = dockArea;
                    }
                    else
                    {
                        var slotBounds = StationeryDockLayout.Arrange(dockContent,
                            layout.Slots.Select(slot => new StationeryDockBinding(slot.Id, slot.Dock!, slot.Size)).ToArray());
                        foreach (var child in binding.DockChildren) positions[child.ModelPath] = Inset(slotBounds[child.Slot!], child.Margin);
                    }
                }
            foreach (var child in layout.Children)
                ArrangeLayout(child, owner, layout.Type == "grid-layout"
                    ? Cell(layout, content, child.Row, child.Column, child.RowSpan, child.ColumnSpan) : content);
        }

        void Visit(StationeryNode node, ScreenRectangle inherited)
        {
            var outer = positions.GetValueOrDefault(node.Path, inherited);
            if (roots.TryGetValue(node.Path, out var rootLayout)) outer = Inset(outer, rootLayout.Margin);
            if (panels.TryGetValue(node.Path, out var box))
            {
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
            if (roots.TryGetValue(node.Path, out var panel))
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
            if (owners.TryGetValue(node.Path, out var ownerBindings))
                foreach (var layout in ownerBindings.Select(b => layouts[("/" + b.Layout.Split('/')[1])]).Distinct())
                    if (layout.Type is "box-layout" or "grid-layout" or "dock-layout") ArrangeLayout(layout, node.Path, outer, content);
            // An unbound dock child must not cover every sibling; other nested bindings still take precedence.
            var inheritedChild = ownerBindings?.Any(b => layouts[b.Layout].Type == "dock-layout") == true
                ? new ScreenRectangle(content.X, content.Y, 0, 0) : content;
            foreach (var child in node.Children) Visit(child, inheritedChild);
        }

        foreach (var model in settings.Models) Visit(model.CreateTree(), new(0, 0, width, height));
        return new(new ReadOnlyDictionary<string, ScreenRectangle>(bounds), new ReadOnlyDictionary<string, ScreenRectangle>(contents))
        { Errors = errors.AsReadOnly(), BorderBounds = new ReadOnlyDictionary<string, ScreenRectangle>(borders),
            LayoutBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutBounds),
            LayoutContentBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutContents),
            LayoutBorderBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutBorders) };
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
