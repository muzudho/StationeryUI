using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ModelLayoutTests
{
    public static void Run()
    {
        var source = """
            {"model":{"id":"demo","type":"viewport","children":[
              {"id":"leftPage","type":"page","children":[{"id":"nameField","type":"textBox"}]},
              {"id":"rightPage","type":"page","children":[{"id":"nameField","type":"textBox"}]}
            ]},"layout":[{"id":"/demo","type":"viewport","padding":{"left":"24px"}}]}
            """;
        var settings = StationeryStyleSettings.Parse(source);
        var tree = settings.Model.CreateTree();
        Require(tree.Resolve("/demo/leftPage/nameField")?.Kind == "textBox", "left path");
        Require(tree.Resolve("/demo/rightPage/nameField")?.Kind == "textBox", "right path");
        Require(settings.Layout[0].Padding.Left == 24, "layout supplies padding");
        Require(StationeryStyleSettings.Parse(source.Replace("24px", "80px")).Model.CreateTree().Resolve("/demo/leftPage/nameField") is not null,
            "layout edits preserve model paths");
        foreach (var invalid in new[]
        {
            "{}", "{\"viewport\":{}}", source.Replace("\"model\"", "\"missing\""),
            source.Replace("\"layout\"", "\"missing\""), source.Replace("\"id\":\"/demo\"", "\"id\":\"id\""),
            source.Replace("\"id\":\"/demo\"", "\"id\":\"/demo/leftPage\""),
            source.Replace("rightPage", "leftPage"), source.Replace("leftPage", "left-page"),
            source.Replace("\"type\":\"viewport\",\"padding\"", "\"type\":\"row\",\"padding\""),
            source.Replace("\"padding\":{\"left\":\"24px\"}", "\"children\":[]"),
            source.Replace("\"model\":", "\"viewport\":{},\"model\":")
        }) Reject(() => StationeryStyleSettings.Parse(invalid));

        // The demo binds actions by code-defined role, while model containers define their paths.
        var binding = DemoModelBinding.Create(DemoModelBinding.Fallback);
        Require(binding.Main["nameField"].Path == "/demo/nameField", "flat model binding");
        Require(binding.DialogControls["nameField"].Path == "/demo/editDialog/nameField", "dialog binding");
        var modelJson = JsonSerializer.SerializeToNode(DemoModelBinding.Fallback.Model, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
        var children = modelJson["children"]!.AsArray();
        var name = children[0]!;
        children.RemoveAt(0);
        children.Insert(0, new JsonObject { ["id"] = "inputs", ["type"] = "container", ["children"] = new JsonArray(name) });
        var wrapped = new JsonObject { ["model"] = modelJson, ["layout"] = new JsonArray(new JsonObject { ["id"] = "demo", ["type"] = "viewport" }) };
        var wrappedSettings = StationeryStyleSettings.Parse(wrapped.ToJsonString());
        var rebound = DemoModelBinding.Create(wrappedSettings);
        Require(rebound.Main["nameField"].Path == "/demo/inputs/nameField", "model container affects path");
        Reject(() => DemoModelBinding.Create(settings)); // Missing demo controls.
        name["type"] = "button";
        Reject(() => DemoModelBinding.Create(StationeryStyleSettings.Parse(wrapped.ToJsonString())));

        // Semantic validation runs before a reload is committed.
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stationery-model-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var config = System.IO.Path.Combine(directory, "config.json");
            var style = System.IO.Path.Combine(directory, "style.json");
            File.WriteAllText(config, "{\"styleFile\":\"style.json\"}");
            File.WriteAllText(style, source);
            var file = new StationeryStyleFile(config, DemoModelBinding.Fallback, value => { _ = DemoModelBinding.Create(value); });
            Require(ReferenceEquals(file.Current, DemoModelBinding.Fallback) && file.LastError is not null, "invalid model retains fallback");
            name["type"] = "textBox";
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(DemoModelBinding.Create(file.Current).Main["nameField"].Path == "/demo/inputs/nameField" && file.LastError is null, "recovery uses new model");
            var good = file.Current;
            File.WriteAllText(style, source);
            file.Reload();
            Require(ReferenceEquals(good, file.Current), "invalid reload retains entire last-good snapshot");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Expected invalid model/layout to be rejected.");
    }
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
