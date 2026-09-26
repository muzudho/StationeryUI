using System.Text.Json;
using System.Text.Json.Nodes;
using StationeryUI.Styling;

internal static class PageLayoutTests
{
    public static void Run()
    {
        var json = StyleTestData.Demo();
        var settings = StationeryStyleSettings.Parse(json.ToJsonString());
        var model = DemoModelBinding.Create(settings);
        Check(DemoModelBinding.Create(DemoModelBinding.Fallback).Signature == model.Signature, "embedded fallback includes all demo roles");
        foreach (var page in new[] { model.TopPage, model.SplitPage, model.LayoutPage })
        {
            var dock = settings.Bindings.Single(b => b.ModelPath == page.Path);
            Check(dock.DockChildren.Single(c => c.CellIndex == 0).Dock == "bottom" && dock.DockChildren.Single(c => c.CellIndex == 1).Dock == "center", "inspector precedes body");
            foreach (var (width, height) in new[] { (1000, 660), (720, 560), (20, 20), (0, 0) })
            {
                var bounds = StyleTestData.Arrange(settings, width, height, page.Path);
                var panel = bounds.Bounds[page.Path + "/mdlInspectorPanel"];
                var reserved = page == model.SplitPage ? 0 : Math.Min(80, height);
                Check(panel.X == 0 && panel.Width == width && panel.Height == reserved && panel.Y + panel.Height == height, "full-width inspector clamped to viewport");
                Check(bounds.Bounds.Values.All(b => b.Width >= 0 && b.Height >= 0), "small windows remain nonnegative");
                foreach (var other in new[] { model.TopPage, model.SplitPage, model.LayoutPage }.Where(p => p != page))
                    Check(bounds.Bounds[other.Path].Width == 0 && bounds.Bounds[other.Path].Height == 0, "inactive tab has no area");
            }
        }
        foreach (var (width, height) in new[] { (1000, 780), (720, 560), (1400, 900) })
        {
            var showcase = StyleTestData.Arrange(settings, width, height, model.LayoutPage.Path);
            var owner = model.LayoutPage.Path + "/mdlBody:/csLayoutShowcaseGridLayout";
            var box = showcase.LayoutBounds[model.LayoutControls["boxContent"].Path + ":/csBoxLayout"];
            var content = showcase.Bounds[model.LayoutControls["boxContent"].Path];
            Check(content.X == box.X + 24 && content.Y == box.Y + 24 && content.Width == box.Width - 48, "box padding surrounds text");
            var grid = showcase.LayoutContentBounds[owner + "/grid"];
            var nested = showcase.LayoutBounds[owner + "/grid/nestedGrid"];
            Check(Math.Abs(nested.Width - grid.Width * 2 / 3) < .001 && Math.Abs(nested.Height - grid.Height * 2 / 3) < .001, "nested grid spans two rows and columns");
            var a = showcase.Bounds[model.LayoutControls["nestedA"].Path];
            var d = showcase.Bounds[model.LayoutControls["nestedD"].Path];
            Check(Math.Abs(a.X + a.Width - d.X) < .001 && Math.Abs(a.Y + a.Height - d.Y) < .001, "quadrants are separate");
            var footer = showcase.Bounds[model.LayoutControls["gridFooter"].Path];
            Check(Math.Abs(footer.Width - grid.Width) < .001 && Math.Abs(footer.Y - nested.Y - nested.Height) < .001, "footer spans three columns");
        }
        var original = json.ToJsonString();
        foreach (var dockLayout in StyleTestData.Objects(json).Where(n => (string?)n["name"] == "csPageDock")) dockLayout["cells"]![0]!["size"] = "0px";
        var full = StationeryStyleSettings.Parse(json.ToJsonString());
        Check(DemoModelBinding.Create(full).Signature == model.Signature, "dock size change preserves model identity");
        Check(StyleTestData.Arrange(full, 1000, 660).Bounds[model.Main["toolHint"].Path].Height == 0, "zero-height inspector");
        var directory = Path.Combine(Path.GetTempPath(), "page-layout-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var config = Path.Combine(directory, "config.json");
            var path = Path.Combine(directory, "style.json");
            File.WriteAllText(config, """{"styleFile":"style.json","autoReload":true}""");
            File.WriteAllText(path, original);
            var file = new StationeryStyleFile(config, settings, value => DemoModelBinding.Create(value));
            File.WriteAllText(path, json.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Check(file.LastError is null && StyleTestData.Arrange(file.Current, 1000, 660).Bounds[model.Main["toolHint"].Path].Height == 0, "reload switches inspector size");
            var good = file.Current;
            StyleTestData.Layout(json, "/csPageDock")["cells"]![0]!["size"] = "-1px";
            File.WriteAllText(path, json.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Check(ReferenceEquals(good, file.Current) && file.LastError is not null, "invalid dock retains last good page");
        }
        finally { Directory.Delete(directory, true); }
        var invalid = StyleTestData.Demo();
        invalid["modelTree"]!["margin"] = new JsonObject { ["left"] = "-1px" };
        StyleTestData.Reject(() => StationeryStyleSettings.Parse(invalid.ToJsonString()));
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
