namespace StationeryUI.StyleDesigner;

using StationeryUI.Styling;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;

/// <summary>A detached new or imported design. Export always creates a new file.</summary>
public sealed class StyleBlueprint
{
    private JsonObject? imported;
    public bool IsImported => imported is not null;
    public string? SelectedLayoutId { get; private set; }
    public static IEnumerable<(string Path, JsonNode Node)> LayoutNodes(JsonNode root)
    {
        IEnumerable<(string, JsonNode)> Visit(JsonArray array, string? parent)
        {
            foreach (var node in array)
            {
                var path = parent is null ? (string)node!["id"]! : parent + "." + (string)node!["id"]!;
                yield return (path, node!);
                if (node!["children"] is JsonArray children)
                    foreach (var child in Visit(children, path)) yield return child;
            }
        }
        return Visit(root["layouts"]!.AsArray(), null);
    }
    public static JsonNode? FindLayout(JsonNode root, string? path) => LayoutNodes(root).FirstOrDefault(l => l.Path == path).Node;
    public string? SelectedLayoutType => !IsImported ? "grid-layout" : (string?)FindLayout(imported!, SelectedLayoutId)?["type"];
    public bool CanEditPanel => SelectedLayoutType == "box-layout";
    public bool CanEditGrid => !IsImported || SelectedLayoutType == "grid-layout";
    public Dictionary<string, Track> PanelEdges { get; } = [];
    private readonly Dictionary<string, string> originalPanelNumbers = [];
    public IReadOnlyList<string> EditableLayouts => imported is null ? ["mainGrid"] : LayoutNodes(imported)
        .Where(l => (string?)l.Node["type"] == "grid-layout" && l.Node["row-definitions"]!.AsArray().Count <= 8 && l.Node["column-definitions"]!.AsArray().Count <= 8)
        .Select(l => l.Path).ToArray();

    public static StyleBlueprint Open(string path) => Parse(File.ReadAllText(path));

    public sealed record Preview(string Json, StationeryStyleSettings Settings, StationeryLayoutResult Layout,
        string ScopePath, bool Standalone, IReadOnlyList<(int Row, int Column, StationeryUI.Canvas.ScreenRectangle Bounds)> Cells);

