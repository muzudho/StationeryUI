using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class StyleBlueprintTests
{
    public static void Run()
    {
        ImportedStyles();
        LayoutEditing();
        IdEditing();
        LivePreview();
        var plan = new StyleBlueprint();
        plan.Resize(3, 2);
        plan.Columns[0].Number = "1.5";
        plan.Columns[1].IsRate = false; plan.Columns[1].Number = "100";
        plan.At(0, 0).Kind = "button"; plan.At(0, 0).Label = "開始\n日本語 \"ラベル\"";
        var json = plan.BuildJson();
        var oldJson = JsonNode.Parse(json)!;
        oldJson["layouts"]![1]!["type"] = "floating-layout";
        oldJson["extension"] = "floating-layout";
        var legacyPlan = StyleBlueprint.Parse(oldJson.ToJsonString());
        Check(legacyPlan.CanEditGrid && legacyPlan.SelectedLayoutType == "grid-layout", "legacy grid remains editable");
        legacyPlan.Columns[0].Number = "2";
        var migrated = JsonNode.Parse(legacyPlan.BuildJson())!;
        Check((string?)migrated["layouts"]![1]!["type"] == "grid-layout" &&
            (string?)migrated["extension"] == "floating-layout", "export normalizes layout type without touching other text");
        Check(legacyPlan.CreatePreview(1000, 600).Cells.Count == 6, "legacy grid preview");
        Check(!json.Contains("\r\r") && !json.ReplaceLineEndings("\n").Contains("\n\n"), "no doubled or blank line endings");
        Check(json.Split('\n')[1].StartsWith("    \"controlTree\""), "four-space indentation");
        var style = StationeryStyleSettings.Parse(json);
        Check(style.Layouts[1].Columns[0].Value == 1.5 && !style.Layouts[1].Columns[1].IsRate, "fractional number and unit");
        using var doc = JsonDocument.Parse(json);
        Check(doc.RootElement.GetProperty("modelTree").GetProperty("children")[0].GetProperty("children")[0].GetProperty("label").GetString() == plan.At(0, 0).Label, "labels preserve Japanese and escaping");
        Check(style.ModelTree.CreateTree().Resolve("/design/mainPage/cellR1C1")!.Kind == "button", "generated stable identity");
        var bounds = StationeryLayoutEngine.Arrange(style, 1000, 600).Bounds;
        Check(bounds["/design/mainPage/cellR1C2"].Width == 100, "pixel width");
        plan.Resize(4, 3);
        Check(plan.At(0, 0).Kind == "button" && plan.Columns[0].Number == "1.5", "growing retains draft");
        plan.Resize(1, 1);
        Check(plan.At(0, 0).Kind == "button" && StationeryStyleSettings.Parse(plan.BuildJson()).Bindings.Single(b => b.ModelPath == "/design/mainPage").Children.Count == 1, "shrinking exports valid bindings");
        foreach (var number in new[] { "", "-1", "NaN", "1rate", "1,5", "1e500" })
        {
            plan.Columns[0].Number = number;
            Reject(() => plan.BuildJson());
        }
        plan.Columns.ForEach(t => t.Number = "0"); Reject(() => plan.BuildJson());
        plan.Columns[0].Number = "1";
        var directory = Path.Combine(Path.GetTempPath(), "style-blueprint-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "plan.stationery-ui.json");
            plan.Export(path);
            var original = File.ReadAllText(path);
            Check(!original.Contains("\r\r") && !original.ReplaceLineEndings("\n").Contains("\n\n"), "exported file has single line endings");
            plan.At(0, 0).Label = "変更後";
            try { plan.Export(path); throw new Exception("Existing file overwritten."); } catch (IOException) { }
            Check(File.ReadAllText(path) == original && Directory.GetFiles(directory).Length == 1, "existing file untouched and temporary cleaned");
            plan.Rows[0].Number = "invalid";
            Reject(() => plan.Export(Path.Combine(directory, "invalid.stationery-ui.json")));
            Check(Directory.GetFiles(directory).Length == 1, "invalid draft creates no output");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void ImportedStyles()
    {
        var source = StyleTestData.Demo();
        source["extraMetadata"] = new JsonObject { ["memo"] = "既存の拡張情報" };
        var plan = StyleBlueprint.Parse(source.ToJsonString());
        Check(plan.SelectedLayoutId == "/csTopDemoGridLayout", "opens existing grid");
        plan.Columns[0].Number = "2.5";
        var edited = JsonNode.Parse(plan.BuildJson())!;
        var expected = source.DeepClone();
        StyleTestData.Layout(expected, "/csTopDemoGridLayout")["column-definitions"]![0] = "2.5rate";
        Check(JsonNode.DeepEquals(expected, edited), "preserves every unrelated property and reference");
        Check(plan.CellDescription(0, 0).Contains("mdlNameField"), "existing model references");
        plan.SelectLayout("/csSplitDemoGridLayout"); plan.Rows[0].Number = "72";
        plan.SelectLayout("/csTopDemoGridLayout");
        Check(plan.Columns[0].Number == "2.5", "selection retains edits");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Layouts.Single(l => l.Id == "csSplitDemoGridLayout").Rows[0].Value == 72, "other layout edit retained");
        try { plan.Resize(1, 1); throw new Exception("Invalid shrink accepted."); } catch (ArgumentException) { }
        Check(plan.Rows.Count == 5 && plan.Columns.Count == 2, "rejected shrink is atomic");
        plan.Columns[0].Number = "invalid";
        Reject(() => plan.SelectLayout("/csSplitDemoGridLayout"));
        Check(plan.SelectedLayoutId == "/csTopDemoGridLayout", "invalid draft not discarded");
        var readOnly = StyleBlueprint.Parse("""{"modelTree":{"id":"root","type":"viewport"},"layouts":[],"controlTree":{},"window":{"width":1000}}""");
        Check(!readOnly.CanEditGrid && JsonNode.Parse(readOnly.BuildJson())!["window"]!["width"]!.GetValue<int>() == 1000, "read-only document retained");
        Reject(() => StyleBlueprint.Parse("{"));
    }

    private static void LivePreview()
    {
        var plan = new StyleBlueprint();
        plan.Columns[0].Number = "120"; plan.Columns[0].IsRate = false;
        var before = plan.BuildJson();
        var small = plan.CreatePreview(400, 300);
        var large = plan.CreatePreview(800, 500);
        Check(small.Cells[0].Bounds.Width == 120 && large.Cells[0].Bounds.Width == 120, "preview px independent of viewport");
        Check(small.Cells[1].Bounds.Width == 280 && large.Cells[1].Bounds.Width == 680, "preview rate follows viewport");
        Check(large.ScopePath == "/design/mainPage" && !large.Standalone, "preview uses bound model tree");
        Check(plan.BuildJson() == before, "preview does not mutate plan");
        var panel = plan.AddLayout("box-layout");
        plan.PanelEdges["margin.left"].Number = "10";
        plan.PanelEdges["padding.left"].Number = "20";
        var preview = plan.CreatePreview(500, 300);
        Check(preview.Standalone && preview.Layout.ContentBounds["/previewRoot"].X == 30, "unbound panel preview uses box model");
        var grid = plan.AddLayout("grid-layout");
        plan.Columns[0].Number = "0";
        preview = plan.CreatePreview(500, 300);
        Check(preview.Standalone && preview.Cells[0].Bounds.Width == 0 && preview.Cells[1].Bounds.Width == 500, "empty zero-width tracks remain configurable");
        var imported = StyleBlueprint.Open(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json"));
        imported.SelectLayout("/csSplitDemoGridLayout");
        preview = imported.CreatePreview(700, 500);
        Check(preview.ScopePath.EndsWith("/mdlSplitPaneDemoPage", StringComparison.Ordinal), "preview chooses bound page");
        Check(preview.Settings.Layouts.Any(l => l.Type == "split-pane"), "preview retains split and other layout definitions");
    }

    private static void IdEditing()
    {
        foreach (var id in new[] { "", "with space", "日本語", "bad-id", "a/b" }) Check(StyleBlueprint.CheckId(id, []).Error is not null, "invalid ID");
        foreach (var id in new[] { "UpperCase", "snake_case", "123name", "fooBAR" }) {
            var result = StyleBlueprint.CheckId(id, []); Check(result.Error is null && result.Warning is not null, "ID warning");
        }
        Check(StyleBlueprint.CheckId("myPanel2", []).Warning is null, "camelCase accepted");
        Check(StyleBlueprint.CheckId("MyPanel", ["myPanel"]).Error is null, "case-sensitive IDs");
        var plan = new StyleBlueprint(); plan.AddLayout("box-layout", "123_panel");
        var before = plan.BuildJson();
        try { plan.AddLayout("box-layout", plan.DefaultLayoutId); throw new Exception("Duplicate accepted."); } catch (ArgumentException) { }
        Check(plan.BuildJson() == before, "duplicate add is atomic");
        plan.RenameId(["layouts", "1"], "newGrid");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings.Any(b => b.Layout == "/newGrid"), "layout rename rewrites routes");
        plan.RenameId(["modelTree", "children", "0"], "renamedPage");
        plan.RenameId(["modelTree", "children", "0", "children", "0"], "newCell");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).ControlTree.Values.Any(b => b.ModelPaths.Values.Contains("/design/renamedPage/newCell")), "model rename rewrites references");
        Check(plan.ValidateId("cellR1C2", ["modelTree", "children", "0", "children", "0"]).Error is not null, "sibling duplicate blocked");
        before = plan.BuildJson();
        try { plan.RenameId(["modelTree", "children", "0", "children", "0"], "cellR1C2"); throw new Exception("Duplicate rename accepted."); } catch (ArgumentException) { }
        Check(plan.BuildJson() == before, "duplicate rename atomic");
        plan.RenameId(["modelTree"], "newRoot");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings.All(b => b.ModelPath.StartsWith("/newRoot")), "root rename updates all references");
    }

    private static void LayoutEditing()
    {
        var plan = new StyleBlueprint();
        var panel = plan.AddLayout("box-layout");
        Check(plan.CanEditPanel && !plan.CanEditGrid, "panel selected");
        plan.PanelEdges["margin.left"].Number = "10"; plan.PanelEdges["padding.top"].Number = "4"; plan.PanelEdges["border.right"].Number = "7";
        var preview = plan.CreatePreview(100, 80);
        Check(preview.Standalone && preview.Layout.Bounds["/previewRoot"].Width == 90 && preview.Layout.ContentBounds["/previewRoot"].Y == 4, "box insets");
        Check(preview.Layout.BorderBounds["/previewRoot"].Width == 97 && preview.Layout.ContentBounds["/previewRoot"].Width == 90, "border does not consume area");
        var oldBox = JsonNode.Parse(plan.BuildJson())!;
        StyleBlueprint.FindLayout(oldBox, panel)!["type"] = "panel"; oldBox["extension"] = "panel";
        var old = StyleBlueprint.Parse(oldBox.ToJsonString()); old.SelectLayout(panel); old.PanelEdges["padding.top"].Number = "6";
        var saved = JsonNode.Parse(old.BuildJson())!;
        Check((string?)StyleBlueprint.FindLayout(saved, panel)!["type"] == "box-layout" && (string?)saved["extension"] == "panel", "alias normalization preserves metadata");
        Check(old.CreatePreview(100, 80).Layout.ContentBounds["/previewRoot"].Y == 6, "old box editable");
        var grid = plan.AddLayout("grid-layout"); plan.Columns[0].Number = "2.5"; plan.SelectLayout(panel);
        Check(plan.PanelEdges["margin.left"].Number == "10", "switch preserves box settings");
        plan.DeleteNode(["layouts", "3"]);
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Layouts.Count == 3, "delete unbound grid");
        var before = plan.BuildJson(); Reject(() => plan.DeleteNode(["layouts", "0"])); Check(before == plan.BuildJson(), "bound delete atomic");
        plan.DeleteNode(["layouts", "2", "border"]);
        Check(StyleBlueprint.FindLayout(JsonNode.Parse(plan.BuildJson())!, panel)!["border"] is null, "optional property stays deleted");
        try { plan.DeleteNode(["layouts"]); throw new Exception("Top-level deleted."); } catch (ArgumentException) { }
        plan.PanelEdges["border.top"].Number = "-1"; Reject(() => plan.BuildJson());
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Invalid draft accepted.");
    }
}
