namespace StationeryUI.Editor;

using StationeryUI.Controls;
using StationeryUI.Styling;
using System.Text.Json.Nodes;

/// <summary>Read-only JSON hierarchy with stable document paths across reloads.</summary>
internal sealed class ReadJsonTree
{
    private sealed record NodeInfo(string[] Path, string? ModelPath, string Description, string[] LayoutPaths);
    private readonly Dictionary<TreeItem, NodeInfo> nodes = [];
    private readonly Dictionary<string, TreeItem> byPath = new(StringComparer.Ordinal);
    private readonly HashSet<string> inspectionPaths = new(StringComparer.Ordinal);
    public TreeView Tree { get; } = new();
    public string[]? SelectedPath => Tree.TargetItem is { } item ? nodes[item].Path : null;

    public static ReadJsonTree Create(string json, ReadJsonTree? previous = null, StationeryStyleSettings? settings = null)
    {
        var document = new ReadJsonTree();
        var oldExpansion = previous?.nodes.ToDictionary(pair => Pointer(pair.Value.Path), pair => pair.Key.IsExpanded,
            StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
        var selected = previous?.SelectedPath;
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new ArgumentException("JSON のルートはオブジェクトにしてください。");
        settings ??= StationeryStyleSettings.Parse(json);
        var definitions = StyleBlueprint.LayoutNodes(root).ToArray();
        var definitionPaths = new Dictionary<JsonNode, string>(ReferenceEqualityComparer.Instance);
        foreach (var definition in definitions) definitionPaths.Add(definition.Node, definition.Path);
        var namedDefinitions = definitions.GroupBy(pair => pair.Path.Split('/')[^1], StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Path, StringComparer.Ordinal);
        var ownersByRoot = settings.Bindings.GroupBy(binding => "/" + binding.Layout.Split('/')[1], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(binding => binding.ModelPath)
                .Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        string[] LayoutInstances(string? layoutPath)
        {
            if (layoutPath is null) return [];
            var rootPath = "/" + layoutPath.Split('/')[1];
            return ownersByRoot.TryGetValue(rootPath, out var owners)
                ? owners.Select(owner => owner + ":" + layoutPath).ToArray() : [];
        }
        var index = 0;
        void Visit(JsonNode? value, string key, string[] path, TreeItem? parent, string? modelPath, bool modelNode,
            string? layoutPath)
        {
            if (modelNode && value is JsonObject model && model["id"] is JsonValue id)
                modelPath = (modelPath ?? "") + "/" + id.GetValue<string>();
            if (value is not null && definitionPaths.TryGetValue(value, out var definitionPath))
                layoutPath = definitionPath;
            else if (key == "layout" && value is JsonObject reference && reference["ref"] is JsonValue refValue
                && refValue.TryGetValue<string>(out var referenceName)
                && namedDefinitions.TryGetValue(referenceName, out var referencedPath))
                layoutPath = referencedPath;
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
            document.nodes[item] = new(path, modelPath, kind, LayoutInstances(layoutPath));
            document.byPath[pointer] = item;
            if (value is JsonObject properties)
                foreach (var pair in properties)
                    Visit(pair.Value, pair.Key, [.. path, pair.Key], item, modelPath,
                        path.Length == 0 && pair.Key == "modelTree", layoutPath);
            else if (value is JsonArray array)
                for (var i = 0; i < array.Count; i++)
                    Visit(array[i], "[" + i + "]", [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                        item, modelPath, path[0] == "modelTree" && path[^1] == "children", layoutPath);
        }
        foreach (var pair in root) Visit(pair.Value, pair.Key, [pair.Key], null, null, pair.Key == "modelTree", null);
        foreach (var info in document.nodes.Values)
        {
            if (info.ModelPath is not null) document.inspectionPaths.Add(info.ModelPath);
            foreach (var path in info.LayoutPaths) document.inspectionPaths.Add(path);
        }
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
            + (info.ModelPath is not null ? "\n\n対応するモデル: " + info.ModelPath
                : info.LayoutPaths.Length > 0
                    ? "\n\nファイル上の対応レイアウト（" + info.LayoutPaths.Length + " 件）:\n" + string.Join("\n", info.LayoutPaths)
                    : "\n\n対応するモデル・レイアウト: なし");
    }

    public string? InspectionPath(TreeItem? item) => item is not null && nodes.TryGetValue(item, out var info)
        ? info.ModelPath ?? info.LayoutPaths.FirstOrDefault() : null;
    public IReadOnlyList<string> LayoutInspectionPaths(TreeItem? item) => item is not null && nodes.TryGetValue(item, out var info)
        ? info.LayoutPaths : [];
    public bool ContainsInspectionPath(string path) => inspectionPaths.Contains(path);
    public string? CopyPath(TreeItem? item) => item is not null && nodes.TryGetValue(item, out var info)
        ? Pointer(info.Path) : null;

    private static string Pointer(IReadOnlyList<string> path)
        => "/" + string.Join("/", path.Select(part => part.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal)));
}
