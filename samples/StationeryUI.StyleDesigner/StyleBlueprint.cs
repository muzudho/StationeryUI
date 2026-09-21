namespace StationeryUI.StyleDesigner;

using StationeryUI.Styling;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;

/// <summary>A new in-memory design, exported as a style file. Never opens or edits an existing file.</summary>
public sealed class StyleBlueprint
{
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
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        StationeryStyleSettings.Parse(json);
        // Use four spaces, matching the style file convention, without altering label strings.
        return string.Join(Environment.NewLine, json.Split('\n').Select(line =>
        {
            var spaces = line.TakeWhile(c => c == ' ').Count();
            return new string(' ', spaces) + line;
        })) + Environment.NewLine;
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
