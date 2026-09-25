namespace StationeryUI.Styling;

using System.Globalization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Text.Json;
using StationeryUI.Canvas;
using StationeryUI.Controls;

/// <summary>Viewport padding in window pixels, independent of the UI zoom.</summary>
public readonly record struct ViewportPadding(double Top, double Right, double Bottom, double Left)
{
    public ScreenRectangle GetContentBounds(double width, double height)
    {
        var x = Math.Min(Left, Math.Max(0, width));
        var y = Math.Min(Top, Math.Max(0, height));
        return new(x, y, Math.Max(0, width - x - Right), Math.Max(0, height - y - Bottom));
    }
}

public sealed record StationeryModelNode(string Id, string Type, IReadOnlyList<StationeryModelNode> Children)
{
    public ViewportPadding Margin { get; init; }

    public StationeryUI.Inspection.StationeryNode CreateTree()
    {
        var root = new StationeryUI.Inspection.StationeryNode(Id, Type);
        AddChildren(root, Children);
        return root;
    }

    private static void AddChildren(StationeryUI.Inspection.StationeryNode parent, IReadOnlyList<StationeryModelNode> children)
    {
        foreach (var child in children) AddChildren(parent.AddChild(child.Id, child.Type), child.Children);
    }
}

/// <summary>A node in the configured viewport and view snapshot trees.</summary>
public sealed class ViewNode(string type, string name)
{
    public string Type { get; set; } = type;
    public string Name { get; set; } = name;
    public string? Style { get; set; }
    public List<ViewNode> ChildNodes { get; } = [];
}

/// <summary>A named style definition from the style settings file.</summary>
public sealed record StationeryViewStyle(string Name, string Type, JsonElement Definition);

/// <summary>Creates the conventional control handle for a model Id.</summary>
public static class StationeryControlHandle
{
    /// <summary>Returns <c>ctrl</c> plus the model stem, removing a leading <c>mdl</c> when present.</summary>
    public static string FromModelId(string modelId)
    {
        var stem = ModelStemFromId(modelId);
        return "ctrl" + char.ToUpperInvariant(stem[0]) + stem[1..];
    }

    /// <summary>Returns the model Id without its conventional <c>mdl</c> prefix, when present.</summary>
    public static string ModelStemFromId(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        return modelId.StartsWith("mdl", StringComparison.Ordinal) && modelId.Length > 3
            ? modelId[3..] : modelId;
    }

    /// <summary>Returns the lower-camel role name corresponding to a model Id.</summary>
    public static string ModelRoleFromId(string modelId)
    {
        var stem = ModelStemFromId(modelId);
        return char.ToLowerInvariant(stem[0]) + stem[1..];
    }
}

public readonly record struct LayoutTrack(double Value, bool IsRate);

/// <summary>A reusable layout definition. It has no reference to model identities.</summary>
public sealed record StationeryLayoutNode(string Id, string Type, ViewportPadding Padding,
    IReadOnlyList<LayoutTrack> Rows, IReadOnlyList<LayoutTrack> Columns, SplitPaneOptions? Split = null, double InspectorHeight = 0,
    ViewportPadding Margin = default, ViewportPadding Border = default)
{
    public IReadOnlyList<StationeryLayoutCell> Cells { get; init; } = [];
    public string? CellError { get; init; }
    public string Path { get; init; } = "/" + Id;
    public string? ParentPath { get; init; }
    public int Row { get; init; }
    public int Column { get; init; }
    public int RowSpan { get; init; } = 1;
    public int ColumnSpan { get; init; } = 1;
    public IReadOnlyList<StationeryLayoutNode> Children { get; init; } = [];
    /// <summary>Runtime-selected zero-based child for tabbed-box-layout.</summary>
    public int SelectedTabIndex { get; set; }
}
/// <summary>A name for a cell, with no geometry or spacing of its own.</summary>
public sealed record StationeryLayoutSlot(string Id);
public sealed record StationeryLayoutCell(int Row = 0, int Column = 0, int RowSpan = 1, int ColumnSpan = 1,
    string? Dock = null, double Size = 0)
{
    public IReadOnlyList<StationeryLayoutSlot> Slots { get; init; } = [];
}
public sealed record StationeryCellBinding(string ModelPath, int Row, int Column, int RowSpan = 1, int ColumnSpan = 1);
public sealed record StationeryDockBinding(string ModelPath, string Dock, double Size = 0)
{ public string? Slot { get; init; } public int CellIndex { get; init; } = -1; }
/// <summary>References are resolved to canonical model paths when a complete settings snapshot is parsed.</summary>
public sealed record StationeryLayoutBinding(string Layout, string ModelPath, IReadOnlyList<StationeryCellBinding> Children,
    string? FirstModel = null, string? SecondModel = null, string? InspectorModel = null)
{
    public IReadOnlyList<StationeryDockBinding> DockChildren { get; init; } = [];
    public string? LayoutError { get; init; }
}

/// <summary>A control handle mapped either to one layout path or to paths selected by layout key.</summary>
public sealed record StationeryControlBinding(string? LayoutPath, IReadOnlyDictionary<string, string> LayoutPaths)
{
    /// <summary>Explicit model path paired with the layout path.</summary>
    public string? ModelReference { get; init; }
    /// <summary>Explicit model paths paired with each keyed layout path.</summary>
    public IReadOnlyDictionary<string, string> ModelReferences { get; init; }
        = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
    /// <summary>Model path resolved from the layout route when the settings snapshot is parsed.</summary>
    public string? ModelPath { get; init; }
    /// <summary>Model paths resolved for each layout key when the settings snapshot is parsed.</summary>
    public IReadOnlyDictionary<string, string> ModelPaths { get; init; }
        = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    public string ResolveLayoutPath(string? layoutKey = null)
    {
        if (LayoutPath is not null) return LayoutPath;
        if (layoutKey is null && LayoutPaths.Count == 1) return LayoutPaths.Values.Single();
        if (layoutKey is null) throw new ArgumentNullException(nameof(layoutKey), "A layout key is required for this control binding.");
        return LayoutPaths.TryGetValue(layoutKey, out var path) ? path
            : throw new KeyNotFoundException($"Unknown layout key '{layoutKey}'.");
    }

    public string ResolveModelPath(string? layoutKey = null)
    {
        if (LayoutPath is not null) return ModelPath ?? ModelReference
            ?? throw new InvalidOperationException("The controlTree layout path has not been resolved to a model.");
        if (layoutKey is null && ModelPaths.Count == 1) return ModelPaths.Values.Single();
        if (layoutKey is null) throw new ArgumentNullException(nameof(layoutKey), "A layout key is required for this control binding.");
        return ModelPaths.TryGetValue(layoutKey, out var path) ? path
            : ModelReferences.TryGetValue(layoutKey, out path) ? path
            : throw new KeyNotFoundException($"Unknown layout key '{layoutKey}'.");
    }
}

/// <summary>Resolves serialized control routes once, while a complete settings snapshot is parsed.</summary>
internal static class StationeryControlBindingResolver
{
    private sealed class PlacementAccumulator(string layout, string model)
    {
        public string Layout { get; } = layout;
        public string Model { get; } = model;
        public List<StationeryCellBinding> Children { get; } = [];
        public List<StationeryDockBinding> DockChildren { get; } = [];
        public string? First { get; set; }
        public string? Second { get; set; }
        public string? Inspector { get; set; }
    }

