namespace StationeryUI.Styling;

using System.Collections.ObjectModel;
using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Controls;

public sealed record StationeryLayoutResult(IReadOnlyDictionary<string, ScreenRectangle> Bounds,
    IReadOnlyDictionary<string, ScreenRectangle> ContentBounds)
{
    /// <summary>Allocated model rectangles before model and root-layout margins.</summary>
    public IReadOnlyDictionary<string, ScreenRectangle> MarginBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    /// <summary>Allocated layout rectangles before each layout's own margin.</summary>
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutMarginBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyList<StationeryLayoutError> Errors { get; init; } = [];
    // Keyed by owner model path + ":" + complete layout path (layouts are reusable).
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutContentBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> LayoutBorderBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    public IReadOnlyDictionary<string, ScreenRectangle> BorderBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    /// <summary>Bounds selected through bindingsV2 control handles.</summary>
    public IReadOnlyDictionary<string, ScreenRectangle> ControlBounds { get; init; } = new Dictionary<string, ScreenRectangle>();
    /// <summary>The selected bindingsV2 path for each resolved control handle.</summary>
    public IReadOnlyDictionary<string, string> ControlLayoutPaths { get; init; } = new Dictionary<string, string>();
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
    /// <summary>
    /// Arranges the existing model/layout bindings and resolves any bindingsV2 entries.
    /// For controls with keyed paths, controlLayoutKeys supplies the selected LayoutKey per handle.
    /// </summary>
    public static StationeryLayoutResult Arrange(StationeryStyleSettings settings, double width, double height,
        IReadOnlyDictionary<string, string>? controlLayoutKeys = null)
    {
        if (!double.IsFinite(width) || width < 0 || !double.IsFinite(height) || height < 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Window dimensions must be finite and nonnegative.");
        var modelMargins = settings.GetModelMargins();
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
        var marginBounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var layoutMarginBounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
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
        void ArrangeLayout(StationeryLayoutNode layout, string owner, ScreenRectangle area, ScreenRectangle? rootContent = null, ScreenRectangle? rootAllocation = null)
        {
            var outer = rootContent is not null ? area : Inset(area, layout.Margin);
            var content = rootContent ?? Inset(outer, layout.Padding);
            var key = owner + ":" + layout.Path;
            layoutMarginBounds.Add(key, rootAllocation ?? area);
            layoutBounds.Add(key, outer); layoutContents.Add(key, content);
            if (layout.Type == "box-layout") layoutBorders.Add(key, new(outer.X - layout.Border.Left, outer.Y - layout.Border.Top,
                outer.Width + layout.Border.Left + layout.Border.Right, outer.Height + layout.Border.Top + layout.Border.Bottom));
            if (layout.Type == "grid-layout")
                foreach (var binding in owners[owner].Where(b => b.Layout == layout.Path))
                    foreach (var child in binding.Children)
                        positions.Add(child.ModelPath, Cell(layout, content, child.Row, child.Column, child.RowSpan, child.ColumnSpan));
            if (layout.Type == "box-layout")
                foreach (var binding in owners[owner].Where(b => b.Layout == layout.Path))
                    foreach (var child in binding.Children)
                        positions[child.ModelPath] = content;
            if (layout.Type == "tabbed-box-layout")
                foreach (var binding in owners[owner].Where(b => b.Layout == layout.Path))
                {
                    if ((uint)layout.SelectedTabIndex >= (uint)binding.Children.Count)
                        throw new InvalidOperationException($"{layout.Path}.SelectedTabIndex {layout.SelectedTabIndex} is outside the {binding.Children.Count} bound tabs.");
                    for (var index = 0; index < binding.Children.Count; index++)
                    {
                        var child = binding.Children[index];
                        positions[child.ModelPath] = index == layout.SelectedTabIndex
                            ? content : new ScreenRectangle(content.X, content.Y, 0, 0);
                    }
                }
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
                        var cellBounds = StationeryDockLayout.Arrange(dockContent,
                            layout.Cells.Select((cell, index) => new StationeryDockBinding(index.ToString(System.Globalization.CultureInfo.InvariantCulture), cell.Dock!, cell.Size)).ToArray());
                        foreach (var (cell, index) in layout.Cells.Select((cell, index) => (cell, index)))
                            foreach (var child in binding.DockChildren.Where(child => child.CellIndex == index))
                                positions[child.ModelPath] = cellBounds[index.ToString(System.Globalization.CultureInfo.InvariantCulture)];
                    }
                }
            foreach (var child in layout.Children)
                ArrangeLayout(child, owner, layout.Type == "grid-layout"
                    ? Cell(layout, content, child.Row, child.Column, child.RowSpan, child.ColumnSpan) : content);
        }

        void Visit(StationeryNode node, ScreenRectangle inherited)
        {
            var allocation = positions.GetValueOrDefault(node.Path, inherited);
            marginBounds.Add(node.Path, allocation);
            var outer = Inset(allocation, modelMargins[node.Path]);
            var rootAllocation = outer;
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
            if (roots.TryGetValue(node.Path, out var panel) && (panel.Type is "box-layout" or "tabbed-box-layout"))
            {
                var inset = panel.Padding.GetContentBounds(content.Width, content.Height);
                content = inset with { X = outer.X + inset.X, Y = outer.Y + inset.Y };
            }
            // A text/link component that owns a box layout is drawn inside that
            // layout's padding. Its model bounds therefore represent the padded
            // component area, while the layout metadata retains the outer frame.
            if (panels.ContainsKey(node.Path) && node.Kind is "textBlock" or "link")
                bounds[node.Path] = content;
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
                    if (layout.Type is "box-layout" or "grid-layout" or "dock-layout" or "tabbed-box-layout") ArrangeLayout(layout, node.Path, outer, content, rootAllocation);
            // An unbound dock child must not cover every sibling; other nested bindings still take precedence.
            var inheritedChild = ownerBindings?.Any(b => layouts[b.Layout].Type == "dock-layout") == true
                ? new ScreenRectangle(content.X, content.Y, 0, 0) : content;
            foreach (var child in node.Children) Visit(child, inheritedChild);
        }

        foreach (var model in settings.Models) Visit(model.CreateTree(), new(0, 0, width, height));
        var controlBounds = new Dictionary<string, ScreenRectangle>(StringComparer.Ordinal);
        var controlLayoutPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        if (settings.BindingsV2.Count > 0)
        {
            var modelPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            var modelParents = new Dictionary<string, string?>(StringComparer.Ordinal);
            void IndexModels(StationeryModelNode model, string parent)
            {
                var path = parent + "/" + model.Id;
                modelPaths.Add(path, model.Id);
                modelParents.Add(path, parent.Length == 0 ? null : parent);
                foreach (var child in model.Children) IndexModels(child, path);
            }
            foreach (var model in settings.Models) IndexModels(model, "");
            var routes = new Dictionary<string, string>(StringComparer.Ordinal);
            string RouteFor(string modelPath)
            {
                if (routes.TryGetValue(modelPath, out var cached)) return cached;
                var parentPath = modelParents[modelPath];
                if (parentPath is null) return routes[modelPath] = "root";
                var parentRoute = RouteFor(parentPath);
                var namedParentRoute = parentRoute + ":" + modelPaths[parentPath];
                var placementRoute = PlacementRoute(parentPath, modelPath);
                return routes[modelPath] = namedParentRoute + "/" + placementRoute;
            }

            string PlacementRoute(string parentPath, string childPath)
            {
                foreach (var placement in settings.Bindings.Where(binding => binding.ModelPath == parentPath))
                {
                    var layout = layouts[placement.Layout];
                    string? edge = null;
                    if (placement.Children.FirstOrDefault(child => child.ModelPath == childPath) is { } child)
                    {
                        if (layout.Type == "box-layout") edge = "single";
                        if (layout.Type == "grid-layout")
                            edge = $"{child.Row + 1}y.{child.Column + 1}x.{child.RowSpan}w.{child.ColumnSpan}h";
                        if (layout.Type == "tabbed-box-layout")
                            edge = child.Row.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (placement.FirstModel == childPath) edge = "first";
                    if (placement.SecondModel == childPath) edge = "second";
                    if (placement.InspectorModel == childPath) edge = "bottom";
                    var dockChild = placement.DockChildren.FirstOrDefault(child => child.ModelPath == childPath);
                    if (dockChild is not null) edge = dockChild.Dock;
                    if (edge is not null)
                    {
                        var nestedLayouts = NestedLayoutRoute(parentPath, placement.Layout);
                        return nestedLayouts.Length == 0 ? edge : nestedLayouts + "/" + edge;
                    }
                }
                throw new InvalidOperationException($"No legacy bindings placement was found for model '{childPath}'.");
            }

            string NestedLayoutRoute(string ownerPath, string boundLayoutPath)
            {
                var rootLayoutPath = settings.Bindings.Where(binding => binding.ModelPath == ownerPath)
                    .Select(binding => binding.Layout)
                    .Where(path => boundLayoutPath == path || boundLayoutPath.StartsWith(path + "/", StringComparison.Ordinal))
                    .OrderBy(path => path.Length).FirstOrDefault()
                    ?? throw new InvalidOperationException($"No root layout is bound to '{ownerPath}'.");
                if (rootLayoutPath == boundLayoutPath) return "";
                var nested = new List<StationeryLayoutNode>();
                var current = layouts[boundLayoutPath];
                while (current.Path != rootLayoutPath)
                {
                    nested.Add(current);
                    current = current.ParentPath is { } parentPath && layouts.TryGetValue(parentPath, out var parentLayout)
                        ? parentLayout : throw new InvalidOperationException($"Layout '{current.Path}' is not nested under '{rootLayoutPath}'.");
                }
                nested.Reverse();
                var segments = new List<string>(nested.Count);
                foreach (var childLayout in nested)
                {
                    var parentLayout = layouts[childLayout.ParentPath!];
                    var edge = parentLayout.Type switch
                    {
                        "box-layout" => "single",
                        "grid-layout" => $"{childLayout.Row + 1}y.{childLayout.Column + 1}x.{childLayout.RowSpan}w.{childLayout.ColumnSpan}h",
                        _ => throw new InvalidOperationException($"Nested layout '{childLayout.Path}' is not placed by a supported parent layout.")
                    };
                    segments.Add(edge + ":" + childLayout.Id);
                }
                return string.Join('/', segments);
            }

            foreach (var (handle, binding) in settings.BindingsV2)
            {
                string? key = null;
                if (binding.LayoutPath is null)
                {
                    if (controlLayoutKeys is null || !controlLayoutKeys.TryGetValue(handle, out key))
                        throw new InvalidOperationException($"Control '{handle}' requires a LayoutKey.");
                }
                var selectedPath = binding.ResolveLayoutPath(key);
                var modelId = ModelIdForHandle(handle);
                // Route only the model named by this control handle. Trying every model
                // makes unplaced/unsupported models throw during normal path lookup;
                // although MatchesRoute catches those exceptions, debuggers configured
                // to break on thrown InvalidOperationException still stop on every frame.
                var matches = modelPaths.Where(pair => pair.Value == modelId)
                    .Select(pair => pair.Key)
                    .Where(path => MatchesRoute(selectedPath, path)).ToArray();
                if (matches.Length != 1)
                {
                    var candidates = modelPaths.Keys.Where(path => modelPaths[path] == modelId)
                        .Select(path =>
                        {
                            try { return $"{path} => {RouteFor(path)}"; }
                            catch (InvalidOperationException exception) { return $"{path} => <{exception.Message}>"; }
                        });
                    throw new InvalidOperationException($"bindingsV2 path for '{handle}' resolves to {matches.Length} current model placements; the path must match exactly one placement. Selected path: '{selectedPath}'. Candidate routes: {string.Join("; ", candidates)}.");
                }
                if (!bounds.TryGetValue(matches[0], out var controlBoundsForHandle))
                    throw new InvalidOperationException($"No arranged bounds exist for control '{handle}' at {matches[0]}.");
                controlBounds.Add(handle, controlBoundsForHandle);
                controlLayoutPaths.Add(handle, selectedPath);
            }

            bool MatchesRoute(string selectedPath, string modelPath)
            {
                try { return string.Equals(selectedPath, RouteFor(modelPath), StringComparison.Ordinal); }
                catch (InvalidOperationException) { return false; }
            }
        }

        return new(new ReadOnlyDictionary<string, ScreenRectangle>(bounds), new ReadOnlyDictionary<string, ScreenRectangle>(contents))
        { Errors = errors.AsReadOnly(), BorderBounds = new ReadOnlyDictionary<string, ScreenRectangle>(borders),
            MarginBounds = new ReadOnlyDictionary<string, ScreenRectangle>(marginBounds),
            LayoutMarginBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutMarginBounds),
            LayoutBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutBounds),
            LayoutContentBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutContents),
            LayoutBorderBounds = new ReadOnlyDictionary<string, ScreenRectangle>(layoutBorders),
            ControlBounds = new ReadOnlyDictionary<string, ScreenRectangle>(controlBounds),
            ControlLayoutPaths = new ReadOnlyDictionary<string, string>(controlLayoutPaths) };

        static string ModelIdForHandle(string handle)
        {
            var suffix = handle.StartsWith("ctrl", StringComparison.Ordinal) ? handle[4..] : handle;
            if (suffix.Length == 0) return suffix;
            return char.ToLowerInvariant(suffix[0]) + suffix[1..];
        }

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