    public Preview CreatePreview(double width, double height)
    {
        var json = BuildJson();
        var settings = StationeryStyleSettings.Parse(json);
        var selectedId = IsImported ? SelectedLayoutId : "mainGrid";
        var binding = settings.Bindings.FirstOrDefault(b => b.Layout.Split('.')[0] == selectedId?.Split('.')[0]);
        var standalone = selectedId is not null && binding is null;
        if (standalone)
        {
            var layout = FindLayout(JsonNode.Parse(json)!, selectedId!.Split('.')[0])!.DeepClone();
            var root = new JsonObject
            {
                ["models"] = new JsonArray(new JsonObject { ["id"] = "previewRoot", ["type"] = "viewport" }),
                ["layouts"] = new JsonArray(layout),
                ["bindings"] = new JsonArray(CanEditPanel
                    ? new JsonObject { ["layout"] = selectedId, ["model"] = "previewRoot" }
                    : new JsonObject { ["layout"] = selectedId, ["parentModel"] = "previewRoot", ["childrenModel"] = new JsonArray() })
            };
            json = root.ToJsonString();
            settings = StationeryStyleSettings.Parse(json);
            binding = settings.Bindings[0];
        }
        var tree = settings.Models[0].CreateTree();
        var scope = tree;
        if (binding is not null)
        {
            for (var node = tree.Resolve(binding.ModelPath); node is not null; node = node.Parent)
                if (node.Kind is "page" or "dialog") { scope = node; break; }
        }
        if (scope == tree) scope = tree.Children.FirstOrDefault(n => n.Kind == "page") ?? tree;
        var arranged = StationeryLayoutEngine.Arrange(settings, width, height);
        var cells = binding is not null && settings.Layouts.Single(l => l.Path == selectedId).Type == "grid-layout"
            ? StationeryLayoutEngine.ArrangeGridCells(settings.Layouts.Single(l => l.Path == selectedId), arranged.LayoutContentBounds[binding.ModelPath + ":" + selectedId])
            : Array.Empty<(int Row, int Column, StationeryUI.Canvas.ScreenRectangle Bounds)>();
        return new(json, settings, arranged, scope.Path, standalone, cells);
    }
    public static StyleBlueprint Parse(string json)
    {
        StationeryStyleSettings.Parse(json);
        var plan = new StyleBlueprint { imported = JsonNode.Parse(json)!.AsObject() };
        // Normalize layout types only; preserve model IDs, labels and extension properties.
        foreach (var (_, layout) in LayoutNodes(plan.imported))
        {
            if ((string?)layout!["type"] == "floating-layout") layout["type"] = "grid-layout";
            if ((string?)layout!["type"] == "panel") layout["type"] = "box-layout";
        }
        if (plan.EditableLayouts.Count > 0) plan.SelectLayout(plan.EditableLayouts[0]);
        else if (LayoutNodes(plan.imported).FirstOrDefault(l => (string?)l.Node["type"] == "box-layout") is { Node: not null } panel)
            plan.SelectLayout(panel.Path);
        return plan;
    }
    public void SelectLayout(string id)
    {
        if (!IsImported) throw new ArgumentException("レイアウトを読み込んでください。");
        var committed = JsonNode.Parse(BuildJson())!.AsObject();
        var layout = FindLayout(committed, id)
            ?? throw new ArgumentException("レイアウトがありません。");
        if ((string?)layout["type"] == "box-layout")
        {
            imported = committed; SelectedLayoutId = id; PanelEdges.Clear(); originalPanelNumbers.Clear();
            foreach (var group in new[] { "margin", "padding", "border" })
                foreach (var side in new[] { "top", "right", "bottom", "left" })
                {
                    var value = (string?)layout[group]?[side] ?? (group == "padding" ? "8px" : "0px");
                    PanelEdges[group + "." + side] = new() { Number = value[..^2], IsRate = false };
                    originalPanelNumbers[group + "." + side] = value[..^2];
                }
            return;
        }
        if (!EditableLayouts.Contains(id)) throw new ArgumentException("このレイアウトは表では編集できません。");
        imported = committed;
        SelectedLayoutId = null;
        Resize(layout["column-definitions"]!.AsArray().Count, layout["row-definitions"]!.AsArray().Count);
        void Read(string key, List<Track> target)
        {
            for (var i = 0; i < target.Count; i++)
            {
                var value = (string)layout[key]![i]!;
                target[i].IsRate = value.EndsWith("rate", StringComparison.Ordinal);
                target[i].Number = value[..^(target[i].IsRate ? 4 : 2)];
            }
        }
        Read("row-definitions", Rows); Read("column-definitions", Columns);
        SelectedLayoutId = id;
    }

    public string CellDescription(int row, int column)
    {
        if (!IsImported) return At(row, column).Label;
        var settings = StationeryStyleSettings.Parse(imported!.ToJsonString());
        var paths = settings.Bindings.Where(b => b.Layout == SelectedLayoutId).SelectMany(b => b.Children)
            .Where(c => row >= c.Row && row < c.Row + c.RowSpan && column >= c.Column && column < c.Column + c.ColumnSpan).Select(c => c.ModelPath);
        return string.Join(" / ", paths.Select(p => p.Split('/').Last()));
    }
    public sealed class Track
    {
        public string Number { get; set; } = "1";
        public bool IsRate { get; set; } = true;
        internal string Length()
        {
            if (!double.TryParse(Number, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
                !double.IsFinite(value) || value < 0)
                throw new JsonException("サイズには 0 以上の数値を入力してください（小数点は .）。");
            return Number + (IsRate ? "rate" : "px");
        }
    }
    public sealed class Cell
    {
        public string Kind { get; set; } = "container";
        public string Label { get; set; } = "";
    }
    public static IReadOnlyList<string> Kinds { get; } = Array.AsReadOnly(new[] { "container", "button", "textBox", "textBlock", "link", "tree" });
    public List<Track> Rows { get; } = [];
    public List<Track> Columns { get; } = [];
    private readonly Dictionary<(int Row, int Column), Cell> cells = [];
    public Cell At(int row, int column) => cells[(row, column)];