    public static IReadOnlyList<StationeryLayoutBinding> ResolvePlacementsFromRoutes(StationeryModelNode model,
        IReadOnlyList<StationeryLayoutNode> layoutNodes, IReadOnlyDictionary<string, StationeryControlBinding> bindings)
    {
        var root = model.CreateTree();
        var layouts = layoutNodes.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var accumulators = new Dictionary<(string Model, string Layout), PlacementAccumulator>();
        var assignedModels = new Dictionary<(string Model, string Layout, string Child), string>();

        foreach (var binding in bindings.Values)
        foreach (var (modelReference, route) in Routes(binding))
        {
            var segments = route.Split('/');
            if (segments.Length == 0 || !segments[0].StartsWith("root:", StringComparison.Ordinal))
                throw new JsonException($"controlTree layoutPath '{route}' must start with root:<layoutId>.");
            var target = root.Resolve(modelReference)
                ?? throw new JsonException($"controlTree modelPath '{modelReference}' does not identify a model.");
            var lineage = new Stack<StationeryUI.Inspection.StationeryNode>();
            for (var node = target; node is not null; node = node.Parent!) lineage.Push(node);
            var modelLineage = lineage.ToArray();
            var rootLayoutId = segments[0][5..];
            var owner = root;
            var activeLayout = ResolveRootLayout(rootLayoutId, route);
            EnsurePlacement(owner, activeLayout);

            var nextModelIndex = 1;
            foreach (var segment in segments.Skip(1))
            {
                var colon = segment.IndexOf(':');
                var edge = colon < 0 ? segment : segment[..colon];
                var nextName = colon < 0 ? null : segment[(colon + 1)..];
                if (nextName is null)
                {
                    if (nextModelIndex >= modelLineage.Length || modelLineage[nextModelIndex].Parent != owner)
                        throw new JsonException($"controlTree layoutPath '{route}' does not lead to modelPath '{modelReference}'.");
                    AddPlacement(owner, modelLineage[nextModelIndex++], activeLayout
                        ?? throw new JsonException($"controlTree layoutPath '{route}' has no active layout."), edge, route);
                    continue;
                }

                var nextModel = nextModelIndex < modelLineage.Length ? modelLineage[nextModelIndex] : null;
                if (nextModel?.Parent == owner && RootLayoutExists(nextName))
                {
                    AddPlacement(owner, nextModel, activeLayout
                        ?? throw new JsonException($"controlTree layoutPath '{route}' has no active layout."), edge, route);
                    owner = nextModel;
                    nextModelIndex++;
                    activeLayout = ResolveRootLayout(nextName, route);
                    EnsurePlacement(owner, activeLayout);
                    continue;
                }

                if (activeLayout is null || !layouts.TryGetValue(activeLayout.Path, out var currentLayout))
                    throw new JsonException($"controlTree layoutPath '{route}' references unknown nested layout '{nextName}'.");
                var nested = currentLayout.Children.FirstOrDefault(child => child.Id == nextName);
                if (nested is null || !MatchesLayoutCell(edge, currentLayout, nested))
                    throw new JsonException($"controlTree layoutPath '{route}' does not match nested layout '{nextName}'.");
                activeLayout = nested;
            }
            if (nextModelIndex != modelLineage.Length)
                throw new JsonException($"controlTree layoutPath '{route}' stops before modelPath '{modelReference}'.");
        }

        return accumulators.Values.Select(item => new StationeryLayoutBinding(item.Layout, item.Model,
            item.Children.AsReadOnly(), item.First, item.Second, item.Inspector)
        { DockChildren = item.DockChildren.AsReadOnly() }).ToArray();

        StationeryLayoutNode? ResolveRootLayout(string? id, string route)
        {
            if (id is null) throw new JsonException($"controlTree route '{route}' must include a root layout ID for every model.");
            var candidates = layoutNodes.Where(layout => layout.ParentPath is null && layout.Id == id).ToArray();
            return candidates.Length == 1 ? candidates[0]
                : throw new JsonException($"controlTree route '{route}' root layout ID '{id}' resolves to {candidates.Length} layouts.");
        }

        bool RootLayoutExists(string id) => layoutNodes.Any(layout => layout.ParentPath is null && layout.Id == id);

        static IEnumerable<(string ModelPath, string Route)> Routes(StationeryControlBinding binding)
        {
            if (binding.LayoutPath is { } path)
            {
                if (binding.ModelReference is null) throw new JsonException("Each controlTree control requires modelPath.");
                yield return (binding.ModelReference, path);
                yield break;
            }
            foreach (var (key, route) in binding.LayoutPaths)
            {
                if (!binding.ModelReferences.TryGetValue(key, out var modelPath))
                    throw new JsonException($"controlTree layout key '{key}' requires modelPath.");
                yield return (modelPath, route);
            }
        }

        void AddPlacement(StationeryUI.Inspection.StationeryNode parent, StationeryUI.Inspection.StationeryNode child,
            StationeryLayoutNode layout, string edge, string route)
        {
            var key = (parent.Path, layout.Path);
            if (!accumulators.TryGetValue(key, out var accumulator))
                accumulators.Add(key, accumulator = new(layout.Path, parent.Path));
            var childKey = (parent.Path, layout.Path, child.Path);
            if (assignedModels.TryGetValue(childKey, out var previous))
            {
                if (previous != edge) throw new JsonException($"Model '{child.Path}' has conflicting controlTree routes.");
                return;
            }
            assignedModels.Add(childKey, edge);
            if (layout.Type == "grid-layout" && ParseGridEdge(edge) is { } grid)
            {
                accumulator.Children.Add(new(child.Path, grid.Row, grid.Column, grid.RowSpan, grid.ColumnSpan));
                return;
            }
            if (layout.Type == "tabbed-box-layout" && int.TryParse(edge, NumberStyles.None, CultureInfo.InvariantCulture, out var tab))
            {
                accumulator.Children.Add(new(child.Path, tab, 0));
                return;
            }
            if (layout.Type == "box-layout" && edge == "single")
            {
                accumulator.Children.Add(new(child.Path, 0, 0));
                return;
            }
            if (layout.Type == "dock-layout")
            {
                var direction = edge;
                int? cellIndex = null;
                var separator = edge.LastIndexOf('.');
                if (separator > 0 && int.TryParse(edge[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var oneBasedIndex))
                { direction = edge[..separator]; cellIndex = oneBasedIndex - 1; }
                var matching = layout.Cells.Select((cell, index) => (cell, index))
                    .Where(item => item.cell.Dock == direction && (cellIndex is null || item.index == cellIndex)).ToArray();
                if (matching.Length != 1) throw new JsonException($"controlTree route '{route}' dock edge '{edge}' resolves to {matching.Length} cells in {layout.Path}.");
                var item = matching[0];
                accumulator.DockChildren.Add(new(child.Path, direction, item.cell.Size) { CellIndex = item.index });
                return;
            }
            if (layout.Type == "split-pane" && (edge is "first" or "second"))
            {
                if (edge == "first") accumulator.First = child.Path; else accumulator.Second = child.Path;
                return;
            }
            if ((layout.Type is "fullscreen-layout" or "work-page-layout") && edge == "bottom")
            {
                accumulator.Inspector = child.Path;
                return;
            }
            throw new JsonException($"controlTree route '{route}' edge '{edge}' is invalid for {layout.Type} at {layout.Path}.");
        }

        void EnsurePlacement(StationeryUI.Inspection.StationeryNode owner, StationeryLayoutNode? layout)
        {
            if (layout is null) return;
            var key = (owner.Path, layout.Path);
            if (!accumulators.ContainsKey(key)) accumulators.Add(key, new(layout.Path, owner.Path));
        }

        static (int Row, int Column, int RowSpan, int ColumnSpan)? ParseGridEdge(string edge)
        {
            var values = Regex.Matches(edge, "[0-9]+").Cast<Match>()
                .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            if (values.Length != 4 || !edge.Contains('y') || !edge.Contains('x') || !edge.Contains('w') || !edge.Contains('h')) return null;
            return (values[0] - 1, values[1] - 1, values[2], values[3]);
        }

        static bool MatchesLayoutCell(string edge, StationeryLayoutNode ownerLayout, StationeryLayoutNode childLayout)
        {
            if (ownerLayout.Type == "box-layout") return edge == "single";
            if (ownerLayout.Type != "grid-layout" || ParseGridEdge(edge) is not { } grid) return false;
            return childLayout.Row == grid.Row && childLayout.Column == grid.Column
                && childLayout.RowSpan == grid.RowSpan && childLayout.ColumnSpan == grid.ColumnSpan;
        }
    }

    public static IReadOnlyDictionary<string, StationeryControlBinding> Resolve(StationeryModelNode root,
        IReadOnlyList<StationeryLayoutNode> layoutNodes, IReadOnlyList<StationeryLayoutBinding> placements,
        IReadOnlyDictionary<string, StationeryControlBinding> bindings)
    {
        var layouts = layoutNodes.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var modelIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var modelParents = new Dictionary<string, string?>(StringComparer.Ordinal);
        void Index(StationeryModelNode model, string parent)
        {
            var path = parent + "/" + model.Id;
            modelIds.Add(path, model.Id);
            modelParents.Add(path, parent.Length == 0 ? null : parent);
            foreach (var child in model.Children) Index(child, path);
        }
        Index(root, "");

        var routeCache = new Dictionary<(string ModelPath, bool IncludeLayoutIds), string?>();
        bool TryRoute(string modelPath, out string route, bool includeLayoutIds = false)
        {
            var cacheKey = (modelPath, includeLayoutIds);
            if (routeCache.TryGetValue(cacheKey, out var cached))
            {
                route = cached ?? "";
                return cached is not null;
            }
            var parentPath = modelParents[modelPath];
            if (parentPath is null)
            {
                route = includeLayoutIds
                    ? $"root:{placements.Where(binding => binding.ModelPath == modelPath).Select(binding => RootLayoutId(binding.Layout)).Distinct(StringComparer.Ordinal).Single()}"
                    : "root";
                routeCache[cacheKey] = route;
                return true;
            }
            if (!TryRoute(parentPath, out var parentRoute, includeLayoutIds) || !TryPlacementRoute(parentPath, modelPath, out var placementRoute, includeLayoutIds))
            {
                routeCache[cacheKey] = null;
                route = "";
                return false;
            }
            var layoutId = includeLayoutIds
                ? placements.Where(binding => binding.ModelPath == modelPath)
                    .Select(binding => RootLayoutId(binding.Layout))
                    .Distinct(StringComparer.Ordinal).SingleOrDefault()
                : null;
            route = parentRoute + "/" + placementRoute + (includeLayoutIds
                ? (layoutId is null ? "" : ":" + layoutId)
                : "");
            routeCache[cacheKey] = route;
            return true;
        }

        string RootLayoutId(string layoutPath)
        {
            var layout = layouts[layoutPath];
            while (layout.ParentPath is { } parentPath) layout = layouts[parentPath];
            return layout.Id;
        }

        bool TryPlacementRoute(string parentPath, string childPath, out string route, bool includeLayoutIds)
        {
            foreach (var placement in placements.Where(binding => binding.ModelPath == parentPath))
            {
                var layout = layouts[placement.Layout];
                string? edge = null;
                if (placement.Children.FirstOrDefault(child => child.ModelPath == childPath) is { } child)
                {
                    if (layout.Type == "box-layout") edge = "single";
                    if (layout.Type == "grid-layout")
                        edge = $"{child.Row + 1}y.{child.Column + 1}x.{child.RowSpan}w.{child.ColumnSpan}h";
                    if (layout.Type == "tabbed-box-layout")
                        edge = child.Row.ToString(CultureInfo.InvariantCulture);
                }
                if (placement.FirstModel == childPath) edge = "first";
                if (placement.SecondModel == childPath) edge = "second";
                if (placement.InspectorModel == childPath) edge = "bottom";
                var dockChild = placement.DockChildren.FirstOrDefault(item => item.ModelPath == childPath);
                if (dockChild is not null) edge = includeLayoutIds && dockChild.CellIndex >= 0
                    ? $"{dockChild.Dock}.{dockChild.CellIndex + 1}" : dockChild.Dock;
                if (edge is null) continue;
                if (!TryNestedLayoutRoute(parentPath, placement.Layout, out var nested)) continue;
                route = nested.Length == 0 ? edge : nested + "/" + edge;
                return true;
            }
            route = "";
            return false;
        }

        bool TryNestedLayoutRoute(string ownerPath, string boundLayoutPath, out string route)
        {
            var rootLayoutPath = placements.Where(binding => binding.ModelPath == ownerPath)
                .Select(binding => binding.Layout)
                .Where(path => boundLayoutPath == path || boundLayoutPath.StartsWith(path + "/", StringComparison.Ordinal))
                .OrderBy(path => path.Length).FirstOrDefault();
            if (rootLayoutPath is null)
            {
                route = "";
                return false;
            }
            if (rootLayoutPath == boundLayoutPath)
            {
                route = "";
                return true;
            }
            var nested = new List<StationeryLayoutNode>();
            var current = layouts[boundLayoutPath];
            while (current.Path != rootLayoutPath)
            {
                nested.Add(current);
                if (current.ParentPath is not { } parentPath || !layouts.TryGetValue(parentPath, out current!))
                {
                    route = "";
                    return false;
                }
            }
            nested.Reverse();
            var segments = new List<string>(nested.Count);
            foreach (var childLayout in nested)
            {
                var parentLayout = layouts[childLayout.ParentPath!];
                string edge;
                if (parentLayout.Type == "box-layout") edge = "single";
                else if (parentLayout.Type == "grid-layout")
                    edge = $"{childLayout.Row + 1}y.{childLayout.Column + 1}x.{childLayout.RowSpan}w.{childLayout.ColumnSpan}h";
                else
                {
                    route = "";
                    return false;
                }
                segments.Add(edge + ":" + childLayout.Id);
            }
            route = string.Join('/', segments);
            return true;
        }

        var resolved = new Dictionary<string, StationeryControlBinding>(StringComparer.Ordinal);
        foreach (var (handle, binding) in bindings)
        {
            var resolvedByKey = new Dictionary<string, string>(StringComparer.Ordinal);
            if (binding.LayoutPath is { } singlePath)
            {
                var modelPath = ResolvePath(handle, binding.ModelReference!, singlePath);
                resolved.Add(handle, binding with { ModelPath = modelPath });
                continue;
            }
            foreach (var (key, selectedPath) in binding.LayoutPaths)
                resolvedByKey.Add(key, ResolvePath(handle, binding.ModelReferences[key], selectedPath));
            resolved.Add(handle, binding with { ModelPaths = new ReadOnlyDictionary<string, string>(resolvedByKey) });
        }
        return new ReadOnlyDictionary<string, StationeryControlBinding>(resolved);

        string ResolvePath(string handle, string modelReference, string selectedPath)
        {
            if (!modelIds.ContainsKey(modelReference))
                throw new JsonException($"controlTree modelPath for '{handle}' references unknown model '{modelReference}'.");
            if (!TryRoute(modelReference, out var expected, includeLayoutIds: true) ||
                !string.Equals(expected, selectedPath, StringComparison.Ordinal))
                throw new JsonException($"controlTree layoutPath for '{handle}' does not place model '{modelReference}'. Expected '{expected}', got '{selectedPath}'.");
            return modelReference;
        }
    }
}

public sealed record StationeryStyleSettings(StationeryModelNode ModelTree,
    IReadOnlyList<StationeryLayoutNode> Layouts, IReadOnlyList<StationeryLayoutBinding> Bindings)
{
    /// <summary>Root containing the configured initial viewport snapshot nodes.</summary>
    public ViewNode ViewportSnapshotTree { get; init; } = new("ViewportSnapshot", "initialViewportSnapshot");
    /// <summary>Root containing every configured viewport node.</summary>
    public ViewNode ViewportsTree { get; init; } = new("Viewports", "viewports");
    /// <summary>Named view style definitions, keyed by style name.</summary>
    public IReadOnlyDictionary<string, StationeryViewStyle> Styles { get; init; }
        = new ReadOnlyDictionary<string, StationeryViewStyle>(new Dictionary<string, StationeryViewStyle>(StringComparer.Ordinal));
    public IReadOnlyDictionary<string, StationeryControlBinding> ControlTree { get; init; }
        = new ReadOnlyDictionary<string, StationeryControlBinding>(new Dictionary<string, StationeryControlBinding>(StringComparer.Ordinal));

    public string ResolveLayoutPath(string controlHandle, string? layoutKey = null)
        => ControlTree.TryGetValue(controlHandle, out var binding) ? binding.ResolveLayoutPath(layoutKey)
            : throw new KeyNotFoundException($"Unknown control handle '{controlHandle}' in controlTree.");

    public static StationeryStyleSettings Default { get; } = LoadDefault();

    private static StationeryStyleSettings LoadDefault()
    {
        using var stream = typeof(StationeryStyleSettings).Assembly.GetManifestResourceStream("StationeryUI.Styling.Default.stationery-style.json")
            ?? throw new InvalidOperationException("The embedded default stationery style is missing.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    // Convenience for consumers interested only in root padding; never depends on layouts array order.
    public ViewportPadding Padding
    {
        get
        {
            var rootPath = "/" + ModelTree.Id;
            var binding = Bindings.FirstOrDefault(binding => binding.ModelPath == rootPath &&
                Layouts.Any(layout => layout.Path == ("/" + binding.Layout.Split('/')[1]) && layout.Type is "box-layout" or "grid-layout" or "dock-layout" or "tabbed-box-layout"));
            return binding is null ? default : Layouts.Single(layout => layout.Path == ("/" + binding.Layout.Split('/')[1])).Padding;
        }
    }

    public static StationeryStyleSettings Parse(string json)
        => ParseCore(json, false);

    /// <summary>Recovers invalid dock child placements as a vertical list when their parent can be resolved.</summary>
    public static StationeryStyleSettings ParseWithDockFallback(string json)
        => ParseCore(json, true);

    private static StationeryStyleSettings ParseCore(string json, bool recoverDockErrors)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement, "root");
        if (root.TryGetProperty("viewport", out _) || root.TryGetProperty("model", out _) || root.TryGetProperty("layout", out _) ||
            root.TryGetProperty("models", out _) || root.TryGetProperty("bindings", out _) || root.TryGetProperty("bindingsV2", out _))
            throw new JsonException("Use modelTree, layouts and controlTree; legacy models/bindings sections are not supported.");
        if (!root.TryGetProperty("modelTree", out var modelJson)) throw new JsonException("modelTree is required.");

        var styles = new Dictionary<string, StationeryViewStyle>(StringComparer.Ordinal);
        if (root.TryGetProperty("styles", out var stylesJson))
        {
            if (stylesJson.ValueKind != JsonValueKind.Array) throw new JsonException("styles must be an array.");
            foreach (var (item, index) in stylesJson.EnumerateArray().Select((item, index) => (item, index)))
            {
                var path = $"styles[{index}]";
                RequireObject(item, path);
                var name = ReadString(item, "name", path);
                var type = ReadString(item, "type", path);
                if (!styles.TryAdd(name, new(name, type, item.Clone())))
                    throw new JsonException($"{path}: duplicate style name '{name}'.");
            }
        }

        var viewportsRoot = new ViewNode("Viewports", "viewports");
        if (root.TryGetProperty("viewports", out var viewportsJson))
        {
            if (viewportsJson.ValueKind != JsonValueKind.Array)
                throw new JsonException("viewports must be an array.");
            foreach (var (item, index) in viewportsJson.EnumerateArray().Select((item, index) => (item, index)))
                viewportsRoot.ChildNodes.Add(ReadViewNode(item, $"viewports[{index}]", null, styles));
        }

        var viewNodesByName = new Dictionary<string, ViewNode>(StringComparer.Ordinal);
        void IndexViewNodes(ViewNode node)
        {
            viewNodesByName.TryAdd(node.Name, node);
            foreach (var child in node.ChildNodes) IndexViewNodes(child);
        }
        IndexViewNodes(viewportsRoot);

        var snapshotRoot = new ViewNode("ViewportSnapshot", "initialViewportSnapshot");
        if (root.TryGetProperty("initialViewportSnapshot", out var snapshotJson))
        {
            if (snapshotJson.ValueKind != JsonValueKind.Array)
                throw new JsonException("initialViewportSnapshot must be an array.");
            foreach (var (item, index) in snapshotJson.EnumerateArray().Select((item, index) => (item, index)))
                snapshotRoot.ChildNodes.Add(ReadViewNode(item, $"initialViewportSnapshot[{index}]", viewNodesByName, styles));
        }

        var model = ReadModel(modelJson, "modelTree");
        if (model.Type != "viewport") throw new JsonException("modelTree.type must be viewport.");
        var modelTree = model.CreateTree();
        var layouts = new List<StationeryLayoutNode>();
        var layoutIds = new HashSet<string>(StringComparer.Ordinal);
        StationeryLayoutNode ReadLayout(JsonElement item, string? parentPath, string? parentType)
        {
            var path = parentPath ?? "layouts";
            RequireObject(item, path);
            var id = ReadString(item, "id", path);
            ValidateId(id, path);
            path = parentPath is null ? "/" + id : parentPath + "/" + id;
            if (!layoutIds.Add(path)) throw new JsonException($"Duplicate layout Id '{id}'.");
            var type = ReadString(item, "type", path);
            if (type == "floating-layout") type = "grid-layout"; // Legacy JSON spelling.
            if (type == "panel") type = "box-layout";
            if (type is not ("box-layout" or "grid-layout" or "dock-layout" or "tabbed-box-layout" or "split-pane" or "fullscreen-layout" or "work-page-layout")) throw new JsonException($"{path}.type must be box-layout, grid-layout, dock-layout, tabbed-box-layout, split-pane, fullscreen-layout or work-page-layout.");
            if (item.TryGetProperty("contents", out _) ||
                item.TryGetProperty("model", out _) || item.TryGetProperty("parentModel", out _))
                throw new JsonException($"{path}: model references and placement belong in controlTree routes.");
            var padding = default(ViewportPadding);
            var margin = default(ViewportPadding);
            var border = default(ViewportPadding);
            ViewportPadding ReadEdges(string name, JsonElement? source = null)
            {
                if (!(source ?? item).TryGetProperty(name, out var edges)) return default;
                RequireObject(edges, path + "." + name);
                double Side(string side) => edges.TryGetProperty(side, out var v) ? ReadLength(v, path + "." + name + "." + side, false).Value : 0;
                return new(Side("top"), Side("right"), Side("bottom"), Side("left"));
            }
            if (type != "box-layout" && item.TryGetProperty("border", out _))
                throw new JsonException($"{path}: border requires a box-layout.");
            if (type is "box-layout" or "grid-layout" or "dock-layout" or "tabbed-box-layout") margin = ReadEdges("margin");
            else if (item.TryGetProperty("margin", out _)) throw new JsonException($"{path}: margin requires box-layout, grid-layout, dock-layout or tabbed-box-layout.");
            SplitPaneOptions? split = null;
            double inspectorHeight = 0;
            IReadOnlyList<LayoutTrack> rows = Array.Empty<LayoutTrack>(), columns = Array.Empty<LayoutTrack>();
            if (type is "fullscreen-layout" or "work-page-layout")
            {
                if (type == "work-page-layout")
                    inspectorHeight = item.TryGetProperty("inspectorHeight", out var h) ? ReadLength(h, path + ".inspectorHeight", false).Value : 80;
                else if (item.TryGetProperty("inspectorHeight", out _)) throw new JsonException("fullscreen-layout has no inspectorHeight.");
                if (item.TryGetProperty("padding", out _) || item.TryGetProperty("row-definitions", out _) || item.TryGetProperty("column-definitions", out _))
                    throw new JsonException("Page layouts cannot contain padding or track definitions; use a child container for content.");
            }
            else if (type == "dock-layout")
            {
                padding = ReadEdges("padding");
                if (item.TryGetProperty("row-definitions", out _) || item.TryGetProperty("column-definitions", out _))
                    throw new JsonException($"{path}: dock-layout uses dock/size on cells, not track definitions.");
            }
            else if (type is "box-layout" or "tabbed-box-layout")
            {
                margin = ReadEdges("margin");
                if (type == "box-layout") border = ReadEdges("border");
                else if (item.TryGetProperty("border", out _)) throw new JsonException($"{path}: border requires a box-layout.");
                if (item.TryGetProperty("row-definitions", out _) || item.TryGetProperty("column-definitions", out _))
                    throw new JsonException($"{path}: track definitions require grid-layout.");
                padding = new(8, 8, 8, 8);
                if (item.TryGetProperty("padding", out var value))
                {
                    RequireObject(value, path + ".padding");
                    padding = new(ReadPixels(value, "top", path), ReadPixels(value, "right", path),
                        ReadPixels(value, "bottom", path), ReadPixels(value, "left", path));
                }
            }
            else if (type == "split-pane")
            {
                var orientation = ReadString(item, "orientation", path);
                if (orientation is not ("horizontal" or "vertical")) throw new JsonException("orientation must be horizontal or vertical.");
                var ratio = .5;
                if (item.TryGetProperty("ratio", out var ratioJson) &&
                    (ratioJson.ValueKind != JsonValueKind.Number || !ratioJson.TryGetDouble(out ratio))) throw new JsonException("ratio must be a number.");
                var divider = item.TryGetProperty("dividerWidth", out var d) ? ReadLength(d, path + ".dividerWidth", false).Value : 8;
                var minimum = item.TryGetProperty("minimumPaneSize", out var m) ? ReadLength(m, path + ".minimumPaneSize", false).Value : 40;
                split = new(orientation == "horizontal", ratio, divider, minimum);
                try { new SplitPane().Configure(split); } catch (ArgumentException ex) { throw new JsonException("Invalid split-pane options.", ex); }
                if (item.TryGetProperty("padding", out _) || item.TryGetProperty("row-definitions", out _) || item.TryGetProperty("column-definitions", out _))
                    throw new JsonException("split-pane cannot contain padding or track definitions.");
            }
            else
            {
                padding = ReadEdges("padding");
                rows = ReadTracks(item, "row-definitions", path);
                columns = ReadTracks(item, "column-definitions", path);
            }
            if (parentPath is not null && type is not ("box-layout" or "grid-layout" or "dock-layout"))
                throw new JsonException($"{path}: nested layouts must be box-layout, grid-layout or dock-layout.");
            var hasCell = new[] { "row", "col", "column", "rowspan", "colspan" }.Any(k => item.TryGetProperty(k, out _));
            if (hasCell && parentType != "grid-layout") throw new JsonException($"{path}: cell placement requires a grid-layout parent.");
            var row = ReadInteger(item, "row", path, 0, 0);
            var col = ReadColumn(item, path, false);
            var rowSpan = ReadInteger(item, "rowspan", path, 1, 1);
            var colSpan = ReadInteger(item, "colspan", path, 1, 1);
            var children = item.TryGetProperty("children", out _)
                ? ReadArray(item, "children", path).EnumerateArray().Select(c => ReadLayout(c, path, type)).ToArray() : [];
            if (type == "box-layout" && children.Length > 1) throw new JsonException($"{path}: box-layout allows at most one child.");
            if (children.Length > 0 && type is not ("box-layout" or "grid-layout")) throw new JsonException($"{path}: children require box-layout or grid-layout.");
            if (item.TryGetProperty("slots", out _)) throw new JsonException($"{path}: define cells, then give each cell an Id using cells[].slots.");
            var cells = new List<StationeryLayoutCell>();
            string? cellError = null;
            try
            {
                if (item.TryGetProperty("cells", out _) && type is not ("grid-layout" or "dock-layout"))
                    throw new JsonException($"{path}: cells require grid-layout or dock-layout.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var occupied = new List<StationeryCellBinding>();
                if (type == "grid-layout")
                    foreach (var child in children) ValidateCell(new(child.Path, child.Row, child.Column, child.RowSpan, child.ColumnSpan), rows.Count, columns.Count, occupied, path);
                if (item.TryGetProperty("cells", out _))
                    foreach (var cellJson in ReadArray(item, "cells", path).EnumerateArray())
                    {
                        var cellPath = $"{path}.cells[{cells.Count}]";
                        RequireObject(cellJson, cellPath);
                        var allowed = type == "grid-layout"
                            ? new[] { "row", "col", "column", "rowspan", "colspan", "slots" }
                            : new[] { "dock", "size", "slots" };
                        foreach (var property in cellJson.EnumerateObject())
                            if (!allowed.Contains(property.Name)) throw new JsonException($"{cellPath}: '{property.Name}' is not a cell placement field. Put spacing on the contained model's layout.");
                        var names = new List<StationeryLayoutSlot>();
                        if (cellJson.TryGetProperty("slots", out _))
                        {
                            var slotsJson = ReadArray(cellJson, "slots", cellPath);
                            if (slotsJson.GetArrayLength() > 1) throw new JsonException($"{cellPath}: a cell can have at most one slot Id.");
                            foreach (var slot in slotsJson.EnumerateArray())
                            {
                                var slotPath = cellPath + ".slots[0]";
                                RequireObject(slot, slotPath);
                                if (slot.EnumerateObject().Any(p => p.Name != "id")) throw new JsonException($"{slotPath}: slots contain only id; placement belongs on the cell.");
                                var idValue = ReadString(slot, "id", slotPath); ValidateId(idValue, slotPath);
                                if (!ids.Add(idValue)) throw new JsonException($"{slotPath}: duplicate slot '{idValue}'.");
                                names.Add(new(idValue));
                            }
                        }
                        if (type == "grid-layout")
                        {
                            var cell = new StationeryCellBinding(cellPath, ReadIndex(cellJson, "row", cellPath, rows.Count), ReadColumn(cellJson, cellPath, true),
                                ReadInteger(cellJson, "rowspan", cellPath, 1, 1), ReadInteger(cellJson, "colspan", cellPath, 1, 1));
                            ValidateCell(cell, rows.Count, columns.Count, occupied, cellPath);
                            cells.Add(new(cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan) { Slots = names.AsReadOnly() });
                        }
                        else
                        {
                            var dock = ReadString(cellJson, "dock", cellPath);
                            if (dock is not ("top" or "right" or "bottom" or "left" or "center")) throw new JsonException($"{cellPath}.dock must be top, right, bottom, left or center.");
                            var size = ReadString(cellJson, "size", cellPath);
                            double pixels = 0;
                            if (dock == "center")
                            {
                                if (cells.Any(c => c.Dock == "center")) throw new JsonException($"{cellPath}: dock-layout allows at most one center.");
                                if (size != "remaining") throw new JsonException($"{cellPath}.size must be remaining for center.");
                            }
                            else pixels = ReadLength(cellJson.GetProperty("size"), cellPath + ".size", false).Value;
                            cells.Add(new(Dock: dock, Size: pixels) { Slots = names.AsReadOnly() });
                        }
                    }
            }
            catch (JsonException ex) when (recoverDockErrors && type == "dock-layout")
            { cellError = ex.Message; cells.Clear(); }
            return new(id, type, padding, rows, columns, split, inspectorHeight, margin, border)
            { Cells = cells.AsReadOnly(), CellError = cellError, Path = path, ParentPath = parentPath, Row = row, Column = col, RowSpan = rowSpan, ColumnSpan = colSpan, Children = Array.AsReadOnly(children) };
        }
        void Flatten(StationeryLayoutNode layout)
        {
            layouts.Add(layout);
            foreach (var child in layout.Children) Flatten(child);
        }
        foreach (var item in ReadArray(root, "layouts", "root").EnumerateArray()) Flatten(ReadLayout(item, null, null));
        var bindings = new List<StationeryLayoutBinding>();
        var panels = new HashSet<string>(StringComparer.Ordinal);
        var grids = new HashSet<string>(StringComparer.Ordinal);
        var placedModels = new HashSet<string>(StringComparer.Ordinal);
        var pages = new HashSet<string>(StringComparer.Ordinal);
        var splits = new HashSet<string>(StringComparer.Ordinal);
        var legacyBindingItems = Array.Empty<JsonElement>();
        foreach (var item in legacyBindingItems)
        {
            var path = $"bindings[{bindings.Count}]";
            RequireObject(item, path);
            if (new[] { "row", "col", "column", "rowspan", "colspan", "dock", "size", "margin", "padding" }.Any(k => item.TryGetProperty(k, out _)))
                throw new JsonException($"{path}: placement belongs in layouts, not bindings.");
            if (item.TryGetProperty("cells", out _) || item.TryGetProperty("slots", out _))
                throw new JsonException($"{path}: cells and slots belong inside layouts; bindings only map slot to model.");
            var layoutId = ReadString(item, "layout", path);
            if (!layoutId.StartsWith('/') || layoutId.Contains('.'))
                throw new JsonException($"{path}.layout must be an absolute slash-separated path, for example /frame/grid.");
            var layout = layouts.FirstOrDefault(layout => layout.Path == layoutId)
                ?? throw new JsonException($"{path}: unknown layout '{layoutId}'.");
            if (layout.Type == "dock-layout")
            {
                if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: dock-layout uses parentModel and childrenModel.");
                var dockParent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
                if (dockParent.Kind is not ("viewport" or "page" or "container" or "dialog")) throw new JsonException($"{dockParent.Path} cannot be a dock parent.");
                if (!grids.Add(dockParent.Path + ":" + layout.Path)) throw new JsonException($"Duplicate dock binding for {dockParent.Path}.");
                var dockChildren = new List<StationeryDockBinding>();
                string? layoutError = null;
                var alreadyPlaced = placedModels.ToHashSet(StringComparer.Ordinal);
                try
                {
                    if (layout.CellError is { } error) throw new JsonException(error);
                    var assigned = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
                    {
                        var childPath = $"{path}.childrenModel[{dockChildren.Count}]";
                        var slot = ResolveCell(child, layout, childPath, assigned);
                        var node = ResolveModel(modelTree, ReadString(child, "model", childPath), dockParent);
                        if (node.Parent != dockParent) throw new JsonException($"{childPath}: dock elements must be direct children of {dockParent.Path}.");
                        if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                        var cellIndex = layout.Cells.ToList().IndexOf(slot);
                        dockChildren.Add(new(node.Path, slot.Dock!, slot.Size)
                        {
                            Slot = child.TryGetProperty("slot", out var legacySlot) ? legacySlot.GetString() : null,
                            CellIndex = cellIndex
                        });
                    }
                }
                catch (JsonException ex) when (recoverDockErrors)
                {
                    layoutError = ex.Message;
                    placedModels.IntersectWith(alreadyPlaced);
                    dockChildren.Clear();
                    foreach (var child in dockParent.Children)
                    {
                        if (!placedModels.Add(child.Path)) throw new JsonException($"Model {child.Path} is already placed; cannot recover dock layout.", ex);
                        dockChildren.Add(new(child.Path, "top", 48));
                    }
                }
                bindings.Add(new(layoutId, dockParent.Path, Array.Empty<StationeryCellBinding>())
                    { DockChildren = dockChildren.AsReadOnly(), LayoutError = layoutError });
                continue;
            }
            if (layout.Type is "fullscreen-layout" or "work-page-layout")
            {
                var node = ResolveModel(modelTree, ReadString(item, "model", path), null);
                var inspector = ResolveModel(modelTree, ReadString(item, "inspectorModel", path), node);
                if (node.Kind != "page" || inspector.Parent != node || inspector.Kind != "container" ||
                    !pages.Add(node.Path) || !placedModels.Add(inspector.Path))
                    throw new JsonException("A page requires one page layout and a direct inspector container placed only once.");
                if (item.TryGetProperty("parentModel", out _) || item.TryGetProperty("childrenModel", out _))
                    throw new JsonException("Page layouts use model and inspectorModel.");
                bindings.Add(new(layoutId, node.Path, Array.Empty<StationeryCellBinding>(), InspectorModel: inspector.Path));
                continue;
            }
            if (layout.Type == "split-pane")
            {
                var node = ResolveModel(modelTree, ReadString(item, "model", path), null);
                var first = ResolveModel(modelTree, ReadString(item, "firstModel", path), node);
                var second = ResolveModel(modelTree, ReadString(item, "secondModel", path), node);
                if (node.Kind != "splitPane" || first.Parent != node || second.Parent != node || first == second || node.Children.Count != 2 ||
                    !splits.Add(node.Path) || !placedModels.Add(first.Path) || !placedModels.Add(second.Path))
                    throw new JsonException("split-pane requires one splitPane model and two distinct direct children, each placed once.");
                if (item.TryGetProperty("parentModel", out _) || item.TryGetProperty("childrenModel", out _)) throw new JsonException("split-pane uses model, firstModel and secondModel.");
                bindings.Add(new(layoutId, node.Path, Array.Empty<StationeryCellBinding>(), first.Path, second.Path));
                continue;
            }
            if (layout.Type == "box-layout")
            {
                if (item.TryGetProperty("parentModel", out _))
                {
                    if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: box-layout uses either model or parentModel, not both.");
                    var boxParent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
                    if (boxParent.Kind is not ("viewport" or "page" or "container" or "dialog" or "textBlock" or "link"))
                        throw new JsonException($"{boxParent.Path} cannot be a box parent.");
                    if (!panels.Add(boxParent.Path + ":" + layout.Path)) throw new JsonException($"Multiple box-layout bindings for {boxParent.Path}.");
                    var boxChildren = new List<StationeryCellBinding>();
                    foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
                    {
                        var childPath = $"{path}.childrenModel[{boxChildren.Count}]";
                        RequireObject(child, childPath);
                        var node = ResolveModel(modelTree, ReadString(child, "model", childPath), boxParent);
                        if (node.Parent != boxParent || !placedModels.Add(node.Path) || boxChildren.Count > 0)
                            throw new JsonException($"{childPath}: box-layout requires one direct child model.");
                        boxChildren.Add(new(node.Path, 0, 0));
                    }
                    bindings.Add(new(layoutId, boxParent.Path, boxChildren.AsReadOnly()));
                }
                else
                {
                    var node = ResolveModel(modelTree, ReadString(item, "model", path), null);
                    if (!panels.Add(node.Path + ":" + layout.Path)) throw new JsonException($"Multiple box-layout bindings for {node.Path}.");
                    bindings.Add(new(layoutId, node.Path, Array.Empty<StationeryCellBinding>()));
                }
                continue;
            }
            if (layout.Type == "tabbed-box-layout")
            {
                if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: tabbed-box-layout uses parentModel and childrenModel.");
                var tabParent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
                if (tabParent.Kind is not ("viewport" or "page" or "container" or "dialog"))
                    throw new JsonException($"{tabParent.Path} cannot be a tabbed-box parent.");
                if (!panels.Add(tabParent.Path + ":" + layout.Path)) throw new JsonException($"Multiple tabbed-box-layout bindings for {tabParent.Path}.");
                var tabChildren = new List<StationeryCellBinding>();
                foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
                {
                    var childPath = $"{path}.childrenModel[{tabChildren.Count}]";
                    RequireObject(child, childPath);
                    if (child.EnumerateObject().Any(p => p.Name != "model"))
                        throw new JsonException($"{childPath}: tab order comes from childrenModel array order; only model is allowed.");
                    var node = ResolveModel(modelTree, ReadString(child, "model", childPath), tabParent);
                    if (node.Parent != tabParent || node.Kind != "page")
                        throw new JsonException($"{childPath}: tabbed-box children must be direct page models of {tabParent.Path}.");
                    if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                    tabChildren.Add(new(node.Path, tabChildren.Count, 0));
                }
                if (tabChildren.Count == 0) throw new JsonException($"{path}: tabbed-box-layout requires at least one page.");
                bindings.Add(new(layoutId, tabParent.Path, tabChildren.AsReadOnly()));
                continue;
            }
            if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: grid-layout uses parentModel and childrenModel.");
            var parent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
            if (parent.Kind is not ("viewport" or "page" or "container" or "dialog"))
                throw new JsonException($"{parent.Path} cannot be a layout parent.");
            if (!grids.Add(parent.Path + ":" + layout.Path)) throw new JsonException($"Multiple grid layouts for {parent.Path}.");
            var children = new List<StationeryCellBinding>();
            var assignedCells = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
            {
                var childPath = $"{path}.childrenModel[{children.Count}]";
                RequireObject(child, childPath);
                var node = ResolveModel(modelTree, ReadString(child, "model", childPath), parent);
                if (node == parent || !node.IsWithin(parent)) throw new JsonException($"{node.Path} must be a descendant of {parent.Path}.");
                var slot = ResolveCell(child, layout, childPath, assignedCells);
                if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                children.Add(new(node.Path, slot.Row, slot.Column, slot.RowSpan, slot.ColumnSpan));
            }
            bindings.Add(new(layoutId, parent.Path, children.AsReadOnly()));
        }
        if (legacyBindingItems.Length == 0)
        {
            if (!root.TryGetProperty("controlTree", out _))
                throw new JsonException("controlTree is required.");
            bindings.AddRange(StationeryControlBindingResolver.ResolvePlacementsFromRoutes(model, layouts, ReadControlTree(root)));
        }
        // A model is associated with at most one root tree; bindings to descendants of that tree are valid.
        foreach (var group in bindings.GroupBy(b => b.ModelPath))
        {
            var roots = group.Select(b => ("/" + b.Layout.Split('/')[1])).Distinct().ToArray();
            if (roots.Length > 1)
                throw new JsonException($"{group.Key}: a node can be associated with at most one layout tree. Nest layouts instead of binding multiple roots.");
        }
        var controlTree = StationeryControlBindingResolver.Resolve(model, layouts, bindings, ReadControlTree(root));
        return new(model, layouts.AsReadOnly(), bindings.AsReadOnly())
        {
            ViewportSnapshotTree = snapshotRoot,
            ViewportsTree = viewportsRoot,
            Styles = new ReadOnlyDictionary<string, StationeryViewStyle>(styles),
            ControlTree = controlTree
        };

        static ViewNode ReadViewNode(JsonElement value, string path, IReadOnlyDictionary<string, ViewNode>? knownNodes,
            IReadOnlyDictionary<string, StationeryViewStyle> styles)
        {
            RequireObject(value, path);
            var name = ReadString(value, "name", path);
            ViewNode? knownNode = null;
            if (knownNodes is not null && knownNodes.TryGetValue(name, out var resolvedNode))
                knownNode = resolvedNode;
            string type;
            if (value.TryGetProperty("type", out var typeJson))
            {
                if (typeJson.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(typeJson.GetString()))
                    throw new JsonException($"{path}.type must be a nonempty string.");
                type = typeJson.GetString()!;
            }
            else if (knownNode is not null) type = knownNode.Type;
            else throw new JsonException($"{path}.type is required unless the node can be resolved by name from viewports.");

            string? style = knownNode?.Style;
            if (value.TryGetProperty("style", out var styleJson))
            {
                if (styleJson.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(styleJson.GetString()))
                    throw new JsonException($"{path}.style must be a nonempty style name.");
                style = styleJson.GetString();
            }
            if (style is not null && !styles.ContainsKey(style))
                throw new JsonException($"{path}.style references unknown style '{style}'.");

            var node = new ViewNode(type, name) { Style = style };
            if (value.TryGetProperty("children", out var children))
            {
                if (children.ValueKind != JsonValueKind.Array)
                    throw new JsonException($"{path}.children must be an array.");
                foreach (var (child, index) in children.EnumerateArray().Select((child, index) => (child, index)))
                    node.ChildNodes.Add(ReadViewNode(child, $"{path}.children[{index}]", knownNodes, styles));
            }
            return node;
        }
    }

    private static IReadOnlyDictionary<string, StationeryControlBinding> ReadControlTree(JsonElement root)
    {
        var result = new Dictionary<string, StationeryControlBinding>(StringComparer.Ordinal);
        if (!root.TryGetProperty("controlTree", out var value))
            return new ReadOnlyDictionary<string, StationeryControlBinding>(result);
        RequireObject(value, "controlTree");
        foreach (var property in value.EnumerateObject())
        {
            var path = "controlTree." + property.Name;
            if (string.IsNullOrWhiteSpace(property.Name) || property.Name.Any(char.IsWhiteSpace))
                throw new JsonException($"{path}: control node names must be nonempty and contain no whitespace.");
            if (result.ContainsKey(property.Name)) throw new JsonException($"{path}: duplicate control handle.");
            RequireObject(property.Value, path);
            var hasModelPath = property.Value.TryGetProperty("modelPath", out var modelPathValue);
            var hasLayoutPath = property.Value.TryGetProperty("layoutPath", out var layoutPathValue);
            if (!hasModelPath || !hasLayoutPath ||
                property.Value.EnumerateObject().Any(p => p.Name is not ("modelPath" or "layoutPath")) ||
                modelPathValue.ValueKind != JsonValueKind.Object || layoutPathValue.ValueKind != JsonValueKind.Object)
                throw new JsonException($"{path}: expected modelPath and layoutPath objects keyed by absolute 'in /control/path' contexts.");
            var keyedModelMap = ReadPathMap(modelPathValue, path + ".modelPath", ReadModelPath);
            var keyedLayoutMap = ReadPathMap(layoutPathValue, path + ".layoutPath", ReadBindingPath);
            if (keyedLayoutMap.Count == 0 || !keyedLayoutMap.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(keyedModelMap.Keys))
                throw new JsonException($"{path}: modelPath and layoutPath must define the same nonempty set of contexts.");
            result.Add(property.Name, new(null, new ReadOnlyDictionary<string, string>(keyedLayoutMap))
            { ModelReferences = new ReadOnlyDictionary<string, string>(keyedModelMap) });
        }
        return new ReadOnlyDictionary<string, StationeryControlBinding>(result);

        static Dictionary<string, string> ReadPathMap(JsonElement value, string mapPath,
            Func<JsonElement, string, string> readValue)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in value.EnumerateObject())
            {
                if (!TryReadControlContext(entry.Name, out var context))
                    throw new JsonException($"{mapPath}: expected an absolute context key such as 'in /ctrlViewPort/ctrlDemoPage'.");
                if (!entries.TryAdd(context, readValue(entry.Value, mapPath + "." + entry.Name)))
                    throw new JsonException($"{mapPath}: duplicate control context '{context}'.");
            }
            return entries;
        }

        static bool TryReadControlContext(string key, out string context)
        {
            context = "";
            if (!key.StartsWith("in /", StringComparison.Ordinal) || key[4..].Any(char.IsWhiteSpace)) return false;
            context = key[3..];
            if (context == "/") return true;
            return !context.EndsWith('/') && !context.Contains("//", StringComparison.Ordinal) &&
                context.Split('/').Skip(1).All(segment => segment.Length > 0 && segment is not ("." or ".."));
        }
    }

