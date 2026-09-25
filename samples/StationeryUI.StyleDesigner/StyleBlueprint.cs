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
                var path = parent is null ? "/" + (string)node!["id"]! : parent + "/" + (string)node!["id"]!;
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
    public IReadOnlyList<string> EditableLayouts => imported is null ? ["/mainGrid"] : LayoutNodes(imported)
        .Where(l => (string?)l.Node["type"] == "grid-layout" && l.Node["row-definitions"]!.AsArray().Count <= 8 && l.Node["column-definitions"]!.AsArray().Count <= 8)
        .Select(l => l.Path).ToArray();

    public static StyleBlueprint Open(string path) => Parse(File.ReadAllText(path));

    public sealed record Preview(string Json, StationeryStyleSettings Settings, StationeryLayoutResult Layout,
        string ScopePath, bool Standalone, IReadOnlyList<(int Row, int Column, StationeryUI.Canvas.ScreenRectangle Bounds)> Cells);

    public Preview CreatePreview(double width, double height, string? targetLayoutId = null)
    {
        var json = BuildJson();
        var settings = StationeryStyleSettings.Parse(json);
        var selectedId = targetLayoutId ?? (IsImported ? SelectedLayoutId : "/mainGrid");
        var binding = settings.Bindings.FirstOrDefault(b => ("/" + b.Layout.Split('/')[1]) == (selectedId is null ? null : "/" + selectedId.Split('/')[1]));
        var standalone = selectedId is not null && binding is null;
        if (standalone)
        {
            var layout = FindLayout(JsonNode.Parse(json)!, "/" + selectedId!.Split('/')[1])!.DeepClone();
            var root = new JsonObject
            {
                ["models"] = new JsonArray(new JsonObject { ["id"] = "previewRoot", ["type"] = "viewport" }),
                ["layouts"] = new JsonArray(layout),
                ["bindingsV2"] = new JsonObject
                {
                    ["ctrlPreviewRoot"] = new JsonObject
                    {
                        ["modelPath"] = "/previewRoot",
                        ["layoutPath"] = $"root:{selectedId!.Split('/')[1]}"
                    }
                }
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
        var settings = StationeryStyleSettings.Parse(json);
        var imported = JsonNode.Parse(json)!.AsObject();
        var migrationDraft = (JsonObject)imported.DeepClone();
        if (!migrationDraft.ContainsKey("bindingsV2") && migrationDraft["bindings"] is JsonArray &&
            TryMigrateLegacyBindings(migrationDraft, settings, out var migrated))
        {
            imported = migrationDraft;
            imported.Remove("bindings");
            imported["bindingsV2"] = migrated;
            json = imported.ToJsonString();
            StationeryStyleSettings.Parse(json);
        }
        var plan = new StyleBlueprint { imported = imported };
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

    private static bool TryMigrateLegacyBindings(JsonObject imported, StationeryStyleSettings settings, out JsonObject migrated)
    {
        migrated = new JsonObject();
        var modelRoot = settings.Models[0].CreateTree();
        var layouts = settings.Layouts.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var placementsByOwner = settings.Bindings.GroupBy(binding => binding.ModelPath)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var synthesizeRoot = !placementsByOwner.ContainsKey(modelRoot.Path);
        if (synthesizeRoot && modelRoot.Children.Count > 1) return false;
        string? syntheticRootId = null;
        if (synthesizeRoot)
        {
            var rootLayouts = settings.Layouts.Where(layout => layout.ParentPath is null).Select(layout => layout.Id).ToHashSet(StringComparer.Ordinal);
            syntheticRootId = "bindingsV2Root";
            for (var suffix = 2; rootLayouts.Contains(syntheticRootId); suffix++) syntheticRootId = "bindingsV2Root" + suffix;
            imported["layouts"]!.AsArray().Add(new JsonObject { ["id"] = syntheticRootId, ["type"] = "box-layout" });
        }

        bool TryRoute(StationeryUI.Inspection.StationeryNode model, out string route)
        {
            if (model.Parent is null)
            {
                var rootLayoutId = syntheticRootId ?? GetRootLayoutId(model.Path);
                if (rootLayoutId is null) { route = ""; return false; }
                route = $"root:{rootLayoutId}";
                return true;
            }
            if (!TryRoute(model.Parent, out var parentRoute)) { route = ""; return false; }
            var edge = model.Parent.Path == modelRoot.Path && synthesizeRoot
                ? modelRoot.Children.Count == 1 ? "single" : null
                : GetPlacementRoute(model.Parent.Path, model.Path);
            if (edge is null) { route = ""; return false; }
            var layoutId = GetRootLayoutId(model.Path);
            route = parentRoute + "/" + edge + (layoutId is null ? "" : ":" + layoutId);
            return true;
        }

        string? GetRootLayoutId(string ownerPath)
        {
            if (!placementsByOwner.TryGetValue(ownerPath, out var ownerPlacements)) return null;
            var ids = ownerPlacements.Select(binding =>
            {
                var layout = layouts[binding.Layout];
                while (layout.ParentPath is { } parentPath) layout = layouts[parentPath];
                return layout.Id;
            }).Distinct(StringComparer.Ordinal).ToArray();
            return ids.Length == 1 ? ids[0] : null;
        }

        string? GetPlacementRoute(string ownerPath, string childPath)
        {
            if (!placementsByOwner.TryGetValue(ownerPath, out var ownerPlacements)) return null;
            foreach (var placement in ownerPlacements)
            {
                var layout = layouts[placement.Layout];
                string? edge = null;
                if (placement.Children.FirstOrDefault(child => child.ModelPath == childPath) is { } cell)
                {
                    if (layout.Type == "box-layout") edge = "single";
                    else if (layout.Type == "grid-layout") edge = GridEdge(cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan);
                    else if (layout.Type == "tabbed-box-layout") edge = cell.Row.ToString(CultureInfo.InvariantCulture);
                }
                if (placement.FirstModel == childPath) edge = "first";
                if (placement.SecondModel == childPath) edge = "second";
                if (placement.InspectorModel == childPath) edge = "bottom";
                var dock = placement.DockChildren.FirstOrDefault(child => child.ModelPath == childPath);
                if (dock is not null) edge = dock.CellIndex >= 0 ? $"{dock.Dock}.{dock.CellIndex + 1}" : dock.Dock;
                if (edge is null || !TryNestedLayoutRoute(ownerPath, placement.Layout, out var nested)) continue;
                return nested.Length == 0 ? edge : nested + "/" + edge;
            }
            return null;
        }

        bool TryNestedLayoutRoute(string ownerPath, string boundLayoutPath, out string route)
        {
            var rootPath = placementsByOwner.GetValueOrDefault(ownerPath)?.Select(binding => binding.Layout)
                .Where(path => boundLayoutPath == path || boundLayoutPath.StartsWith(path + "/", StringComparison.Ordinal))
                .OrderBy(path => path.Length).FirstOrDefault();
            if (rootPath is null) { route = ""; return false; }
            if (rootPath == boundLayoutPath) { route = ""; return true; }
            var nested = new List<StationeryLayoutNode>();
            var current = layouts[boundLayoutPath];
            while (current.Path != rootPath)
            {
                nested.Add(current);
                if (current.ParentPath is not { } parent || !layouts.TryGetValue(parent, out current!))
                { route = ""; return false; }
            }
            nested.Reverse();
            var segments = new List<string>();
            foreach (var child in nested)
            {
                var parent = layouts[child.ParentPath!];
                var edge = parent.Type == "box-layout" ? "single"
                    : parent.Type == "grid-layout" ? GridEdge(child.Row, child.Column, child.RowSpan, child.ColumnSpan) : null;
                if (edge is null) { route = ""; return false; }
                segments.Add(edge + ":" + child.Id);
            }
            route = string.Join('/', segments);
            return true;
        }

        static string GridEdge(int row, int column, int rowSpan, int columnSpan)
            => $"{row + 1}y.{column + 1}x.{rowSpan}w.{columnSpan}h";

        var allModels = new List<StationeryUI.Inspection.StationeryNode>();
        void Visit(StationeryUI.Inspection.StationeryNode model)
        {
            allModels.Add(model);
            foreach (var child in model.Children) Visit(child);
        }
        Visit(modelRoot);
        foreach (var model in allModels)
        {
            if (!TryRoute(model, out var route)) continue;
            var handle = "model" + Convert.ToHexString(Encoding.UTF8.GetBytes(model.Path));
            migrated.Add(handle, new JsonObject { ["modelPath"] = model.Path, ["layoutPath"] = route });
        }

        try
        {
            var candidate = (JsonObject)imported.DeepClone();
            candidate.Remove("bindings");
            candidate["bindingsV2"] = migrated.DeepClone();
            var converted = StationeryStyleSettings.Parse(candidate.ToJsonString());
            if (converted.Bindings.Count != settings.Bindings.Count + (synthesizeRoot ? 1 : 0))
            { migrated = new(); return false; }
            return true;
        }
        catch (JsonException) { migrated = new(); return false; }
    }
    public void SelectLayout(string id)
    {
        if (!IsImported) throw new ArgumentException("レイアウトを読み込んでください。");
        var committed = JsonNode.Parse(BuildJson())!.AsObject();
        var layout = FindLayout(committed, id)
            ?? throw new ArgumentException("レイアウトがありません。");
        if ((string?)layout["type"] == "box-layout")
        {
            imported = committed; SelectedLayoutId = id; InitializeEdges(layout);
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
        InitializeEdges(layout);
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

    public StyleBlueprint()
    {
        InitializeEdges(new JsonObject
        {
            ["padding"] = new JsonObject
            {
                ["top"] = "0px", ["right"] = "0px",
                ["bottom"] = "0px", ["left"] = "0px"
            }
        });
        Resize(2, 2);
    }

    private void InitializeEdges(JsonNode layout)
    {
        PanelEdges.Clear(); originalPanelNumbers.Clear();
        foreach (var group in new[] { "margin", "padding", "border" })
            foreach (var side in new[] { "top", "right", "bottom", "left" })
            {
                var value = (string?)layout[group]?[side] ?? (group == "padding" ? "8px" : "0px");
                PanelEdges[group + "." + side] = new() { Number = value[..^2], IsRate = false };
                originalPanelNumbers[group + "." + side] = value[..^2];
            }
    }
    public void Resize(int columns, int rows)
    {
        if (columns is < 1 or > 8 || rows is < 1 or > 8)
            throw new ArgumentException("この設計ツールでは横・縦とも 1～8 セルを指定してください。");
        if (IsImported && SelectedLayoutId is not null)
        {
            var settings = StationeryStyleSettings.Parse(BuildJson());
            var selectedLayout = settings.Layouts.Single(l => l.Path == SelectedLayoutId);
            if (selectedLayout.Children.Any(c => c.RowSpan > rows - c.Row || c.ColumnSpan > columns - c.Column) ||
                selectedLayout.Cells.Any(c => c.RowSpan > rows - c.Row || c.ColumnSpan > columns - c.Column))
                throw new ArgumentException("Nested layout or slot would be outside the resized grid.");
            if (settings.Bindings.Where(b => b.Layout == SelectedLayoutId).SelectMany(b => b.Children).Any(c => c.RowSpan > rows - c.Row || c.ColumnSpan > columns - c.Column))
                throw new ArgumentException("配置済みの文房具が表の外に出ます。既存の配置を保つため、このサイズには縮小できません。");
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
            if (SelectedLayoutId is not null && PanelEdges.Count > 0)
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
            if (SelectedLayoutId is not null && !CanEditPanel)
            {
                var layout = FindLayout(draft, SelectedLayoutId)!;
                layout["row-definitions"] = new JsonArray(Rows.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>());
                layout["column-definitions"] = new JsonArray(Columns.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>());
            }
            return Serialize(draft);
        }
        var models = new JsonArray();
        var bindingsV2 = new JsonObject();
        var cells = new JsonArray();
        for (var row = 0; row < Rows.Count; row++)
            for (var column = 0; column < Columns.Count; column++)
            {
                var cell = At(row, column);
                if (!Kinds.Contains(cell.Kind)) throw new JsonException("未対応の文房具の種類です。");
                var id = $"cellR{row + 1}C{column + 1}";
                models.Add(new JsonObject { ["id"] = id, ["type"] = cell.Kind, ["label"] = cell.Label });
                cells.Add(new JsonObject { ["row"] = row, ["col"] = column });
                var handle = StationeryControlHandle.FromModelId(id);
                bindingsV2[handle] = new JsonObject
                {
                    ["modelPath"] = $"/design/mainPage/{id}",
                    ["layoutPath"] = $"root:designRoot/single:mainGrid/{row + 1}y.{column + 1}x.1w.1h"
                };
            }
        var root = new JsonObject
        {
            ["models"] = new JsonArray(new JsonObject
            {
                ["id"] = "design", ["type"] = "viewport", ["children"] = new JsonArray(new JsonObject
                { ["id"] = "mainPage", ["type"] = "page", ["children"] = models })
            }),
            ["layouts"] = new JsonArray(
                new JsonObject { ["id"] = "designRoot", ["type"] = "box-layout" },
                new JsonObject
                {
                    ["id"] = "mainGrid", ["type"] = "grid-layout", ["cells"] = cells,
                    ["row-definitions"] = new JsonArray(Rows.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>()),
                    ["column-definitions"] = new JsonArray(Columns.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>()),
                    ["margin"] = EdgeObject("margin"), ["padding"] = EdgeObject("padding")
                }),
            ["bindingsV2"] = bindingsV2
        };
        return Serialize(root);
    }

    private JsonObject EdgeObject(string group) => new()
    {
        ["top"] = PanelEdges[group + ".top"].Length(), ["right"] = PanelEdges[group + ".right"].Length(),
        ["bottom"] = PanelEdges[group + ".bottom"].Length(), ["left"] = PanelEdges[group + ".left"].Length()
    };

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
        var fullPath = parentPath is null ? "/" + id : parentPath + "/" + id;
        SelectLayout(fullPath);
        return fullPath;
    }

    public string? LayoutPathFor(IReadOnlyList<string> path)
    {
        if (path.Count == 0 || path[0] != "layouts" || NodeId(path) is null) return null;
        var root = JsonNode.Parse(BuildJson())!;
        var node = NodeAt(root, path);
        return LayoutNodes(root).FirstOrDefault(l => ReferenceEquals(l.Node, node)).Path;
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
        // Only layouts/models and slots inside a cell can carry editable identities.
        IReadOnlyList<string> ownerPath = path[0] == "layouts" && path.Count >= 6 && path[^2] == "slots" && path[^4] == "cells"
            ? path.Take(path.Count - 4).ToArray() : path;
        if (ownerPath.Count % 2 != 0 || ownerPath.Where((_, i) => i > 0 && i % 2 == 0).Any(p => p != "children")) return null;
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
        if (draft.ContainsKey("bindings"))
            throw new InvalidOperationException("This legacy layout cannot be migrated automatically; rename its models or layouts after converting the file to bindingsV2.");
        var settings = StationeryStyleSettings.Parse(draft.ToJsonString());
        var node = NodeAt(draft, path);
        var oldId = (string)node["id"]!;
        if (oldId == id) return;
        var selected = IsImported ? SelectedLayoutId : "/mainGrid";
        if (path[0] == "layouts")
        {
            if (path.Count >= 2 && path[^2] == "slots")
            {
                node["id"] = id; Serialize(draft); imported = draft; return;
            }
            var oldPath = LayoutNodes(draft).Single(l => ReferenceEquals(l.Node, node)).Path;
            var newPath = oldPath[..^oldId.Length] + id;
            RewriteBindingsV2Routes(draft, settings, oldPath, null, oldId, id);
            if (selected is not null && (selected == oldPath || selected.StartsWith(oldPath + "/", StringComparison.Ordinal)))
                selected = newPath + selected[oldPath.Length..];
        }
        else
        {
            var ids = new List<string>();
            for (JsonNode? ancestor = node; ancestor is not null && ancestor != draft; ancestor = ancestor.Parent)
                if (ancestor is JsonObject obj && obj["id"] is JsonValue value) ids.Insert(0, value.GetValue<string>());
            var oldPath = "/" + string.Join("/", ids);
            RewriteBindingsV2Routes(draft, settings, null, oldPath, oldId, id);
        }
        node["id"] = id;
        Serialize(draft);
        imported = draft; SelectedLayoutId = null;
        if (selected is not null) SelectLayout(selected);
    }

    private static void RewriteBindingsV2Routes(JsonObject draft, StationeryStyleSettings settings,
        string? oldLayoutPath, string? oldModelPath, string oldId, string newId)
    {
        if (draft["bindingsV2"] is not JsonObject bindingsV2) return;

        void RewriteEntry(JsonNode node)
        {
            if (node is JsonObject obj && obj["modelPath"] is JsonValue modelValue && modelValue.TryGetValue<string>(out var modelPath))
            {
                if (oldModelPath is not null && (modelPath == oldModelPath || modelPath.StartsWith(oldModelPath + "/", StringComparison.Ordinal)))
                    obj["modelPath"] = oldModelPath[..^oldId.Length] + newId + modelPath[oldModelPath.Length..];
                if (obj["layoutPath"] is JsonValue routeValue && routeValue.TryGetValue<string>(out var route))
                    obj["layoutPath"] = RewriteLayoutRoute(route);
                return;
            }
            if (node is JsonObject keyed)
                foreach (var objectEntry in keyed.ToArray()) if (objectEntry.Value is not null) RewriteEntry(objectEntry.Value);
            else if (node is JsonArray array)
                foreach (var arrayItem in array) if (arrayItem is not null) RewriteEntry(arrayItem);
        }

        foreach (var entry in bindingsV2.ToArray()) if (entry.Value is not null) RewriteEntry(entry.Value);

        string RewriteLayoutRoute(string route)
        {
            if (oldLayoutPath is null) return route;
            var oldLayoutId = oldLayoutPath.Split('/').Last();
            return route.Replace("root:" + oldLayoutId, "root:" + newId, StringComparison.Ordinal)
                .Replace(":" + oldLayoutId, ":" + newId, StringComparison.Ordinal);
        }
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