    public StyleBlueprint() => Resize(2, 2);
    public void Resize(int columns, int rows)
    {
        if (columns is < 1 or > 8 || rows is < 1 or > 8)
            throw new ArgumentException("この設計ツールでは横・縦とも 1～8 セルを指定してください。");
        if (IsImported && SelectedLayoutId is not null)
        {
            var settings = StationeryStyleSettings.Parse(BuildJson());
            if (settings.Layouts.Single(l => l.Path == SelectedLayoutId).Children.Any(c => c.RowSpan > rows - c.Row || c.ColumnSpan > columns - c.Column))
                throw new ArgumentException("Nested layout would be outside the resized grid.");
            if (settings.Bindings.Where(b => b.Layout == SelectedLayoutId).SelectMany(b => b.Children).Any(c => c.RowSpan > rows - c.Row || c.ColumnSpan > columns - c.Column))
                throw new ArgumentException("配置済みの文房具が表の外に出ます。既存の bindings を保つため、このサイズには縮小できません。");
        }
        static void ResizeTracks(List<Track> tracks, int count)
        {
            while (tracks.Count < count) tracks.Add(new());
            if (tracks.Count > count) tracks.RemoveRange(count, tracks.Count - count);
        }
        ResizeTracks(Columns, columns); ResizeTracks(Rows, rows);
        foreach (var key in cells.Keys.Where(k => k.Row >= rows || k.Column >= columns).ToArray()) cells.Remove(key);
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++) cells.TryAdd((row, column), new());
    }

    public string BuildJson()
    {
        if (imported is not null)
        {
            var draft = (JsonObject)imported.DeepClone();
            if (CanEditPanel)
            {
                var panel = FindLayout(draft, SelectedLayoutId)!;
                foreach (var group in new[] { "margin", "padding", "border" })
                {
                    foreach (var side in new[] { "top", "right", "bottom", "left" })
                    {
                        var key = group + "." + side;
                        var length = PanelEdges[key].Length();
                        if (PanelEdges[key].Number == originalPanelNumbers[key]) continue;
                        if (panel[group] is null) panel[group] = new JsonObject();
                        panel[group]![side] = length;
                    }
                }
            }
            else if (SelectedLayoutId is not null)
            {
                var layout = FindLayout(draft, SelectedLayoutId)!;
                layout["row-definitions"] = new JsonArray(Rows.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>());
                layout["column-definitions"] = new JsonArray(Columns.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>());
            }
            return Serialize(draft);
        }
        var models = new JsonArray();
        var bindings = new JsonArray();
        for (var row = 0; row < Rows.Count; row++)
            for (var column = 0; column < Columns.Count; column++)
            {
                var cell = At(row, column);
                if (!Kinds.Contains(cell.Kind)) throw new JsonException("未対応の文房具の種類です。");
                var id = $"cellR{row + 1}C{column + 1}";
                models.Add(new JsonObject { ["id"] = id, ["type"] = cell.Kind, ["label"] = cell.Label });
                bindings.Add(new JsonObject { ["model"] = id, ["row"] = row, ["column"] = column });
            }
        var root = new JsonObject
        {
            ["models"] = new JsonArray(new JsonObject
            {
                ["id"] = "design", ["type"] = "viewport", ["children"] = new JsonArray(new JsonObject
                { ["id"] = "mainPage", ["type"] = "page", ["children"] = models })
            }),
            ["layouts"] = new JsonArray(new JsonObject
            {
                ["id"] = "mainGrid", ["type"] = "grid-layout",
                ["row-definitions"] = new JsonArray(Rows.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>()),
                ["column-definitions"] = new JsonArray(Columns.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>())
            }),
            ["bindings"] = new JsonArray(new JsonObject
            { ["layout"] = "mainGrid", ["parentModel"] = "design/mainPage", ["childrenModel"] = bindings })
        };
        return Serialize(root);
    }

    private static string Serialize(JsonObject root)
    {
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        StationeryStyleSettings.Parse(json);
        // Use four spaces, matching the style file convention, without altering label strings.
        return string.Join(Environment.NewLine, json.ReplaceLineEndings("\n").Split('\n').Select(line =>
        {
            var spaces = line.TakeWhile(c => c == ' ').Count();
            return new string(' ', spaces) + line;
        })) + Environment.NewLine;
    }