    private static string ReadBindingPath(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new JsonException($"{path}: expected a nonempty layout path string.");
        var layoutPath = value.GetString()!;
        layoutPath = NormalizeBracketLayoutPath(layoutPath, path);
        if (layoutPath.Any(char.IsWhiteSpace) || !layoutPath.StartsWith("root:", StringComparison.Ordinal) ||
            layoutPath.EndsWith('/') || layoutPath.Contains("//", StringComparison.Ordinal) ||
            layoutPath.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".." || segment.Count(c => c == ':') > 1 || segment.StartsWith(':') || segment.EndsWith(':') ||
                segment.Split(':').Any(part => part.StartsWith("mdl", StringComparison.Ordinal) && part.Length > 3 && char.IsUpper(part[3]))))
            throw new JsonException($"{path}: layout paths must be root-based slash-separated routes without whitespace or empty segments.");
        return layoutPath;
    }

    private static string NormalizeBracketLayoutPath(string pathValue, string path)
    {
        // Public bracket syntax: tabbedPages[0].pageDock[center_2].grid[1y_1x_2w_1h].
        // Normalize to the existing internal route grammar so all route resolution stays shared.
        var tokens = pathValue.Split('.');
        if (tokens.Length == 0 || tokens.Any(string.IsNullOrEmpty))
            throw new JsonException($"{path}: expected a dot-separated layout path.");

        static bool TryToken(string token, out string name, out string? cell)
        {
            name = token;
            cell = null;
            var open = token.IndexOf('[');
            if (open < 0) return token.IndexOf(']') < 0 && token.Length > 0;
            if (open == 0 || token[^1] != ']' || token.IndexOf('[', open + 1) >= 0 || token.IndexOf(']') != token.Length - 1)
                return false;
            name = token[..open];
            cell = token[(open + 1)..^1];
            return name.Length > 0 && cell.Length > 0;
        }

        if (!TryToken(tokens[0], out var rootName, out var pendingCell))
            throw new JsonException($"{path}: the first dot-syntax segment must be a root layout ID.");
        var route = new System.Text.StringBuilder("root:").Append(rootName);
        for (var i = 1; i < tokens.Length; i++)
        {
            if (!TryToken(tokens[i], out var name, out var cell))
                throw new JsonException($"{path}: invalid dot-syntax segment '{tokens[i]}'.");
            route.Append('/');
            if (pendingCell is not null)
                route.Append(pendingCell.Replace('_', '.')).Append(':').Append(name);
            else
                route.Append(name);
            pendingCell = cell;
        }
        if (pendingCell is not null) route.Append('/').Append(pendingCell.Replace('_', '.'));
        return route.ToString();
    }

    private static string ReadModelPath(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new JsonException($"{path}: expected a nonempty absolute model path.");
        var modelPath = value.GetString()!;
        if (!modelPath.StartsWith('/') || modelPath.EndsWith('/') || modelPath.Contains("//", StringComparison.Ordinal) ||
            modelPath.Split('/').Skip(1).Any(segment => segment.Length == 0 || segment is "." or ".." || segment.Any(char.IsWhiteSpace)))
            throw new JsonException($"{path}: expected an absolute slash-separated model path.");
        return modelPath;
    }

    private static StationeryLayoutCell ResolveCell(JsonElement child, StationeryLayoutNode layout, string path, HashSet<string> assigned)
    {
        RequireObject(child, path);
        if (child.EnumerateObject().Any(p => p.Name is not ("model" or "slot" or "cell")))
            throw new JsonException($"{path}: placement belongs in layouts.cells; use the cell object to select a layout cell.");
        if (child.TryGetProperty("cell", out var cell))
        {
            RequireObject(cell, path + ".cell");
            if (layout.Type == "grid-layout")
            {
                if (cell.EnumerateObject().Any(p => p.Name is not ("row" or "col")))
                    throw new JsonException($"{path}.cell: grid cell keys are row and col.");
                var row = ReadInteger(cell, "row", path + ".cell", 0, 0);
                var col = ReadInteger(cell, "col", path + ".cell", 0, 0);
                var candidate = layout.Cells.FirstOrDefault(c => c.Row == row && c.Column == col);
                if (candidate is null) throw new JsonException($"{path}: unknown cell row={row}, col={col} in {layout.Path}.");
                var key = $"row={row},col={col}";
                if (!assigned.Add(key)) throw new JsonException($"{path}: cell '{key}' is assigned more than once.");
                return candidate;
            }
            if (cell.EnumerateObject().Any(p => p.Name is not ("dock" or "index")))
                throw new JsonException($"{path}.cell: dock cell keys are dock and index.");
            var direction = ReadString(cell, "dock", path + ".cell");
            var index = ReadInteger(cell, "index", path + ".cell", 1, 1);
            var candidates = layout.Cells.Where(c => c.Dock == direction).ToArray();
            if (index > candidates.Length) throw new JsonException($"{path}: unknown cell {direction}={index} in {layout.Path}.");
            var dockCandidate = candidates[index - 1];
            if (!assigned.Add($"{direction}={index}")) throw new JsonException($"{path}: cell '{direction}={index}' is assigned more than once.");
            return dockCandidate;
        }
        // Legacy slot references remain readable while styles migrate.
        var id = ReadString(child, "slot", path);
        var slot = layout.Cells.FirstOrDefault(c => c.Slots.Any(s => s.Id == id)) ?? throw new JsonException($"{path}: unknown slot '{id}' in {layout.Path}.");
        if (!assigned.Add("legacy:" + id)) throw new JsonException($"{path}: slot '{id}' is assigned more than once.");
        return slot;
    }

    private static StationeryUI.Inspection.StationeryNode ResolveModel(StationeryUI.Inspection.StationeryNode root,
        string reference, StationeryUI.Inspection.StationeryNode? parent)
    {
        // Child references are scoped to the binding parent, never a global short-ID search.
        var path = reference.StartsWith('/') ? reference : (parent?.Path ?? "") + "/" + reference;
        return root.Resolve(path) ?? throw new JsonException($"Unknown model path '{path}'.");
    }

    internal IReadOnlyDictionary<string, ViewportPadding> GetModelMargins()
    {
        var result = new Dictionary<string, ViewportPadding>(StringComparer.Ordinal);
        void Visit(StationeryModelNode node, string parent)
        {
            var path = parent + "/" + node.Id;
            result.Add(path, node.Margin);
            foreach (var child in node.Children) Visit(child, path);
        }
        Visit(ModelTree, "");
        return result;
    }

    private static StationeryModelNode ReadModel(JsonElement value, string path)
    {
        RequireObject(value, path);
        var id = ReadString(value, "id", path);
        var type = ReadString(value, "type", path);
        ValidateId(id, path);
        var children = new List<StationeryModelNode>();
        if (value.TryGetProperty("children", out _))
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in ReadArray(value, "children", path).EnumerateArray())
            {
                var parsed = ReadModel(child, $"{path}.children[{children.Count}]");
                if (!ids.Add(parsed.Id)) throw new JsonException($"Duplicate model Id '{parsed.Id}' under {path}.");
                children.Add(parsed);
            }
        }
        var margin = default(ViewportPadding);
        if (value.TryGetProperty("margin", out var edges))
        {
            RequireObject(edges, path + ".margin");
            double Side(string side) => edges.TryGetProperty(side, out var v)
                ? ReadLength(v, path + ".margin." + side, false).Value : 0;
            margin = new(Side("top"), Side("right"), Side("bottom"), Side("left"));
        }
        return new(id, type, children.AsReadOnly()) { Margin = margin };
    }

    private static void ValidateId(string id, string path)
    {
        try { _ = new StationeryUI.Inspection.StationeryNode(id); }
        catch (ArgumentException ex) { throw new JsonException($"{path}: {ex.Message}", ex); }
    }
    private static JsonElement RequireObject(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Object ? value : throw new JsonException($"{path} must be an object.");
    private static JsonElement ReadArray(JsonElement value, string name, string path) =>
        value.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? array : throw new JsonException($"{path}.{name} must be an array.");
    private static string ReadString(JsonElement value, string name, string path)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString())) throw new JsonException($"{path}.{name} must be a nonempty string.");
        return property.GetString()!;
    }
    private static int ReadInteger(JsonElement value, string name, string path, int minimum, int fallback)
    {
        if (!value.TryGetProperty(name, out var property)) return fallback;
        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var result) || result < minimum)
            throw new JsonException($"{path}.{name} must be an integer >= {minimum}.");
        return result;
    }
    private static int ReadColumn(JsonElement value, string path, bool required)
    {
        var col = value.TryGetProperty("col", out _);
        var column = value.TryGetProperty("column", out _);
        if (col && column || required && !col && !column) throw new JsonException($"{path}: specify col (or legacy column), not both.");
        return ReadInteger(value, col ? "col" : "column", path, 0, 0);
    }
    private static void ValidateCell(StationeryCellBinding cell, int rows, int columns, List<StationeryCellBinding> occupied, string path)
    {
        if (cell.Row >= rows || cell.Column >= columns || cell.RowSpan > rows - cell.Row || cell.ColumnSpan > columns - cell.Column)
            throw new JsonException($"{path}: cell span is outside the grid.");
        if (occupied.Any(c => cell.Row < c.Row + c.RowSpan && c.Row < cell.Row + cell.RowSpan &&
            cell.Column < c.Column + c.ColumnSpan && c.Column < cell.Column + cell.ColumnSpan))
            throw new JsonException($"{path}: overlapping cell spans.");
        occupied.Add(cell);
    }
    private static int ReadIndex(JsonElement value, string name, string path, int count)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Number ||
            !property.TryGetInt32(out var index) || index < 0 || index >= count)
            throw new JsonException($"{path}.{name} must be an integer from 0 to {count - 1}.");
        return index;
    }
    private static double ReadPixels(JsonElement padding, string side, string path) =>
        padding.TryGetProperty(side, out var value) ? ReadLength(value, path + ".padding." + side, allowRate: false).Value : 8;
    private static IReadOnlyList<LayoutTrack> ReadTracks(JsonElement value, string name, string path)
    {
        var result = ReadArray(value, name, path).EnumerateArray()
            .Select((item, index) => ReadLength(item, $"{path}.{name}[{index}]", allowRate: true)).ToArray();
        if (result.Length == 0 || !result.Any(track => track.Value > 0))
            throw new JsonException($"{path}.{name} must have at least one positive track.");
        return Array.AsReadOnly(result);
    }
    private static LayoutTrack ReadLength(JsonElement value, string path, bool allowRate)
    {
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
        var rate = allowRate && text.EndsWith("rate", StringComparison.Ordinal);
        var suffix = rate ? 4 : 2;
        if ((!rate && !text.EndsWith("px", StringComparison.Ordinal)) ||
            !double.TryParse(text.AsSpan(0, text.Length - suffix), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount) || !double.IsFinite(amount) || amount < 0)
            throw new JsonException($"{path} must be a nonnegative px{(allowRate ? " or rate" : "")} string.");
        return new(amount, rate);
    }
}
