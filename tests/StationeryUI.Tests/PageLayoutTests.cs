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
        Check(bounds.ContentBounds[model.TopPage.Path + "/body"].Height == 564, "body reserves inspector and padding");
        Check(settings.Layouts.All(l => l.Type is not ("work-page-layout" or "fullscreen-layout")), "demo uses dock instead of legacy page layouts");
        foreach (var page in new[] { model.TopPage, model.SplitPage, model.LayoutPage })
        {
            var owner = settings.Bindings.Where(b => b.ModelPath == page.Path).ToArray();
            Check(owner.Length == 1 && owner[0].Layout == "pageDock", "each page owns only the shared dock layout");
            Check(owner[0].DockChildren[0].Dock == "bottom" && owner[0].DockChildren[1].Dock == "center", "inspector precedes center body");
        }
        Check(bounds.Bounds[model.SplitControls["toolHint"].Path].Height == 0, "fullscreen hides hint");
        // Exercise the actual showcase bindings, including nested boxes and spanning grid cells.
        foreach (var (width, height) in new[] { (1000, 780), (720, 560), (1400, 900) })
        {
            var showcase = StationeryLayoutEngine.Arrange(settings, width, height);
            var owner = model.LayoutPage.Path + "/body:layoutShowcase";
            var box = showcase.LayoutBounds[owner + ".box"];
            var content = showcase.Bounds[model.LayoutControls["boxContent"].Path];
            Check(content.X == box.X + 24 && content.Y == box.Y + 24 && content.Width == box.Width - 48, "box padding surrounds its only child");
            var grid = showcase.LayoutContentBounds[owner + ".grid"];
            var nested = showcase.LayoutBounds[owner + ".grid.nestedGrid"];
            Check(Math.Abs(nested.Width - grid.Width * 2 / 3) < .001 && Math.Abs(nested.Height - grid.Height * 2 / 3) < .001, "nested grid spans two rows and columns");
            var a = showcase.Bounds[model.LayoutControls["nestedA"].Path];
            var d = showcase.Bounds[model.LayoutControls["nestedD"].Path];
            Check(a.X + a.Width == d.X && a.Y + a.Height == d.Y, "nested cells occupy separate quadrants");
            var footer = showcase.Bounds[model.LayoutControls["gridFooter"].Path];
            Check(footer.Width == grid.Width && footer.Y == nested.Y + nested.Height, "footer spans all three columns");
            Check(showcase.Bounds[model.LayoutControls["toolHint"].Path].Height == 80, "layout page reserves inspector");
        }
        Check(DemoModelBinding.Create(DemoModelBinding.Fallback).Signature == model.Signature, "fallback includes showcase roles");
        var binding = json["bindings"]!.AsArray().Single(b => (string?)b!["parentModel"] == "demo/topDemoPage")!;
        var inspector = binding["childrenModel"]![0]!;
        var signature = model.Signature;
        inspector["size"] = "0px";
        var full = StationeryStyleSettings.Parse(json.ToJsonString());
        Check(DemoModelBinding.Create(full).Signature == signature, "binding switch preserves identities");
        Check(StationeryLayoutEngine.Arrange(full, 1000, 660).ContentBounds[model.TopPage.Path + "/body"].Height == 644, "zero-height dock gives body available height");
        inspector["size"] = "80px";
        foreach (var size in new[] { 0, 1, 40, 80, 81, 660 })
        {
            var result = StationeryLayoutEngine.Arrange(settings, 500, size);
            var small = result.Bounds["/demo/topDemoPage/inspectorPanel"];
            Check(small.Height == Math.Min(80, size) && small.Y + small.Height == size, "small window clamps inspector");
            Check(result.Bounds.Values.All(r => r.Width >= 0 && r.Height >= 0), "nonnegative bounds");
        }
        var valid = json.ToJsonString();
        var duplicate = binding.DeepClone();
        inspector["model"] = "missingPanel";
        Reject(json.ToJsonString());
        json = JsonNode.Parse(valid)!;
        json["bindings"]!.AsArray().Add(duplicate);
        Reject(json.ToJsonString());
        Reject(valid.Replace("80px", "-80px"));
        // A real file reload changes dock size without changing models or editing state.
        var directory = Path.Combine(Path.GetTempPath(), "page-layout-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "config.json"), "{\"styleFile\":\"style.json\",\"autoReload\":true}");
            var path = Path.Combine(directory, "style.json");
            File.WriteAllText(path, valid);
            var file = new StationeryStyleFile(Path.Combine(directory, "config.json"), settings, s => DemoModelBinding.Create(s));
            File.WriteAllText(path, valid.Replace("\"size\":\"80px\"", "\"size\":\"0px\""));
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Check(file.LastError is null && StationeryLayoutEngine.Arrange(file.Current, 1000, 660).Bounds[model.Main["toolHint"].Path].Height == 0, "reload switches layout");
        }
        finally { Directory.Delete(directory, true); }
        CheckLegacyPageLayouts();
    }
    private static void CheckLegacyPageLayouts()
    {
        const string source = """
        {"models":[{"id":"app","type":"viewport","children":[{"id":"page","type":"page","children":[{"id":"inspector","type":"container"}]}]}],
         "layouts":[{"id":"pageLayout","type":"work-page-layout","inspectorHeight":"80px"}],
         "bindings":[{"layout":"pageLayout","model":"app/page","inspectorModel":"inspector"}]}
        """;
        var settings = StationeryStyleSettings.Parse(source);
        Check(StationeryLayoutEngine.Arrange(settings, 1000, 660).Bounds["/app/page/inspector"].Height == 80, "legacy work page remains supported");
        settings = StationeryStyleSettings.Parse(source.Replace("work-page-layout", "fullscreen-layout").Replace(",\"inspectorHeight\":\"80px\"", ""));
        Check(StationeryLayoutEngine.Arrange(settings, 1000, 660).Bounds["/app/page/inspector"].Height == 0, "legacy fullscreen remains supported");
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(string json)
    {
        try { StationeryStyleSettings.Parse(json); } catch (JsonException) { return; }
        throw new Exception("Invalid page binding accepted.");
    }
}
