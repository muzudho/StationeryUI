namespace StationeryUI.Editor;

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
    private readonly string rootLayoutId = "layout" + Guid.NewGuid().ToString("N");
    public string DefaultLayoutId { get; } = "layout" + Guid.NewGuid().ToString("N");
    public string DefaultLayoutPath => "/" + DefaultLayoutId;
    public bool IsImported => imported is not null;
    public bool CanAddLayout => imported is null || imported.ContainsKey("layouts");
    public bool CanEditPages => imported?["viewports"] is JsonArray;
    public IReadOnlyList<string> PageNames => imported is null ? [] : ViewportPages(imported)
        .Select(page => (string?)page?["name"] ?? "").ToArray();
    private static JsonArray ViewportPages(JsonObject root)
    {
        if (root["viewports"] is not JsonArray views || views.Count != 1 ||
            views[0]?["children"] is not JsonArray pages ||
            pages.Any(page => (string?)page?["type"] != "Page"))
            throw new ArgumentException("ページの編集には、単一の viewports に Page が並ぶ形式が必要です。");
        return pages;
    }
    public void AddPage(string name)
    {
        if (imported is null) throw new ArgumentException("先に文房具UIファイルを開いてください。");
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z][a-zA-Z0-9]*$"))
            throw new ArgumentException("ページ名は英小文字で始まる英数字にしてください。");
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var pages = ViewportPages(draft);
        var suffix = char.ToUpperInvariant(name[0]) + name[1..];
        var viewName = "v" + suffix;
        var modelName = "mdl" + suffix;
        if (pages.Any(page => (string?)page?["name"] == viewName) ||
            draft["modelTree"]?["children"] is not JsonArray modelPages ||
            modelPages.Any(page => (string?)page?["id"] == modelName))
            throw new ArgumentException("そのページ名はすでに使われています。");
        pages.Add(new JsonObject
        {
            ["type"] = "Page", ["name"] = viewName,
            ["layout"] = new JsonObject { ["name"] = "cs" + suffix + "Layout", ["type"] = "BoxLayout" },
            ["controlHandle"] = "ctrl" + suffix,
            ["modelPath"] = new JsonObject { ["in /ctrlViewPort/ctrl" + suffix] = "/" + (string?)draft["modelTree"]?["id"] + "/" + modelName },
            ["children"] = new JsonArray(), ["place"] = pages.Count
        });
        modelPages.Add(new JsonObject { ["id"] = modelName, ["type"] = "page", ["children"] = new JsonArray() });
        Serialize(draft);
        imported = draft;
    }
    public void DeletePage(string viewName)
    {
        if (imported is null) throw new ArgumentException("先に文房具UIファイルを開いてください。");
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var pages = ViewportPages(draft);
        var index = pages.ToList().FindIndex(page => (string?)page?["name"] == viewName);
        if (index < 0) throw new ArgumentException("ページが見つかりません。");
        if (pages.Count == 1) throw new ArgumentException("最後のページは削除できません。");
        var modelName = "mdl" + viewName[1..];
        if (draft["modelTree"]?["children"] is not JsonArray modelPages)
            throw new ArgumentException("対応する modelTree が見つかりません。");
        var modelIndex = modelPages.ToList().FindIndex(page => (string?)page?["id"] == modelName);
        if (modelIndex < 0) throw new ArgumentException("対応するモデルページが見つかりません。");
        pages.RemoveAt(index);
        modelPages.RemoveAt(modelIndex);
        if (draft["initialViewportSnapshot"] is JsonArray snapshots)
            foreach (var snapshot in snapshots)
                if (snapshot?["children"] is JsonArray visible)
                {
                    var removed = false;
                    foreach (var item in visible.ToArray())
                        if ((string?)item?["name"] == viewName) { visible.Remove(item); removed = true; }
                    if (removed && visible.Count == 0)
                        visible.Add(new JsonObject { ["name"] = (string?)pages[0]?["name"], ["children"] = new JsonArray() });
                }
        Serialize(draft);
        imported = draft;
        if (SelectedLayoutId is not null && FindLayout(draft, SelectedLayoutId) is null)
        {
            SelectedLayoutId = null; PanelEdges.Clear();
            if (EditableLayouts.Count > 0) SelectLayout(EditableLayouts[0]);
        }
    }
    public string? SelectedLayoutId { get; private set; }
    public static IEnumerable<(string Path, JsonNode Node)> LayoutNodes(JsonNode root)
    {
        if ((root["layouts"] ?? root["styles"]) is JsonArray layouts)
        {
            IEnumerable<(string, JsonNode)> VisitLegacy(JsonArray array, string? parent)
            {
                foreach (var node in array)
                {
                    var id = (string?)node!["id"] ?? (string?)node["name"] ?? throw new JsonException("Layout name is missing.");
                    var path = parent is null ? "/" + id : parent + "/" + id;
                    yield return (path, node);
                    if (node["children"] is JsonArray children)
                        foreach (var child in VisitLegacy(children, path)) yield return child;
                }
            }
            return VisitLegacy(layouts, null);
        }
        IEnumerable<(string Path, JsonNode Node)> VisitViews(JsonArray views)
        {
            foreach (var view in views)
            {
                if (view is JsonObject obj && obj["layout"] is JsonObject layout && !layout.ContainsKey("ref"))
                {
                    foreach (var definition in VisitDefinition(layout, null)) yield return definition;
                }
                if (view?["children"] is JsonArray children)
                    foreach (var child in VisitViews(children)) yield return child;
            }
        }
        IEnumerable<(string Path, JsonNode Node)> VisitDefinition(JsonNode layout, string? parent)
        {
            var name = (string?)layout["name"] ?? (string?)layout["id"] ?? throw new JsonException("Layout name is missing.");
            var path = parent is null ? "/" + name : parent + "/" + name;
            yield return (path, layout);
            if (layout["children"] is JsonArray children)
                foreach (var child in children)
                    foreach (var definition in VisitDefinition(child!, path)) yield return definition;
        }
        // References point to existing definitions; they are not anonymous definitions.
        return root["viewports"] is JsonArray viewports ? VisitViews(viewports).DistinctBy(l => l.Path) : [];
    }
    public static JsonNode? FindLayout(JsonNode root, string? path) => LayoutNodes(root).FirstOrDefault(l => l.Path == path).Node;
    public static string? NormalizeLayoutType(string? type) => type switch
    {
        "BoxLayout" => "box-layout", "GridLayout" => "grid-layout", "DockLayout" => "dock-layout",
        "TabbedBoxLayout" => "tabbed-box-layout", "SplitPane" => "split-pane", _ => type
    };
    public string? SelectedLayoutType => !IsImported ? "grid-layout" : NormalizeLayoutType((string?)FindLayout(imported!, SelectedLayoutId)?["type"]);
    public bool CanEditPanel => SelectedLayoutType == "box-layout";
    public bool CanEditGrid => !IsImported || SelectedLayoutType == "grid-layout";
    public Dictionary<string, Track> PanelEdges { get; } = [];
    private readonly Dictionary<string, string> originalPanelNumbers = [];
    public IReadOnlyList<string> EditableLayouts => imported is null ? [DefaultLayoutPath] : LayoutNodes(imported)
        .Where(l => NormalizeLayoutType((string?)l.Node["type"]) == "grid-layout" && l.Node["row-definitions"] is JsonArray rows && rows.Count <= 8 && l.Node["column-definitions"] is JsonArray columns && columns.Count <= 8)
        .Select(l => l.Path).ToArray();

    public static StyleBlueprint Open(string path) => Parse(File.ReadAllText(path));

    public sealed record Preview(string Json, StationeryStyleSettings Settings, StationeryLayoutResult Layout,
        string ScopePath, bool Standalone, IReadOnlyList<(int Row, int Column, StationeryUI.Canvas.ScreenRectangle Bounds)> Cells);

    public Preview CreatePreview(double width, double height, string? targetLayoutId = null)
    {
        var json = BuildJson();
        var settings = StationeryStyleSettings.Parse(json);
        var selectedId = targetLayoutId ?? (IsImported ? SelectedLayoutId : DefaultLayoutPath);
        var binding = settings.Bindings.FirstOrDefault(b => ("/" + b.Layout.Split('/')[1]) == (selectedId is null ? null : "/" + selectedId.Split('/')[1]));
        var standalone = selectedId is not null && binding is null;
        if (standalone)
        {
            var layout = FindLayout(JsonNode.Parse(json)!, "/" + selectedId!.Split('/')[1])!.DeepClone();
            var root = new JsonObject
            {
                ["modelTree"] = new JsonObject { ["id"] = "previewRoot", ["type"] = "viewport" },
                ["layouts"] = new JsonArray(layout),
                ["controlTree"] = new JsonObject
                {
                    ["ctrlPreviewRoot"] = new JsonObject
                    {
                        ["modelPath"] = new JsonObject { ["in /"] = "/previewRoot" },
                        ["layoutPath"] = new JsonObject { ["in /"] = selectedId!.Split('/')[1] }
                    }
                }
            };
            json = root.ToJsonString();
            settings = StationeryStyleSettings.Parse(json);
            binding = settings.Bindings[0];
        }
        var tree = settings.ModelTree.CreateTree();
        var scope = tree;
        if (binding is not null)
        {
            for (var node = tree.Resolve(binding.ModelPath); node is not null; node = node.Parent)
                if (node.Kind is "page" or "dialog") { scope = node; break; }
        }
        if (scope == tree) scope = tree.Children.FirstOrDefault(n => n.Kind == "page") ?? tree;
        // Shared controls can have a different route on each page. The designer
        // has no application state, so use the route within the previewed scope.
        var controlLayoutKeys = settings.ControlTree
            .Where(pair => pair.Value.LayoutPath is null)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ModelPaths
                .FirstOrDefault(route => route.Value == scope.Path || route.Value.StartsWith(scope.Path + "/", StringComparison.Ordinal)).Key
                ?? pair.Value.LayoutPaths.Keys.First(), StringComparer.Ordinal);
        var arranged = StationeryLayoutEngine.Arrange(settings, width, height, controlLayoutKeys);
        var cells = binding is not null && settings.Layouts.Single(l => l.Path == selectedId).Type == "grid-layout"
            ? StationeryLayoutEngine.ArrangeGridCells(settings.Layouts.Single(l => l.Path == selectedId), arranged.LayoutContentBounds[binding.ModelPath + ":" + selectedId])
            : Array.Empty<(int Row, int Column, StationeryUI.Canvas.ScreenRectangle Bounds)>();
        return new(json, settings, arranged, scope.Path, standalone, cells);
    }
    public static StyleBlueprint Parse(string json)
    {
        _ = StationeryStyleSettings.Parse(json);
        var imported = JsonNode.Parse(json)!.AsObject();
        var plan = new StyleBlueprint { imported = imported };
        // Normalize layout types only; preserve model IDs, labels and extension properties.
        foreach (var (_, layout) in LayoutNodes(plan.imported))
        {
            if ((string?)layout!["type"] == "floating-layout") layout["type"] = "grid-layout";
            if ((string?)layout!["type"] == "panel") layout["type"] = "box-layout";
        }
        if (plan.EditableLayouts.Count > 0) plan.SelectLayout(plan.EditableLayouts[0]);
        else if (LayoutNodes(plan.imported).FirstOrDefault(l => NormalizeLayoutType((string?)l.Node["type"]) == "box-layout") is { Node: not null } panel)
            plan.SelectLayout(panel.Path);
        return plan;
    }

    private static bool TryMigrateLegacyBindings(JsonObject imported, StationeryStyleSettings settings, out JsonObject migrated)
    {
        migrated = new JsonObject();
        var modelRoot = settings.ModelTree.CreateTree();
        var layouts = settings.Layouts.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var placementsByOwner = settings.Bindings.GroupBy(binding => binding.ModelPath)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var synthesizeRoot = !placementsByOwner.ContainsKey(modelRoot.Path);
        if (synthesizeRoot && modelRoot.Children.Count > 1) return false;
        string? syntheticRootId = null;
        if (synthesizeRoot)
        {
            var rootLayouts = settings.Layouts.Where(layout => layout.ParentPath is null).Select(layout => layout.Id).ToHashSet(StringComparer.Ordinal);
            syntheticRootId = "controlTreeRoot";
            for (var suffix = 2; rootLayouts.Contains(syntheticRootId); suffix++) syntheticRootId = "controlTreeRoot" + suffix;
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
            => $"{row + 1}y.{column + 1}x.{columnSpan}w.{rowSpan}h";

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
            candidate["controlTree"] = migrated.DeepClone();
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
        if (NormalizeLayoutType((string?)layout["type"]) == "box-layout")
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
        var modelChildren = new JsonArray();
        var controlTree = new JsonObject();
        var cells = new JsonArray();
        for (var row = 0; row < Rows.Count; row++)
            for (var column = 0; column < Columns.Count; column++)
            {
                var cell = At(row, column);
                if (!Kinds.Contains(cell.Kind)) throw new JsonException("未対応の文房具の種類です。");
                var id = $"cellR{row + 1}C{column + 1}";
                modelChildren.Add(new JsonObject { ["id"] = id, ["type"] = cell.Kind, ["label"] = cell.Label });
                cells.Add(new JsonObject { ["row"] = row, ["col"] = column });
                var handle = StationeryControlHandle.FromModelId(id);
                controlTree[handle] = new JsonObject
                {
                    ["modelPath"] = new JsonObject { ["in /ctrlViewPort/ctrlMainPage"] = $"/design/mainPage/{id}" },
                    ["layoutPath"] = new JsonObject { ["in /ctrlViewPort/ctrlMainPage"] = $"{rootLayoutId}[single].{DefaultLayoutId}[{row + 1}y_{column + 1}x_1w_1h]" }
                };
            }
        var root = new JsonObject
        {
            ["controlTree"] = controlTree,
            ["modelTree"] = new JsonObject
            {
                ["id"] = "design", ["type"] = "viewport", ["children"] = new JsonArray(new JsonObject
                { ["id"] = "mainPage", ["type"] = "page", ["children"] = modelChildren })
            },
            ["layouts"] = new JsonArray(
                new JsonObject { ["id"] = rootLayoutId, ["type"] = "box-layout",
                    ["padding"] = new JsonObject { ["top"] = "0px", ["right"] = "0px", ["bottom"] = "0px", ["left"] = "0px" } },
                new JsonObject
                {
                    ["id"] = DefaultLayoutId, ["type"] = "grid-layout", ["cells"] = cells,
                    ["row-definitions"] = new JsonArray(Rows.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>()),
                    ["column-definitions"] = new JsonArray(Columns.Select(t => JsonValue.Create(t.Length())).ToArray<JsonNode?>()),
                    ["margin"] = EdgeObject("margin"), ["padding"] = EdgeObject("padding")
                }),
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
        if (path.Count < 1 || path[0] is not ("modelTree" or "layouts")) return null;
        // Only modelTree/layout nodes and slots inside a cell can carry editable identities.
        if (path[0] == "modelTree")
        {
            if ((path.Count > 1 && path.Count % 2 == 0) ||
                path.Where((_, i) => i > 0 && i < path.Count - 1 && i % 2 == 1).Any(part => part != "children")) return null;
            return NodeAt(JsonNode.Parse(BuildJson())!, path) is JsonObject model ? (string?)model["id"] : null;
        }
        IReadOnlyList<string> ownerPath = path[0] == "layouts" && path.Count >= 6 && path[^2] == "slots" && path[^4] == "cells"
            ? path.Take(path.Count - 4).ToArray() : path;
        if (ownerPath.Count % 2 != 0 || ownerPath.Where((_, i) => i > 0 && i % 2 == 0).Any(p => p != "children")) return null;
        return NodeAt(JsonNode.Parse(BuildJson())!, path) is JsonObject obj ? (string?)obj["id"] : null;
    }

    public (string? Error, string? Warning) ValidateId(string id, IReadOnlyList<string>? path = null)
    {
        var root = JsonNode.Parse(BuildJson())!;
        JsonArray array;
        if (path is null) array = root["layouts"]!.AsArray();
        else if (path.Count == 1 && path[0] == "modelTree") array = [];
        else array = NodeAt(root, path.Take(path.Count - 1).ToArray()).AsArray();
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
        var selected = IsImported ? SelectedLayoutId : DefaultLayoutPath;
        if (path[0] == "layouts")
        {
            if (path.Count >= 2 && path[^2] == "slots")
            {
                node["id"] = id; Serialize(draft); imported = draft; return;
            }
            var oldPath = LayoutNodes(draft).Single(l => ReferenceEquals(l.Node, node)).Path;
            var newPath = oldPath[..^oldId.Length] + id;
            RewriteControlTreeRoutes(draft, settings, oldPath, null, oldId, id);
            if (selected is not null && (selected == oldPath || selected.StartsWith(oldPath + "/", StringComparison.Ordinal)))
                selected = newPath + selected[oldPath.Length..];
        }
        else
        {
            var ids = new List<string>();
            for (JsonNode? ancestor = node; ancestor is not null && ancestor != draft; ancestor = ancestor.Parent)
                if (ancestor is JsonObject obj && obj["id"] is JsonValue value) ids.Insert(0, value.GetValue<string>());
            var oldPath = "/" + string.Join("/", ids);
            RewriteControlTreeRoutes(draft, settings, null, oldPath, oldId, id);
        }
        node["id"] = id;
        Serialize(draft);
        imported = draft; SelectedLayoutId = null;
        if (selected is not null) SelectLayout(selected);
    }

    private static void RewriteControlTreeRoutes(JsonObject draft, StationeryStyleSettings settings,
        string? oldLayoutPath, string? oldModelPath, string oldId, string newId)
    {
        if (draft["controlTree"] is not JsonObject controlTree) return;

        void RewriteEntry(JsonNode node)
        {
            if (node is JsonObject obj && obj["modelPath"] is JsonObject models && obj["layoutPath"] is JsonObject routes)
            {
                foreach (var modelEntry in models.ToArray())
                    if (modelEntry.Value is JsonValue modelValue && modelValue.TryGetValue<string>(out var modelPath) &&
                        oldModelPath is not null && (modelPath == oldModelPath || modelPath.StartsWith(oldModelPath + "/", StringComparison.Ordinal)))
                        models[modelEntry.Key] = oldModelPath[..^oldId.Length] + newId + modelPath[oldModelPath.Length..];
                foreach (var routeEntry in routes.ToArray())
                    if (routeEntry.Value is JsonValue routeValue && routeValue.TryGetValue<string>(out var route))
                        routes[routeEntry.Key] = RewriteLayoutRoute(route);
                return;
            }
            if (node is JsonObject keyed)
                foreach (var objectEntry in keyed.ToArray()) if (objectEntry.Value is not null) RewriteEntry(objectEntry.Value);
            else if (node is JsonArray array)
                foreach (var arrayItem in array) if (arrayItem is not null) RewriteEntry(arrayItem);
        }

        foreach (var entry in controlTree.ToArray()) if (entry.Value is not null) RewriteEntry(entry.Value);

        string RewriteLayoutRoute(string route)
        {
            if (oldLayoutPath is null) return route;
            var oldLayoutId = oldLayoutPath.Split('/').Last();
            return route.Replace(oldLayoutId + "[", newId + "[", StringComparison.Ordinal)
                .Replace("." + oldLayoutId + ".", "." + newId + ".", StringComparison.Ordinal);
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
        if (!path.EndsWith(".stationery-ui.json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("出力先の末尾を .stationery-ui.json にしてください。");
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
