namespace StationeryUI.Editor;

using StationeryUI.Controls;
using System.Text.Json.Nodes;

/// <summary>Read-only JSON hierarchy with stable document paths across reloads.</summary>
internal sealed class ReadJsonTree
{
    private sealed record NodeInfo(string[] Path, string? ModelPath, string Description);
    private readonly Dictionary<TreeItem, NodeInfo> nodes = [];
    private readonly Dictionary<string, TreeItem> byPath = new(StringComparer.Ordinal);
    public TreeView Tree { get; } = new();
    public string[]? SelectedPath => Tree.TargetItem is { } item ? nodes[item].Path : null;

    public static ReadJsonTree Create(string json, ReadJsonTree? previous = null)
    {
        var document = new ReadJsonTree();
        var oldExpansion = previous?.nodes.ToDictionary(pair => Pointer(pair.Value.Path), pair => pair.Key.IsExpanded,
            StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
        var selected = previous?.SelectedPath;
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new ArgumentException("JSON のルートはオブジェクトにしてください。");
        var index = 0;
        void Visit(JsonNode? value, string key, string[] path, TreeItem? parent, string? modelPath, bool modelNode)
        {
            if (modelNode && value is JsonObject model && model["id"] is JsonValue id)
                modelPath = (modelPath ?? "") + "/" + id.GetValue<string>();
            var title = value is JsonObject obj && obj["id"] is JsonValue name
                ? $"{key}: {name.GetValue<string>()} ({(string?)obj["type"] ?? "object"})"
                : value is JsonValue ? $"{key}: {value}" : key;
            var pointer = Pointer(path);
            var item = document.Tree.AddNode("json" + index++, title, parent,
                oldExpansion.TryGetValue(pointer, out var expanded) ? expanded : path.Length < 2);
            var kind = value switch
            {
                JsonObject objectValue => $"オブジェクト（{objectValue.Count} 項目）",
                JsonArray array => $"配列（{array.Count} 件）",
                null => "null",
                _ => "値: " + value.ToJsonString()
            };
            document.nodes[item] = new(path, modelPath, kind);
            document.byPath[pointer] = item;
            if (value is JsonObject properties)
                foreach (var pair in properties)
                    Visit(pair.Value, pair.Key, [.. path, pair.Key], item, modelPath,
                        path.Length == 0 && pair.Key == "modelTree");
            else if (value is JsonArray array)
                for (var i = 0; i < array.Count; i++)
                    Visit(array[i], "[" + i + "]", [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                        item, modelPath, path[0] == "modelTree" && path[^1] == "children");
        }
        foreach (var pair in root) Visit(pair.Value, pair.Key, [pair.Key], null, null, pair.Key == "modelTree");
        if (selected is null || !document.SelectPath(selected)) document.Tree.Move(0);
        return document;
    }

    public bool SelectPath(IReadOnlyList<string> path)
    {
        if (!byPath.TryGetValue(Pointer(path), out var item)) return false;
        Tree.Select(item); Tree.SetTarget(item); return true;
    }

    public string Details(TreeItem? item)
    {
        if (item is null || !nodes.TryGetValue(item, out var info)) return "JSON の項目を選択してください。";
        return "JSON パス: " + Pointer(info.Path) + "\n\n" + info.Description
            + (info.ModelPath is null ? "\n\n対応するモデル: なし" : "\n\n対応するモデル: " + info.ModelPath);
    }

    public string? InspectionPath(TreeItem? item) => item is not null && nodes.TryGetValue(item, out var info)
        ? info.ModelPath : null;
    public string? CopyPath(TreeItem? item) => item is not null && nodes.TryGetValue(item, out var info)
        ? Pointer(info.Path) : null;

    private static string Pointer(IReadOnlyList<string> path)
        => "/" + string.Join("/", path.Select(part => part.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal)));
}