    public string AddLayout(string type, string? requestedId = null, string? parentPath = null, int row = 0, int col = 0, int rowSpan = 1, int colSpan = 1)
    {
        if (type == "floating-layout") type = "grid-layout";
        if (type == "panel") type = "box-layout";
        if (type is not ("box-layout" or "grid-layout")) throw new ArgumentException("追加できない種類です。");
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var parent = parentPath is null ? null : FindLayout(draft, parentPath) ?? throw new ArgumentException("Unknown parent layout.");
        if (parent is not null && (string?)parent["type"] is not ("box-layout" or "grid-layout")) throw new ArgumentException("Parent must be a box or grid.");
        if (parent is not null && parent["children"] is null) parent["children"] = new JsonArray();
        var layouts = parent is null ? draft["layouts"]!.AsArray() : parent["children"]!.AsArray();
        var prefix = type == "box-layout" ? "boxLayout" : "gridLayout";
        var number = 1;
        while (layouts.Any(l => (string?)l!["id"] == prefix + number)) number++;
        var id = requestedId ?? prefix + number;
        var error = CheckId(id, layouts.Select(l => (string)l!["id"]!)).Error;
        if (error is not null) throw new ArgumentException(error);
        var added = new JsonObject { ["id"] = id, ["type"] = type };
        if (type == "grid-layout")
        {
            added["row-definitions"] = new JsonArray("1rate", "1rate");
            added["column-definitions"] = new JsonArray("1rate", "1rate");
        }
        else foreach (var group in new[] { "margin", "padding", "border" })
            added[group] = new JsonObject { ["top"] = "0px", ["right"] = "0px", ["bottom"] = "0px", ["left"] = "0px" };
        if ((string?)parent?["type"] == "grid-layout")
        {
            added["row"] = row; added["col"] = col; added["rowspan"] = rowSpan; added["colspan"] = colSpan;
        }
        layouts.Add(added);
        Serialize(draft);
        imported = draft; SelectedLayoutId = null;
        var fullPath = parentPath is null ? id : parentPath + "." + id;
        SelectLayout(fullPath);
        return fullPath;
    }

