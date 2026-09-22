namespace StationeryUI.Styling;

using System.Globalization;
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

public readonly record struct LayoutTrack(double Value, bool IsRate);

/// <summary>A reusable layout definition. It has no reference to model identities.</summary>
public sealed record StationeryLayoutNode(string Id, string Type, ViewportPadding Padding,
    IReadOnlyList<LayoutTrack> Rows, IReadOnlyList<LayoutTrack> Columns, SplitPaneOptions? Split = null, double InspectorHeight = 0,
    ViewportPadding Margin = default, ViewportPadding Border = default)
{
    public IReadOnlyList<StationeryLayoutSlot> Slots { get; init; } = [];
    public string? SlotError { get; init; }
    public string Path { get; init; } = "/" + Id;
    public string? ParentPath { get; init; }
    public int Row { get; init; }
    public int Column { get; init; }
    public int RowSpan { get; init; } = 1;
    public int ColumnSpan { get; init; } = 1;
    public IReadOnlyList<StationeryLayoutNode> Children { get; init; } = [];
}
public sealed record StationeryLayoutSlot(string Id, int Row = 0, int Column = 0, int RowSpan = 1, int ColumnSpan = 1,
    string? Dock = null, double Size = 0, ViewportPadding Margin = default);
public sealed record StationeryCellBinding(string ModelPath, int Row, int Column, int RowSpan = 1, int ColumnSpan = 1)
{ public ViewportPadding Margin { get; init; } }
public sealed record StationeryDockBinding(string ModelPath, string Dock, double Size = 0)
{ public string? Slot { get; init; } public ViewportPadding Margin { get; init; } }
/// <summary>References are resolved to canonical model paths when a complete settings snapshot is parsed.</summary>
public sealed record StationeryLayoutBinding(string Layout, string ModelPath, IReadOnlyList<StationeryCellBinding> Children,
    string? FirstModel = null, string? SecondModel = null, string? InspectorModel = null)
{
    public IReadOnlyList<StationeryDockBinding> DockChildren { get; init; } = [];
    public string? LayoutError { get; init; }
}

