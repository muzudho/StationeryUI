using StationeryUI.Controls;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class SplitPaneTests
{
    public static void Run()
    {
        var split = new SplitPane();
        split.Configure(new(false, .25, 10, 30));
        var a = split.Arrange(new(10, 20, 210, 100));
        Check(a.First.Width == 50 && a.Divider.X == 60 && a.Second.X == 70 && a.Second.Width == 150, "vertical positions");
        split.Drag(new(10, 20, 210, 100), 10000, 5);
        Check(split.Arrange(new(10, 20, 210, 100)).Second.Width == 30, "minimum end clamp");
        var ratio = split.Ratio;
        split.Configure(new(false, .25, 10, 30)); Check(split.Ratio == ratio, "unchanged style preserves drag");
        split.Configure(new(true, .5, 8, 20));
        a = split.Arrange(new(0, 0, 200, 108));
        Check(a.First.Height == 50 && a.Divider.Y == 50 && a.Second.Y == 58, "horizontal positions");
        a = split.Arrange(new(0, 0, 10, 5));
        Check(a.First.Height == 0 && a.Second.Height == 0 && a.Divider.Height == 5, "tiny viewport");
        split.Drag(new(0, 0, 100, 108), -100, 4);
        Check(split.Arrange(new(0, 0, 100, 108)).First.Height == 20, "minimum start clamp");
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json"));
        var settings = StationeryStyleSettings.Parse(source);
        var result = StyleTestData.Arrange(settings, 1000, 780, "/mdlDemo/mdlSplitPaneDemoPage");
        Check(result.Bounds["/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlVerticalSplit/mdlLeftPane"].Width > 0, "bound split content");
        foreach (var edit in new Action<JsonNode>[] {
            n => StyleTestData.Layout(n, "/csVerticalSplitPaneLayout")["ratio"] = 2,
            n => StyleTestData.Layout(n, "/csVerticalSplitPaneLayout")["orientation"] = "diagonal",
            n => StyleTestData.Layout(n, "/csVerticalSplitPaneLayout")["dividerWidth"] = "0px",
            n => StyleTestData.Layout(n, "/csVerticalSplitPaneLayout")["minimumPaneSize"] = "-1px",
            n => StyleTestData.Objects(n).Single(o => (string?)o["name"] == "vLeftPane")["place"] = "second",
            n => StyleTestData.Objects(n).Single(o => (string?)o["name"] == "vLeftPane")["place"] = "missing",
        })
        {
            var json = JsonNode.Parse(source)!; edit(json);
            try { StationeryStyleSettings.Parse(json.ToJsonString()); } catch (JsonException) { continue; }
            throw new Exception("Invalid split settings accepted.");
        }
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
