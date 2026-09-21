using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class StyleBlueprintTests
{
    public static void Run()
    {
        ImportedStyles();
        var plan = new StyleBlueprint();
        plan.Resize(3, 2);
        plan.Columns[0].Number = "1.5";
        plan.Columns[1].IsRate = false; plan.Columns[1].Number = "100";
        plan.At(0, 0).Kind = "button"; plan.At(0, 0).Label = "開始\n日本語 \"ラベル\"";
        var json = plan.BuildJson();
        var style = StationeryStyleSettings.Parse(json);
        Check(style.Layouts[0].Columns[0].Value == 1.5 && !style.Layouts[0].Columns[1].IsRate, "fractional number and unit");
        using var doc = JsonDocument.Parse(json);
        Check(doc.RootElement.GetProperty("models")[0].GetProperty("children")[0].GetProperty("children")[0].GetProperty("label").GetString() == plan.At(0, 0).Label, "labels preserve Japanese and escaping");
        Check(style.Models[0].CreateTree().Resolve("/design/mainPage/cellR1C1")!.Kind == "button", "generated stable identity");
        var bounds = StationeryLayoutEngine.Arrange(style, 1000, 600).Bounds;
        Check(bounds["/design/mainPage/cellR1C2"].Width == 100, "pixel width");
        plan.Resize(4, 3);
        Check(plan.At(0, 0).Kind == "button" && plan.Columns[0].Number == "1.5", "growing retains draft");
        plan.Resize(1, 1);
        Check(plan.At(0, 0).Kind == "button" && StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].Children.Count == 1, "shrinking exports valid bindings");
        foreach (var number in new[] { "", "-1", "NaN", "1rate", "1,5", "1e500" })
        {
            plan.Columns[0].Number = number;
            Reject(() => plan.BuildJson());
        }
        plan.Columns[0].Number = "0"; Reject(() => plan.BuildJson());
        plan.Columns[0].Number = "1";
        var directory = Path.Combine(Path.GetTempPath(), "style-blueprint-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "plan.stationery-style.json");
            plan.Export(path);
            var original = File.ReadAllText(path);
            plan.At(0, 0).Label = "変更後";
            try { plan.Export(path); throw new Exception("Existing file overwritten."); } catch (IOException) { }
            Check(File.ReadAllText(path) == original && Directory.GetFiles(directory).Length == 1, "existing file untouched and temporary cleaned");
            plan.Rows[0].Number = "invalid";
            Reject(() => plan.Export(Path.Combine(directory, "invalid.stationery-style.json")));
            Check(Directory.GetFiles(directory).Length == 1, "invalid draft creates no output");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void ImportedStyles()
    {
        var source = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json")))!;
        source["extraMetadata"] = new JsonObject { ["memo"] = "既存の拡張情報" };
        var imported = StyleBlueprint.Parse(source.ToJsonString());
        Check(imported.IsImported && imported.SelectedLayoutId == "topDemoLayout", "opens existing layout");
        imported.Columns[0].Number = "2.5";
        var edited = JsonNode.Parse(imported.BuildJson())!;
        foreach (var key in new[] { "models", "bindings", "extraMetadata" })
            Check(JsonNode.DeepEquals(source[key], edited[key]), "preserves " + key);
        Check(JsonNode.DeepEquals(source["layouts"]![0], edited["layouts"]![0]), "preserves unrelated panel");
        Check(imported.CellDescription(0, 0) == "nameField", "existing model references in table");
        imported.SelectLayout("splitDemoLayout");
        imported.Rows[0].Number = "72";
        imported.SelectLayout("topDemoLayout");
        Check(imported.Columns[0].Number == "2.5", "selection retains earlier layout edits");
        Check(StationeryStyleSettings.Parse(imported.BuildJson()).Layouts.Single(l => l.Id == "splitDemoLayout").Rows[0].Value == 72, "other edit retained");
        try { imported.Resize(1, 1); throw new Exception("Invalid shrink accepted."); } catch (ArgumentException) { }
        Check(imported.Rows.Count == 5 && imported.Columns.Count == 2, "rejected shrink is atomic");
        imported.Columns[0].Number = "invalid";
        Reject(() => imported.SelectLayout("splitDemoLayout"));
        Check(imported.SelectedLayoutId == "topDemoLayout", "invalid draft not discarded on selection");
        var readOnly = StyleBlueprint.Parse("{\"models\":[{\"id\":\"root\",\"type\":\"viewport\"}],\"layouts\":[],\"bindings\":[],\"window\":{\"width\":1000}}");
        Check(!readOnly.CanEditGrid && JsonNode.Parse(readOnly.BuildJson())!["window"]!["width"]!.GetValue<int>() == 1000, "non-grid document stays intact");
        Reject(() => StyleBlueprint.Parse("{"));
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Invalid draft accepted.");
    }
}
