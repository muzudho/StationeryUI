using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using System.Text.Json;

internal static class StyleBlueprintTests
{
    public static void Run()
    {
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
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Invalid draft accepted.");
    }
}
