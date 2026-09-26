using StationeryUI.Styling;
using StationeryUI.Editor;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class StyleTestData
{
    public static JsonNode Demo() => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json")))!;
    public static JsonNode Layout(JsonNode root, string path) => StyleBlueprint.FindLayout(root, path)!;
    public static IEnumerable<JsonObject> Objects(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            yield return obj;
            foreach (var entry in obj)
                if (entry.Value is not null)
                    foreach (var child in Objects(entry.Value)) yield return child;
        }
        else if (node is JsonArray array)
            foreach (var item in array)
                if (item is not null)
                    foreach (var child in Objects(item)) yield return child;
    }
    public static JsonObject Control(string model, string route) => new()
    {
        ["modelPath"] = new JsonObject { ["in /"] = model },
        ["layoutPath"] = new JsonObject { ["in /"] = route }
    };
    public static StationeryLayoutResult Arrange(StationeryStyleSettings settings, double width, double height, string page = "/mdlDemo/mdlTopDemoPage")
    {
        foreach (var binding in settings.Bindings)
        {
            var layout = settings.Layouts.Single(l => l.Path == binding.Layout);
            if (layout.Type != "tabbed-box-layout") continue;
            var index = binding.Children.ToList().FindIndex(c => c.ModelPath == page || page.StartsWith(c.ModelPath + "/", StringComparison.Ordinal));
            if (index >= 0) layout.SelectedTabIndex = index;
        }
        var keys = settings.ControlTree.Where(p => p.Value.LayoutPath is null).ToDictionary(p => p.Key,
            p => p.Value.ModelPaths.FirstOrDefault(m => m.Value == page || m.Value.StartsWith(page + "/", StringComparison.Ordinal)).Key
                ?? p.Value.LayoutPaths.Keys.First());
        return StationeryLayoutEngine.Arrange(settings, width, height, keys);
    }
    public static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Invalid settings were accepted.");
    }
}
