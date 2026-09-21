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
    public string? SelectedLayoutType => !IsImported ? "floating-layout" : (string?)imported!["layouts"]!.AsArray().FirstOrDefault(l => (string?)l!["id"] == SelectedLayoutId)?["type"];
    public bool CanEditPanel => SelectedLayoutType == "panel";
    public bool CanEditGrid => !IsImported || SelectedLayoutType == "floating-layout";
    public Dictionary<string, Track> PanelEdges { get; } = [];
    private readonly Dictionary<string, string> originalPanelNumbers = [];
    public IReadOnlyList<string> EditableLayouts => imported?["layouts"]!.AsArray()
        .Where(l => (string?)l!["type"] == "floating-layout" && l["row-definitions"]!.AsArray().Count <= 8 && l["column-definitions"]!.AsArray().Count <= 8)
        .Select(l => (string)l!["id"]!).ToArray() ?? ["mainGrid"];

    public static StyleBlueprint Open(string path) => Parse(File.ReadAllText(path));
    public static StyleBlueprint Parse(string json)
    {
        StationeryStyleSettings.Parse(json);
        var plan = new StyleBlueprint { imported = JsonNode.Parse(json)!.AsObject() };
        if (plan.EditableLayouts.Count > 0) plan.SelectLayout(plan.EditableLayouts[0]);
        else if (plan.imported["layouts"]!.AsArray().FirstOrDefault(l => (string?)l!["type"] == "panel") is { } panel)
            plan.SelectLayout((string)panel["id"]!);
        return plan;
    }
    public void SelectLayout(string id)
    {
        if (!IsImported) throw new ArgumentException("レイアウトを読み込んでください。");
        var committed = JsonNode.Parse(BuildJson())!.AsObject();
        var layout = committed["layouts"]!.AsArray().FirstOrDefault(l => (string?)l!["id"] == id)
            ?? throw new ArgumentException("レイアウトがありません。");
        if ((string?)layout["type"] == "panel")
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
            .Where(c => c.Row == row && c.Column == column).Select(c => c.ModelPath);
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
            if (settings.Bindings.Where(b => b.Layout == SelectedLayoutId).SelectMany(b => b.Children).Any(c => c.Row >= rows || c.Column >= columns))
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
                var panel = draft["layouts"]!.AsArray().Single(l => (string?)l!["id"] == SelectedLayoutId)!;
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
                var layout = draft["layouts"]!.AsArray().Single(l => (string?)l!["id"] == SelectedLayoutId)!;
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
                ["id"] = "mainGrid", ["type"] = "floating-layout",
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

    public string AddLayout(string type)
    {
        if (type is not ("panel" or "floating-layout")) throw new ArgumentException("追加できない種類です。");
        var draft = JsonNode.Parse(BuildJson())!.AsObject();
        var layouts = draft["layouts"]!.AsArray();
        var prefix = type == "panel" ? "panel" : "floatingLayout";
        var number = 1;
        while (layouts.Any(l => (string?)l!["id"] == prefix + number)) number++;
        var id = prefix + number;
        var added = new JsonObject { ["id"] = id, ["type"] = type };
        if (type == "floating-layout")
        {
            added["row-definitions"] = new JsonArray("1rate", "1rate");
            added["column-definitions"] = new JsonArray("1rate", "1rate");
        }
        else foreach (var group in new[] { "margin", "padding", "border" })
            added[group] = new JsonObject { ["top"] = "0px", ["right"] = "0px", ["bottom"] = "0px", ["left"] = "0px" };
        layouts.Add(added);
        Serialize(draft);
        imported = draft; SelectedLayoutId = null;
        SelectLayout(id);
        return id;
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
        if (keep is not null && draft["layouts"]!.AsArray().Any(l => (string?)l!["id"] == keep)) SelectLayout(keep);
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
