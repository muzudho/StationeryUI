using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ModelLayoutTests
{
    public static void Run()
    {
        var shipped = StationeryStyleSettings.Parse(File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json")));
        Require(shipped.Models.Count == 1 && shipped.Layouts.Count == 1, "shipped style uses arrays");
        Require(DemoModelBinding.Create(shipped).Main["nameField"].Kind == "textBox", "shipped style binds to demo controls");
        var source = """
            {"models":[{"id":"demo","type":"viewport","children":[
              {"id":"leftPage","type":"page","children":[{"id":"nameField","type":"textBox"}]},
              {"id":"rightPage","type":"page","children":[{"id":"nameField","type":"textBox"}]}
            ]}],"layouts":[{"id":"/demo","type":"viewport","padding":{"left":"24px"}}]}
            """;
        var settings = StationeryStyleSettings.Parse(source);
        Require(settings.Models.Count == 1, "models array retains its root");
        var tree = settings.Models[0].CreateTree();
        Require(tree.Resolve("/demo/leftPage/nameField")?.Kind == "textBox", "left path");
        Require(tree.Resolve("/demo/rightPage/nameField")?.Kind == "textBox", "right path");
        Require(settings.Layouts[0].Padding.Left == 24, "layouts supplies padding");
        Require(StationeryStyleSettings.Parse(source.Replace("24px", "80px")).Models[0].CreateTree().Resolve("/demo/leftPage/nameField") is not null,
            "layouts edits preserve models paths");
        foreach (var invalid in new[]
        {
            "{}", "{\"viewport\":{}}", source.Replace("\"models\"", "\"missing\""),
            source.Replace("\"layouts\"", "\"missing\""), source.Replace("\"id\":\"/demo\"", "\"id\":\"id\""),
            source.Replace("\"id\":\"/demo\"", "\"id\":\"/demo/leftPage\""),
            source.Replace("rightPage", "leftPage"), source.Replace("leftPage", "left-page"),
            source.Replace("\"type\":\"viewport\",\"padding\"", "\"type\":\"row\",\"padding\""),
            source.Replace("\"padding\":{\"left\":\"24px\"}", "\"children\":[]"),
            source.Replace("\"models\":", "\"viewport\":{},\"models\":")
        }) Reject(() => StationeryStyleSettings.Parse(invalid));
        Reject(() => StationeryStyleSettings.Parse(source.Replace("\"models\"", "\"model\"")));
        Reject(() => StationeryStyleSettings.Parse(source.Replace("\"layouts\"", "\"layout\"")));
        Reject(() => StationeryStyleSettings.Parse(source.Replace("\"models\":", "\"model\":[],\"models\":")));

        foreach (var invalidModel in new[] { "{}", "null", "[]", "[null]", "[1]",
            "[{\"id\":\"demo\",\"type\":\"viewport\"},{\"id\":\"other\",\"type\":\"viewport\"}]" })
            Reject(() => StationeryStyleSettings.Parse("{\"models\":" + invalidModel + ",\"layouts\":[{\"id\":\"demo\",\"type\":\"viewport\"}]}"));

        // The demo binds actions by code-defined role, while models containers define their paths.
        var binding = DemoModelBinding.Create(DemoModelBinding.Fallback);
        Require(binding.Main["nameField"].Path == "/demo/nameField", "flat models binding");
        Require(binding.DialogControls["nameField"].Path == "/demo/editDialog/nameField", "dialog binding");
        var modelJson = JsonSerializer.SerializeToNode(DemoModelBinding.Fallback.Models, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
        var children = modelJson[0]!["children"]!.AsArray();
        var name = children[0]!;
        children.RemoveAt(0);
        children.Insert(0, new JsonObject { ["id"] = "inputs", ["type"] = "container", ["children"] = new JsonArray(name) });
        var wrapped = new JsonObject { ["models"] = modelJson, ["layouts"] = new JsonArray(new JsonObject { ["id"] = "demo", ["type"] = "viewport" }) };
        var wrappedSettings = StationeryStyleSettings.Parse(wrapped.ToJsonString());
        var rebound = DemoModelBinding.Create(wrappedSettings);
        Require(rebound.Main["nameField"].Path == "/demo/inputs/nameField", "models container affects path");
        Reject(() => DemoModelBinding.Create(settings)); // Missing demo controls.
        name["type"] = "button";
        Reject(() => DemoModelBinding.Create(StationeryStyleSettings.Parse(wrapped.ToJsonString())));

        // Semantic validation runs before a reload is committed.
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stationery-models-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var config = System.IO.Path.Combine(directory, "config.json");
            var style = System.IO.Path.Combine(directory, "style.json");
            File.WriteAllText(config, "{\"styleFile\":\"style.json\"}");
            File.WriteAllText(style, source);
            var file = new StationeryStyleFile(config, DemoModelBinding.Fallback, value => { _ = DemoModelBinding.Create(value); });
            Require(ReferenceEquals(file.Current, DemoModelBinding.Fallback) && file.LastError is not null, "invalid models retains fallback");
            name["type"] = "textBox";
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(DemoModelBinding.Create(file.Current).Main["nameField"].Path == "/demo/inputs/nameField" && file.LastError is null, "recovery uses new models");
            var good = file.Current;
            File.WriteAllText(style, source);
            file.Reload();
            Require(ReferenceEquals(good, file.Current), "invalid reload retains entire last-good snapshot");
            File.WriteAllText(style, "{\"models\":{},\"layouts\":[]}");
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(ReferenceEquals(good, file.Current) && file.LastError is not null, "old object format preserves last-good snapshot");
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(file.LastError is null && file.Current.Models.Count == 1, "array format recovers after invalid reload");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Expected invalid models/layouts to be rejected.");
    }
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