public sealed record StationeryStyleSettings(IReadOnlyList<StationeryModelNode> Models,
    IReadOnlyList<StationeryLayoutNode> Layouts, IReadOnlyList<StationeryLayoutBinding> Bindings)
{
    public static StationeryStyleSettings Default { get; } = Parse("""
        {"models":[{"id":"demo","type":"viewport"}],
         "layouts":[{"id":"rootPanel","type":"box-layout"}],
         "bindings":[{"layout":"/rootPanel","model":"demo"}]}
        """);

    // Convenience for consumers interested only in root padding; never depends on layouts array order.
    public ViewportPadding Padding
    {
        get
        {
            var rootPath = "/" + Models[0].Id;
            var binding = Bindings.FirstOrDefault(binding => binding.ModelPath == rootPath &&
                Layouts.Any(layout => layout.Path == ("/" + binding.Layout.Split('/')[1]) && layout.Type is "box-layout" or "grid-layout" or "dock-layout"));
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
        if (root.TryGetProperty("viewport", out _) || root.TryGetProperty("model", out _) || root.TryGetProperty("layout", out _))
            throw new JsonException("Use the models, layouts and bindings arrays; old root keys are obsolete.");
        var modelJson = ReadArray(root, "models", "root");
        if (modelJson.GetArrayLength() != 1) throw new JsonException("models currently requires exactly one viewport root.");
        var model = ReadModel(modelJson[0], "models[0]");
        if (model.Type != "viewport") throw new JsonException("models[0].type must be viewport.");
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
            if (type is not ("box-layout" or "grid-layout" or "dock-layout" or "split-pane" or "fullscreen-layout" or "work-page-layout")) throw new JsonException($"{path}.type must be box-layout, grid-layout, dock-layout, split-pane, fullscreen-layout or work-page-layout.");
            if (item.TryGetProperty("contents", out _) ||
                item.TryGetProperty("model", out _) || item.TryGetProperty("parentModel", out _))
                throw new JsonException($"{path}: model references and placement belong in bindings.");
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
            if (type is "box-layout" or "grid-layout" or "dock-layout") margin = ReadEdges("margin");
            else if (item.TryGetProperty("margin", out _)) throw new JsonException($"{path}: margin requires box-layout, grid-layout or dock-layout.");
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
                    throw new JsonException($"{path}: dock-layout uses dock/size on slots, not track definitions.");
            }
            else if (type == "box-layout")
            {
                margin = ReadEdges("margin");
                border = ReadEdges("border");
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
            var slots = new List<StationeryLayoutSlot>();
            string? slotError = null;
            try
            {
                if (item.TryGetProperty("slots", out _) && type is not ("grid-layout" or "dock-layout"))
                    throw new JsonException($"{path}: slots require grid-layout or dock-layout.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var occupied = new List<StationeryCellBinding>();
                // Check nested layout cells even when the grid has no model slots.
                if (type == "grid-layout")
                {
                    foreach (var child in children) ValidateCell(new(child.Path, child.Row, child.Column, child.RowSpan, child.ColumnSpan), rows.Count, columns.Count, occupied, path);
                }
                if (item.TryGetProperty("slots", out _))
                    foreach (var slot in ReadArray(item, "slots", path).EnumerateArray())
                    {
                        var slotPath = $"{path}.slots[{slots.Count}]";
                        RequireObject(slot, slotPath);
                        var slotId = ReadString(slot, "id", slotPath); ValidateId(slotId, slotPath);
                        if (!ids.Add(slotId)) throw new JsonException($"{slotPath}: duplicate slot '{slotId}'.");
                        if (slot.TryGetProperty("model", out _) || slot.TryGetProperty("parentModel", out _))
                            throw new JsonException($"{slotPath}: model references belong in bindings.");
                        if (type == "grid-layout")
                        {
                            if (slot.TryGetProperty("dock", out _) || slot.TryGetProperty("size", out _)) throw new JsonException($"{slotPath}: grid slots use cell coordinates.");
                            var cell = new StationeryCellBinding(slotId, ReadIndex(slot, "row", slotPath, rows.Count), ReadColumn(slot, slotPath, true),
                                ReadInteger(slot, "rowspan", slotPath, 1, 1), ReadInteger(slot, "colspan", slotPath, 1, 1));
                            ValidateCell(cell, rows.Count, columns.Count, occupied, slotPath);
                            slots.Add(new(slotId, cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan, Margin: ReadEdges("margin", slot)));
                        }
                        else
                        {
                            if (new[] { "row", "col", "column", "rowspan", "colspan" }.Any(k => slot.TryGetProperty(k, out _))) throw new JsonException($"{slotPath}: dock slots use dock and size.");
                            var dock = ReadString(slot, "dock", slotPath);
                            if (dock is not ("top" or "right" or "bottom" or "left" or "center")) throw new JsonException($"{slotPath}.dock must be top, right, bottom, left or center.");
                            var size = ReadString(slot, "size", slotPath);
                            double pixels = 0;
                            if (dock == "center")
                            {
                                if (slots.Any(s => s.Dock == "center")) throw new JsonException($"{slotPath}: dock-layout allows at most one center.");
                                if (size != "remaining") throw new JsonException($"{slotPath}.size must be remaining for center.");
                            }
                            else pixels = ReadLength(slot.GetProperty("size"), slotPath + ".size", false).Value;
                            slots.Add(new(slotId, Dock: dock, Size: pixels, Margin: ReadEdges("margin", slot)));
                        }
                    }
            }
            catch (JsonException ex) when (recoverDockErrors && type == "dock-layout")
            { slotError = ex.Message; slots.Clear(); }
            return new(id, type, padding, rows, columns, split, inspectorHeight, margin, border)
            { Slots = slots.AsReadOnly(), SlotError = slotError, Path = path, ParentPath = parentPath, Row = row, Column = col, RowSpan = rowSpan, ColumnSpan = colSpan, Children = Array.AsReadOnly(children) };
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
        foreach (var item in ReadArray(root, "bindings", "root").EnumerateArray())
        {
            var path = $"bindings[{bindings.Count}]";
            RequireObject(item, path);
            if (new[] { "row", "col", "column", "rowspan", "colspan", "dock", "size", "margin", "padding" }.Any(k => item.TryGetProperty(k, out _)))
                throw new JsonException($"{path}: placement belongs in layouts, not bindings.");
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
                    if (layout.SlotError is { } error) throw new JsonException(error);
                    var assigned = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
                    {
                        var childPath = $"{path}.childrenModel[{dockChildren.Count}]";
                        var slot = ResolveSlot(child, layout, childPath, assigned);
                        var node = ResolveModel(modelTree, ReadString(child, "model", childPath), dockParent);
                        if (node.Parent != dockParent) throw new JsonException($"{childPath}: dock elements must be direct children of {dockParent.Path}.");
                        if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                        dockChildren.Add(new(node.Path, slot.Dock!, slot.Size) { Slot = slot.Id, Margin = slot.Margin });
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
                if (item.TryGetProperty("parentModel", out _) || item.TryGetProperty("childrenModel", out _))
                    throw new JsonException($"{path}: a box-layout binding uses model, not parentModel/childrenModel.");
                var node = ResolveModel(modelTree, ReadString(item, "model", path), null);
                if (!panels.Add(node.Path + ":" + layout.Path)) throw new JsonException($"Multiple box-layout bindings for {node.Path}.");
                bindings.Add(new(layoutId, node.Path, Array.Empty<StationeryCellBinding>()));
                continue;
            }
            if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: grid-layout uses parentModel and childrenModel.");
            var parent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
            if (parent.Kind is not ("viewport" or "page" or "container" or "dialog"))
                throw new JsonException($"{parent.Path} cannot be a layout parent.");
            if (!grids.Add(parent.Path + ":" + layout.Path)) throw new JsonException($"Multiple grid layouts for {parent.Path}.");
            var children = new List<StationeryCellBinding>();
            var assignedSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
            {
                var childPath = $"{path}.childrenModel[{children.Count}]";
                RequireObject(child, childPath);
                var node = ResolveModel(modelTree, ReadString(child, "model", childPath), parent);
                if (node == parent || !node.IsWithin(parent)) throw new JsonException($"{node.Path} must be a descendant of {parent.Path}.");
                var slot = ResolveSlot(child, layout, childPath, assignedSlots);
                if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                children.Add(new(node.Path, slot.Row, slot.Column, slot.RowSpan, slot.ColumnSpan) { Margin = slot.Margin });
            }
            bindings.Add(new(layoutId, parent.Path, children.AsReadOnly()));
        }
        // A model owns at most one root tree; bindings to descendants of that tree are valid.
        foreach (var group in bindings.GroupBy(b => b.ModelPath))
        {
            var roots = group.Select(b => ("/" + b.Layout.Split('/')[1])).Distinct().ToArray();
            if (roots.Length > 1)
                throw new JsonException($"{group.Key}: a node can own at most one layout tree. Nest layouts instead of binding multiple roots.");
        }
        return new(Array.AsReadOnly(new[] { model }), layouts.AsReadOnly(), bindings.AsReadOnly());
    }

    private static StationeryLayoutSlot ResolveSlot(JsonElement child, StationeryLayoutNode layout, string path, HashSet<string> assigned)
    {
        RequireObject(child, path);
        if (new[] { "row", "col", "column", "rowspan", "colspan", "dock", "size", "margin", "padding" }.Any(k => child.TryGetProperty(k, out _)))
            throw new JsonException($"{path}: placement belongs in layouts.slots; bindings contain only slot and model references.");
        var id = ReadString(child, "slot", path);
        var slot = layout.Slots.FirstOrDefault(s => s.Id == id) ?? throw new JsonException($"{path}: unknown slot '{id}' in {layout.Path}.");
        if (!assigned.Add(id)) throw new JsonException($"{path}: slot '{id}' is assigned more than once.");
        return slot;
    }

    private static StationeryUI.Inspection.StationeryNode ResolveModel(StationeryUI.Inspection.StationeryNode root,
        string reference, StationeryUI.Inspection.StationeryNode? parent)
    {
        // Child references are scoped to the binding parent, never a global short-ID search.
        var path = reference.StartsWith('/') ? reference : (parent?.Path ?? "") + "/" + reference;
        return root.Resolve(path) ?? throw new JsonException($"Unknown model path '{path}'.");
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
        return new(id, type, children.AsReadOnly());
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