    public string? LayoutPathFor(IReadOnlyList<string> path)
    {
        if (path.Count == 0 || path[0] != "layouts" || NodeId(path) is null) return null;
        var root = JsonNode.Parse(BuildJson())!;
        var node = NodeAt(root, path);
        return LayoutNodes(root).Single(l => ReferenceEquals(l.Node, node)).Path;
    }
    public (string? Error, string? Warning) ValidateChildId(string id, string? parentPath)
    {
        var root = JsonNode.Parse(BuildJson())!;
        var siblings = parentPath is null ? root["layouts"]!.AsArray() : FindLayout(root, parentPath)?["children"] as JsonArray;
        return CheckId(id, siblings?.Select(n => (string)n!["id"]!) ?? []);
    }
    public void SetPlacement(string path, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var node = FindLayout(draft, path) ?? throw new ArgumentException("Unknown layout.");
        node["row"] = row; node.AsObject().Remove("column"); node["col"] = col;
        node["rowspan"] = rowSpan; node["colspan"] = colSpan;
        Serialize(draft);
        imported = draft;
    }
    public static (string? Error, string? Warning) CheckId(string id, IEnumerable<string> siblings)
    {
        if (id.Length == 0 || id.Any(c => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
            return ("Id は英字・数字・アンダースコアだけで、1文字以上入力してください。", null);
        if (siblings.Contains(id, StringComparer.Ordinal)) return ("同じ親の中に、この Id が既にあります。", null);
        var warnings = new List<string>();
        if (char.IsAsciiDigit(id[0])) warnings.Add("数字で始まっています");
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z][a-z0-9]*(?:[A-Z][a-z0-9]+)*$")) warnings.Add("camelCase ではありません");
        return (null, warnings.Count == 0 ? null : "警告：" + string.Join("。", warnings) + "（このまま確定できます）。");
    }

    private static JsonNode NodeAt(JsonNode root, IReadOnlyList<string> path)
    {
        foreach (var part in path) root = root is JsonArray array ? array[int.Parse(part, CultureInfo.InvariantCulture)]! : root[part]!;
        return root;
    }

    public string? NodeId(IReadOnlyList<string> path)
    {
        if (path.Count < 2 || path[0] is not ("models" or "layouts")) return null;
        // Only model nodes and layout definitions, not metadata objects with an id property.
        if (path.Count % 2 != 0 || path.Where((_, i) => i > 0 && i % 2 == 0).Any(p => p != "children")) return null;
        return NodeAt(JsonNode.Parse(BuildJson())!, path) is JsonObject obj ? (string?)obj["id"] : null;
    }

    public (string? Error, string? Warning) ValidateId(string id, IReadOnlyList<string>? path = null)
    {
        var root = JsonNode.Parse(BuildJson())!;
        var array = path is null ? root["layouts"]!.AsArray() : NodeAt(root, path.Take(path.Count - 1).ToArray()).AsArray();
        var current = path is null ? null : NodeAt(root, path);
        return CheckId(id, array.Where(n => n != current).Select(n => (string)n!["id"]!));
    }

    public void RenameId(IReadOnlyList<string> path, string id)
    {
        if (NodeId(path) is null) throw new ArgumentException("Id を持つモデルかレイアウトを操作対象にしてください。");
        var error = ValidateId(id, path).Error;
        if (error is not null) throw new ArgumentException(error);
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var settings = StationeryStyleSettings.Parse(draft.ToJsonString());
        var node = NodeAt(draft, path);
        var oldId = (string)node["id"]!;
        if (oldId == id) return;
        var selected = IsImported ? SelectedLayoutId : "mainGrid";
        if (path[0] == "layouts")
        {
            var oldPath = LayoutNodes(draft).Single(l => ReferenceEquals(l.Node, node)).Path;
            var newPath = oldPath[..^oldId.Length] + id;
            string RewriteLayout(string value) => value == oldPath || value.StartsWith(oldPath + ".", StringComparison.Ordinal)
                ? newPath + value[oldPath.Length..] : value;
            foreach (var binding in draft["bindings"]!.AsArray())
                binding!["layout"] = RewriteLayout((string)binding["layout"]!);
            if (selected is not null) selected = RewriteLayout(selected);
        }
        else
        {
            var ids = new List<string>();
            for (JsonNode? ancestor = node; ancestor is not null && ancestor != draft; ancestor = ancestor.Parent)
                if (ancestor is JsonObject obj && obj["id"] is JsonValue value) ids.Insert(0, value.GetValue<string>());
            var oldPath = "/" + string.Join("/", ids);
            ids[^1] = id;
            var newPath = "/" + string.Join("/", ids);
            void Rewrite(JsonNode binding, string key, string? resolved)
            {
                if (resolved is not null && (resolved == oldPath || resolved.StartsWith(oldPath + "/", StringComparison.Ordinal)))
                    binding[key] = newPath + resolved[oldPath.Length..];
            }
            for (var i = 0; i < settings.Bindings.Count; i++)
            {
                var binding = draft["bindings"]![i]!;
                var resolved = settings.Bindings[i];
                Rewrite(binding, binding["parentModel"] is null ? "model" : "parentModel", resolved.ModelPath);
                Rewrite(binding, "firstModel", resolved.FirstModel); Rewrite(binding, "secondModel", resolved.SecondModel);
                Rewrite(binding, "inspectorModel", resolved.InspectorModel);
                for (var c = 0; c < resolved.Children.Count; c++) Rewrite(binding["childrenModel"]![c]!, "model", resolved.Children[c].ModelPath);
                for (var c = 0; c < resolved.DockChildren.Count; c++) Rewrite(binding["childrenModel"]![c]!, "model", resolved.DockChildren[c].ModelPath);
            }
        }
        node["id"] = id;
        Serialize(draft);
        imported = draft; SelectedLayoutId = null;
        if (selected is not null) SelectLayout(selected);
    }

    public void DeleteNode(IReadOnlyList<string> path)
    {
        if (path.Count < 2) throw new ArgumentException("トップ階層は削除できません。");
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        JsonNode parent = draft;
        foreach (var part in path.Take(path.Count - 1))
            parent = parent is JsonArray array ? array[int.Parse(part, CultureInfo.InvariantCulture)]! : parent[part]!;
        if (parent is JsonArray children) children.RemoveAt(int.Parse(path[^1], CultureInfo.InvariantCulture));
        else parent.AsObject().Remove(path[^1]);
        Serialize(draft); // Reject dangling bindings and required-property deletion atomically.
        var keep = SelectedLayoutId;
        imported = draft; SelectedLayoutId = null; PanelEdges.Clear();
        if (keep is not null && FindLayout(draft, keep) is not null) SelectLayout(keep);
        else if (EditableLayouts.Count > 0) SelectLayout(EditableLayouts[0]);
    }

    /// <summary>Export a new file only; an existing target is never overwritten.</summary>
    public void Export(string path)
    {
        if (!path.EndsWith(".stationery-style.json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("出力先の末尾を .stationery-style.json にしてください。");
        var bytes = new UTF8Encoding(false).GetBytes(BuildJson());
        var destination = Path.GetFullPath(path);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, destination, overwrite: false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
