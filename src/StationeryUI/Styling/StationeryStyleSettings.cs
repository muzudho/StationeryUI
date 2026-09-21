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
    IReadOnlyList<LayoutTrack> Rows, IReadOnlyList<LayoutTrack> Columns, SplitPaneOptions? Split = null);
public sealed record StationeryCellBinding(string ModelPath, int Row, int Column);
/// <summary>References are resolved to canonical model paths when a complete settings snapshot is parsed.</summary>
public sealed record StationeryLayoutBinding(string Layout, string ModelPath, IReadOnlyList<StationeryCellBinding> Children,
    string? FirstModel = null, string? SecondModel = null);

public sealed record StationeryStyleSettings(IReadOnlyList<StationeryModelNode> Models,
    IReadOnlyList<StationeryLayoutNode> Layouts, IReadOnlyList<StationeryLayoutBinding> Bindings)
{
    public static StationeryStyleSettings Default { get; } = Parse("""
        {"models":[{"id":"demo","type":"viewport"}],
         "layouts":[{"id":"rootPanel","type":"panel"}],
         "bindings":[{"layout":"rootPanel","model":"demo"}]}
        """);

    // Convenience for consumers interested only in root padding; never depends on layouts array order.
    public ViewportPadding Padding
    {
        get
        {
            var rootPath = "/" + Models[0].Id;
            var binding = Bindings.FirstOrDefault(binding => binding.ModelPath == rootPath &&
                Layouts.Any(layout => layout.Id == binding.Layout && layout.Type == "panel"));
            return binding is null ? default : Layouts.Single(layout => layout.Id == binding.Layout).Padding;
        }
    }

    public static StationeryStyleSettings Parse(string json)
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
        foreach (var item in ReadArray(root, "layouts", "root").EnumerateArray())
        {
            var path = $"layouts[{layouts.Count}]";
            RequireObject(item, path);
            var id = ReadString(item, "id", path);
            ValidateId(id, path);
            if (!layoutIds.Add(id)) throw new JsonException($"Duplicate layout Id '{id}'.");
            var type = ReadString(item, "type", path);
            if (type is not ("panel" or "floating-layout" or "split-pane")) throw new JsonException($"{path}.type must be panel, floating-layout or split-pane.");
            if (item.TryGetProperty("children", out _) || item.TryGetProperty("contents", out _) ||
                item.TryGetProperty("model", out _) || item.TryGetProperty("parentModel", out _))
                throw new JsonException($"{path}: model references and placement belong in bindings.");
            var padding = default(ViewportPadding);
            SplitPaneOptions? split = null;
            IReadOnlyList<LayoutTrack> rows = Array.Empty<LayoutTrack>(), columns = Array.Empty<LayoutTrack>();
            if (type == "panel")
            {
                if (item.TryGetProperty("row-definitions", out _) || item.TryGetProperty("column-definitions", out _))
                    throw new JsonException($"{path}: track definitions require floating-layout.");
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
                if (item.TryGetProperty("padding", out _)) throw new JsonException($"{path}: put padding in a separate panel layout.");
                rows = ReadTracks(item, "row-definitions", path);
                columns = ReadTracks(item, "column-definitions", path);
            }
            layouts.Add(new(id, type, padding, rows, columns, split));
        }
        var bindings = new List<StationeryLayoutBinding>();
        var panels = new HashSet<string>(StringComparer.Ordinal);
        var grids = new HashSet<string>(StringComparer.Ordinal);
        var placedModels = new HashSet<string>(StringComparer.Ordinal);
        var splits = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in ReadArray(root, "bindings", "root").EnumerateArray())
        {
            var path = $"bindings[{bindings.Count}]";
            RequireObject(item, path);
            var layoutId = ReadString(item, "layout", path);
            var layout = layouts.FirstOrDefault(layout => layout.Id == layoutId)
                ?? throw new JsonException($"{path}: unknown layout '{layoutId}'.");
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
            if (layout.Type == "panel")
            {
                if (item.TryGetProperty("parentModel", out _) || item.TryGetProperty("childrenModel", out _))
                    throw new JsonException($"{path}: a panel binding uses model, not parentModel/childrenModel.");
                var node = ResolveModel(modelTree, ReadString(item, "model", path), null);
                if (!panels.Add(node.Path)) throw new JsonException($"Multiple panel bindings for {node.Path}.");
                bindings.Add(new(layoutId, node.Path, Array.Empty<StationeryCellBinding>()));
                continue;
            }
            if (item.TryGetProperty("model", out _)) throw new JsonException($"{path}: floating-layout uses parentModel and childrenModel.");
            var parent = ResolveModel(modelTree, ReadString(item, "parentModel", path), null);
            if (parent.Kind is not ("viewport" or "page" or "container" or "dialog"))
                throw new JsonException($"{parent.Path} cannot be a layout parent.");
            if (!grids.Add(parent.Path)) throw new JsonException($"Multiple floating layouts for {parent.Path}.");
            var children = new List<StationeryCellBinding>();
            var cells = new HashSet<(int, int)>();
            foreach (var child in ReadArray(item, "childrenModel", path).EnumerateArray())
            {
                var childPath = $"{path}.childrenModel[{children.Count}]";
                RequireObject(child, childPath);
                var node = ResolveModel(modelTree, ReadString(child, "model", childPath), parent);
                if (node == parent || !node.IsWithin(parent)) throw new JsonException($"{node.Path} must be a descendant of {parent.Path}.");
                var row = ReadIndex(child, "row", childPath, layout.Rows.Count);
                var column = ReadIndex(child, "column", childPath, layout.Columns.Count);
                if (!cells.Add((row, column))) throw new JsonException($"Duplicate cell ({row}, {column}) in {path}.");
                if (!placedModels.Add(node.Path)) throw new JsonException($"Model {node.Path} is placed more than once.");
                children.Add(new(node.Path, row, column));
            }
            bindings.Add(new(layoutId, parent.Path, children.AsReadOnly()));
        }
        return new(Array.AsReadOnly(new[] { model }), layouts.AsReadOnly(), bindings.AsReadOnly());
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
