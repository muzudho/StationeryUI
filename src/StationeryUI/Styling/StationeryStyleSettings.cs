namespace StationeryUI.Styling;

using System.Globalization;
using System.Text.Json;
using StationeryUI.Canvas;

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

/// <summary>Layout identity references a model node; layout does not establish model ownership.</summary>
public sealed record StationeryLayoutNode(string Id, string Type, ViewportPadding Padding);

public sealed record StationeryStyleSettings(StationeryModelNode Model, IReadOnlyList<StationeryLayoutNode> Layout)
{
    public static StationeryStyleSettings Default { get; } = Parse("""
        {"model":{"id":"demo","type":"viewport"},"layout":[{"id":"demo","type":"viewport"}]}
        """);
    public ViewportPadding Padding => Layout[0].Padding;

    public static StationeryStyleSettings Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement, "root");
        if (root.TryGetProperty("viewport", out _))
            throw new JsonException("The top-level viewport is obsolete. Use model and layout instead.");
        if (!root.TryGetProperty("model", out var modelJson)) throw new JsonException("model is required.");
        var model = ReadModel(modelJson, "model");
        if (model.Type != "viewport") throw new JsonException("model root type must be viewport.");
        if (!root.TryGetProperty("layout", out var layoutJson) || layoutJson.ValueKind != JsonValueKind.Array)
            throw new JsonException("layout must be an array.");
        // This first layout implementation supports the existing viewport padding operation.
        if (layoutJson.GetArrayLength() != 1) throw new JsonException("layout currently requires exactly one viewport entry.");
        var item = RequireObject(layoutJson[0], "layout[0]");
        var id = ReadString(item, "id", "layout[0]");
        var type = ReadString(item, "type", "layout[0]");
        if (type != "viewport") throw new JsonException("layout[0].type currently supports only viewport.");
        if (id != model.Id && id != "/" + model.Id)
            throw new JsonException("layout[0].id must reference the model root by Id or absolute path.");
        if (item.TryGetProperty("children", out _) || item.TryGetProperty("contents", out _))
            throw new JsonException("Nested layout is not implemented yet.");
        var padding = new ViewportPadding(8, 8, 8, 8);
        if (item.TryGetProperty("padding", out var value))
        {
            RequireObject(value, "layout[0].padding");
            padding = new(ReadPixels(value, "top", 8), ReadPixels(value, "right", 8),
                ReadPixels(value, "bottom", 8), ReadPixels(value, "left", 8));
        }
        return new(model, Array.AsReadOnly(new[] { new StationeryLayoutNode(id, type, padding) }));
    }

    private static StationeryModelNode ReadModel(JsonElement value, string path)
    {
        RequireObject(value, path);
        var id = ReadString(value, "id", path);
        var type = ReadString(value, "type", path);
        try { _ = new StationeryUI.Inspection.StationeryNode(id, type); }
        catch (ArgumentException ex) { throw new JsonException($"{path}: {ex.Message}", ex); }
        var children = new List<StationeryModelNode>();
        if (value.TryGetProperty("children", out var array))
        {
            if (array.ValueKind != JsonValueKind.Array) throw new JsonException($"{path}.children must be an array.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in array.EnumerateArray())
            {
                var parsed = ReadModel(child, $"{path}.children[{children.Count}]");
                if (!ids.Add(parsed.Id)) throw new JsonException($"Duplicate model Id '{parsed.Id}' under {path}.");
                children.Add(parsed);
            }
        }
        return new(id, type, children.AsReadOnly());
    }

    private static JsonElement RequireObject(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Object ? value : throw new JsonException($"{path} must be an object.");

    private static string ReadString(JsonElement value, string name, string path)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString())) throw new JsonException($"{path}.{name} must be a nonempty string.");
        return property.GetString()!;
    }

    private static double ReadPixels(JsonElement padding, string side, double fallback)
    {
        if (!padding.TryGetProperty(side, out var value)) return fallback;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
        if (!text.EndsWith("px", StringComparison.Ordinal) ||
            !double.TryParse(text.AsSpan(0, text.Length - 2), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var pixels) || !double.IsFinite(pixels) || pixels < 0)
            throw new JsonException($"layout[0].padding.{side} must be a nonnegative pixel string, such as \"8px\".");
        return pixels;
    }
}
