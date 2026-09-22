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
        oldJson["layouts"]![0]!["type"] = "floating-layout";
        oldJson["extension"] = "floating-layout";
        var legacyPlan = StyleBlueprint.Parse(oldJson.ToJsonString());
        Check(legacyPlan.CanEditGrid && legacyPlan.SelectedLayoutType == "grid-layout", "legacy grid remains editable");
        legacyPlan.Columns[0].Number = "2";
        var migrated = JsonNode.Parse(legacyPlan.BuildJson())!;
        Check((string?)migrated["layouts"]![0]!["type"] == "grid-layout" &&
            (string?)migrated["extension"] == "floating-layout", "export normalizes layout type without touching other text");
        Check(legacyPlan.CreatePreview(1000, 600).Cells.Count == 6, "legacy grid preview");
        Check(!json.Contains("\r\r") && !json.ReplaceLineEndings("\n").Contains("\n\n"), "no doubled or blank line endings");
        Check(json.Split('\n')[1].StartsWith("    \"models\""), "four-space indentation");
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
            Check(!original.Contains("\r\r") && !original.ReplaceLineEndings("\n").Contains("\n\n"), "exported file has single line endings");
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
        Check(imported.IsImported && imported.SelectedLayoutId == "/topDemoLayout", "opens existing layout");
        imported.Columns[0].Number = "2.5";
        var edited = JsonNode.Parse(imported.BuildJson())!;
        foreach (var key in new[] { "models", "bindings", "extraMetadata" })
            Check(JsonNode.DeepEquals(source[key], edited[key]), "preserves " + key);
        Check(JsonNode.DeepEquals(source["layouts"]![0], edited["layouts"]![0]), "preserves unrelated panel");
        Check(imported.CellDescription(0, 0) == "nameField", "existing model references in table");
        imported.SelectLayout("/splitDemoLayout");
        imported.Rows[0].Number = "72";
        imported.SelectLayout("/topDemoLayout");
        Check(imported.Columns[0].Number == "2.5", "selection retains earlier layout edits");
        Check(StationeryStyleSettings.Parse(imported.BuildJson()).Layouts.Single(l => l.Id == "splitDemoLayout").Rows[0].Value == 72, "other edit retained");
        try { imported.Resize(1, 1); throw new Exception("Invalid shrink accepted."); } catch (ArgumentException) { }
        Check(imported.Rows.Count == 5 && imported.Columns.Count == 2, "rejected shrink is atomic");
        imported.Columns[0].Number = "invalid";
        Reject(() => imported.SelectLayout("/splitDemoLayout"));
        Check(imported.SelectedLayoutId == "/topDemoLayout", "invalid draft not discarded on selection");
        var readOnly = StyleBlueprint.Parse("{\"models\":[{\"id\":\"root\",\"type\":\"viewport\"}],\"layouts\":[],\"bindings\":[],\"window\":{\"width\":1000}}");
        Check(!readOnly.CanEditGrid && JsonNode.Parse(readOnly.BuildJson())!["window"]!["width"]!.GetValue<int>() == 1000, "non-grid document stays intact");
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
        var imported = StyleBlueprint.Open(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json"));
        imported.SelectLayout("/splitDemoLayout");
        preview = imported.CreatePreview(700, 500);
        Check(preview.ScopePath.EndsWith("/splitPaneDemoPage", StringComparison.Ordinal), "preview chooses bound page");
        Check(preview.Settings.Layouts.Any(l => l.Type == "split-pane"), "preview retains split and other layout definitions");
    }

    private static void IdEditing()
    {
        foreach (var id in new[] { "", "with space", "日本語", "bad-id", "a/b" })
            Check(StyleBlueprint.CheckId(id, []).Error is not null, "invalid id characters");
        foreach (var id in new[] { "UpperCase", "snake_case", "123name", "fooBAR" })
        {
            var result = StyleBlueprint.CheckId(id, []);
            Check(result.Error is null && result.Warning is not null, "non-camel id is warning only");
        }
        Check(StyleBlueprint.CheckId("myPanel2", []).Warning is null, "camelCase accepted");
        var plan = new StyleBlueprint();
        plan.AddLayout("box-layout", "123_panel");
        var before = plan.BuildJson();
        try { plan.AddLayout("box-layout", "mainGrid"); throw new Exception("Duplicate accepted"); } catch (ArgumentException) { }
        Check(plan.BuildJson() == before, "duplicate add is atomic");
        plan.RenameId(["layouts", "0"], "newGrid");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].Layout == "/newGrid", "layout references updated");
        plan.RenameId(["models", "0", "children", "0"], "renamedPage");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].ModelPath == "/design/renamedPage", "model parent references updated");
        plan.RenameId(["models", "0", "children", "0", "children", "0"], "newCell");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].Children[0].ModelPath == "/design/renamedPage/newCell", "child references updated");
        Check(plan.ValidateId("cellR1C2", ["models", "0", "children", "0", "children", "0"]).Error is not null, "sibling duplicate blocked");
        Check(plan.ValidateId("newCell", ["models", "0", "children", "0", "children", "0"]).Error is null, "unchanged id accepted");
        before = plan.BuildJson();
        try { plan.RenameId(["models", "0", "children", "0", "children", "0"], "cellR1C2"); throw new Exception("Duplicate rename accepted"); } catch (ArgumentException) { }
        Check(plan.BuildJson() == before, "duplicate rename is atomic");
        Check(StyleBlueprint.CheckId("MyPanel", ["myPanel"]).Error is null, "case-sensitive sibling ids");
        var fixture = StyleBlueprint.Open(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json"));
        fixture.RenameId(["models", "0", "children", "0", "children", "0", "children", "0"], "renamedName");
        var root = JsonNode.Parse(fixture.BuildJson())!;
        var pageChildren = root["models"]![0]!["children"]![0]!["children"]![0]!["children"]!.AsArray();
        var dialog = pageChildren.Single(n => (string?)n!["id"] == "editDialog")!;
        Check((string?)dialog["children"]![0]!["id"] == "nameField", "same id under other parent not renamed");
        fixture.RenameId(["models", "0"], "newRoot");
        var settings = StationeryStyleSettings.Parse(fixture.BuildJson());
        Check(settings.Bindings.All(b => b.ModelPath.StartsWith("/newRoot/", StringComparison.Ordinal) || b.ModelPath == "/newRoot"), "all binding kinds survive root rename");
    }

    private static void LayoutEditing()
    {
        var plan = new StyleBlueprint();
        var panel = plan.AddLayout("box-layout");
        Check(plan.CanEditPanel && !plan.CanEditGrid, "panel editor selection");
        plan.PanelEdges["margin.left"].Number = "10";
        plan.PanelEdges["padding.top"].Number = "4";
        plan.PanelEdges["border.right"].Number = "7";
        var json = JsonNode.Parse(plan.BuildJson())!;
        json["bindings"]!.AsArray().Clear();
        json["bindings"]!.AsArray().Add(new JsonObject { ["layout"] = panel, ["model"] = "design/mainPage" });
        var oldBox = json.DeepClone();
        oldBox["layouts"]![1]!["type"] = "panel";
        oldBox["extension"] = "panel";
        var oldPlan = StyleBlueprint.Parse(oldBox.ToJsonString());
        oldPlan.SelectLayout(panel);
        Check(oldPlan.CanEditPanel && oldPlan.SelectedLayoutType == "box-layout", "old box can be selected and edited");
        oldPlan.PanelEdges["padding.top"].Number = "6";
        var savedBox = JsonNode.Parse(oldPlan.BuildJson())!;
        Check((string?)savedBox["layouts"]![1]!["type"] == "box-layout" &&
            (string?)savedBox["layouts"]![1]!["padding"]!["top"] == "6px" &&
            (string?)savedBox["extension"] == "panel", "old box export canonicalizes only layout type");
        Check(oldPlan.CreatePreview(100, 80).Layout.ContentBounds["/design/mainPage"].Y == 6, "old box preview uses padding");
        var compatibility = new StyleBlueprint();
        var oldAdded = compatibility.AddLayout("panel");
        Check(compatibility.SelectedLayoutType == "box-layout" && oldAdded.StartsWith("/boxLayout"), "legacy add uses canonical type and generated ID");
        var style = StationeryStyleSettings.Parse(json.ToJsonString());
        var arranged = StationeryLayoutEngine.Arrange(style, 100, 80);
        Check(arranged.Bounds["/design/mainPage"].Width == 90 && arranged.ContentBounds["/design/mainPage"].Y == 4, "margin and padding affect layout");
        Check(arranged.BorderBounds["/design/mainPage"].Width == 97 && arranged.ContentBounds["/design/mainPage"].Width == 90, "border does not consume size");
        var grid = plan.AddLayout("grid-layout");
        Check(plan.CanEditGrid && plan.SelectedLayoutId == grid, "new grid uses grid editor");
        plan.Columns[0].Number = "2.5";
        plan.SelectLayout(panel);
        Check(plan.PanelEdges["margin.left"].Number == "10", "panel values survive switching");
        plan.DeleteNode(["layouts", "2"]);
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Layouts.Count == 2, "delete unused layout");
        var before = plan.BuildJson();
        Reject(() => plan.DeleteNode(["layouts", "0"]));
        Check(plan.BuildJson() == before, "referenced layout deletion is atomic");
        plan.DeleteNode(["layouts", "1", "border"]);
        Check(JsonNode.Parse(plan.BuildJson())!["layouts"]![1]!["border"] is null, "deleted optional panel property stays deleted");
        try { plan.DeleteNode(["layouts"]); throw new Exception("Root deleted"); } catch (ArgumentException) { }
        plan.PanelEdges["border.top"].Number = "-1";
        Reject(() => plan.BuildJson());
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Invalid draft accepted.");
    }
}
