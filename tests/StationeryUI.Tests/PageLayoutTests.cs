using System.Text.Json;
using System.Text.Json.Nodes;
using StationeryUI.Styling;

internal static class PageLayoutTests
{
    public static void Run()
    {
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json")))!;
        var settings = StationeryStyleSettings.Parse(json.ToJsonString());
        var model = DemoModelBinding.Create(settings);
        var bounds = StationeryLayoutEngine.Arrange(settings, 1000, 660);
        var panel = bounds.Bounds["/demo/topDemoPage/inspectorPanel"];
        Check(panel.X == 0 && panel.Y == 580 && panel.Width == 1000 && panel.Height == 80, "full-width bottom panel");
        Check(bounds.Bounds[model.Main["toolHint"].Path] == panel, "hint fills panel");
        Check(bounds.ContentBounds[model.TopPage.Path].Height == 564, "body reserves inspector and padding");
        Check(bounds.Bounds[model.SplitControls["toolHint"].Path].Height == 0, "fullscreen hides hint");
        var binding = json["bindings"]!.AsArray().Single(b => (string?)b!["model"] == "demo/topDemoPage" && b["inspectorModel"] is not null)!;
        var signature = model.Signature;
        binding["layout"] = "fullscreenLayout";
        var full = StationeryStyleSettings.Parse(json.ToJsonString());
        Check(DemoModelBinding.Create(full).Signature == signature, "binding switch preserves identities");
        Check(StationeryLayoutEngine.Arrange(full, 1000, 660).ContentBounds[model.TopPage.Path].Height == 644, "fullscreen uses available height");
        binding["layout"] = "workPageLayout";
        foreach (var size in new[] { 0, 1, 40, 80, 81, 660 })
        {
            var result = StationeryLayoutEngine.Arrange(settings, 500, size);
            var small = result.Bounds["/demo/topDemoPage/inspectorPanel"];
            Check(small.Height == Math.Min(80, size) && small.Y + small.Height == size, "small window clamps inspector");
            Check(result.Bounds.Values.All(r => r.Width >= 0 && r.Height >= 0), "nonnegative bounds");
        }
        var valid = json.ToJsonString();
        var duplicate = binding.DeepClone();
        binding["inspectorModel"] = "nameField";
        Reject(json.ToJsonString());
        json = JsonNode.Parse(valid)!;
        json["bindings"]!.AsArray().Add(duplicate);
        Reject(json.ToJsonString());
        Reject(valid.Replace("80px", "-80px"));
        // A real file reload switches page layout without changing models or losing the last valid snapshot.
        var directory = Path.Combine(Path.GetTempPath(), "page-layout-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "config.json"), "{\"styleFile\":\"style.json\",\"autoReload\":true}");
            var path = Path.Combine(directory, "style.json");
            File.WriteAllText(path, valid);
            var file = new StationeryStyleFile(Path.Combine(directory, "config.json"), settings, s => DemoModelBinding.Create(s));
            File.WriteAllText(path, valid.Replace("\"layout\":\"workPageLayout\"", "\"layout\":\"fullscreenLayout\""));
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Check(file.LastError is null && StationeryLayoutEngine.Arrange(file.Current, 1000, 660).Bounds[model.Main["toolHint"].Path].Height == 0, "reload switches layout");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(string json)
    {
        try { StationeryStyleSettings.Parse(json); } catch (JsonException) { return; }
        throw new Exception("Invalid page binding accepted.");
    }
}
