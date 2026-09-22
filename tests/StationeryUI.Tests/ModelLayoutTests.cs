using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ModelLayoutTests
{
    public static void Run()
    {
        var shippedText = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-style.json"));
        var shipped = StationeryStyleSettings.Parse(shippedText);
        Require(shipped.Models.Count == 1 && shipped.Layouts.Any(l => l.Type == "work-page-layout") && shipped.Bindings.Any(b => b.InspectorModel is not null), "shipped independent arrays");
        Require(DemoModelBinding.Create(shipped).Main["nameField"].Kind == "textBox", "shipped code binding");
        var source = """
            {"models":[{"id":"demo","type":"viewport","children":[
              {"id":"leftPage","type":"page","children":[{"id":"nameField","type":"textBox"}]},
              {"id":"rightPage","type":"page","children":[{"id":"nameField","type":"textBox"}]}
            ]}],"layouts":[{"id":"unrelatedLayoutId","type":"box-layout","padding":{"left":"24px"}}],
            "bindings":[{"layout":"unrelatedLayoutId","model":"/demo"}]}
            """;
        var settings = StationeryStyleSettings.Parse(source);
        var tree = settings.Models[0].CreateTree();
        Require(tree.Resolve("/demo/leftPage/nameField")?.Kind == "textBox", "left path");
        Require(tree.Resolve("/demo/rightPage/nameField")?.Kind == "textBox", "right path");
        Require(settings.Padding.Left == 24, "padding is found through bindings");
        var legacy = StationeryStyleSettings.Parse(source.Replace("box-layout", "panel"));
        Require(legacy.Layouts.Single().Type == "box-layout" && legacy.Padding == settings.Padding,
            "legacy panel normalizes and retains bound padding");
        var expected = StationeryLayoutEngine.Arrange(settings, 300, 200);
        var actual = StationeryLayoutEngine.Arrange(legacy, 300, 200);
        Require(expected.Bounds.All(p => actual.Bounds[p.Key] == p.Value) &&
            expected.ContentBounds.All(p => actual.ContentBounds[p.Key] == p.Value), "legacy box geometry unchanged");
        foreach (var invalid in new[]
        {
            "{}", "{\"viewport\":{}}", source.Replace("\"models\"", "\"missing\""),
            source.Replace("\"layouts\"", "\"missing\""), source.Replace("\"bindings\"", "\"missing\""),
            source.Replace("\"model\":\"/demo\"", "\"model\":\"/missing\""),
            source.Replace("rightPage", "leftPage"), source.Replace("leftPage", "left-page"),
            source.Replace("\"type\":\"box-layout\"", "\"type\":\"viewport\""),
            source.Replace("\"models\":", "\"viewport\":{},\"models\":"),
            source.Replace("\"models\":", "\"model\":[],\"models\":")
        }) Reject(() => StationeryStyleSettings.Parse(invalid));

        foreach (var invalidModel in new[] { "{}", "null", "[]", "[null]", "[1]",
            "[{\"id\":\"demo\",\"type\":\"viewport\"},{\"id\":\"other\",\"type\":\"viewport\"}]" })
            Reject(() => StationeryStyleSettings.Parse("{\"models\":" + invalidModel + ",\"layouts\":[],\"bindings\":[]}"));

        var binding = DemoModelBinding.Create(DemoModelBinding.Fallback);
        Require(binding.Main["nameField"].Path == "/demo/topDemoPage/nameField", "flat model binding");
        Require(binding.DialogControls["nameField"].Path == "/demo/topDemoPage/editDialog/nameField", "dialog binding");
        var wrapped = JsonNode.Parse(shippedText)!;
        var children = wrapped["models"]![0]!["children"]![0]!["children"]!.AsArray();
        var name = children[0]!;
        children.RemoveAt(0);
        children.Insert(0, new JsonObject { ["id"] = "inputs", ["type"] = "container", ["children"] = new JsonArray(name) });
        // Only bindings must change when a model moves; the layouts definition stays identical.
        var layoutBefore = wrapped["layouts"]!.ToJsonString();
        wrapped["bindings"]![1]!["childrenModel"]![0]!["model"] = "inputs/nameField";
        var wrappedSettings = StationeryStyleSettings.Parse(wrapped.ToJsonString());
        Require(DemoModelBinding.Create(wrappedSettings).Main["nameField"].Path == "/demo/topDemoPage/inputs/nameField", "scoped child path");
        Require(layoutBefore == wrapped["layouts"]!.ToJsonString(), "layout definitions are independent");
        Reject(() => DemoModelBinding.Create(settings));
        name["type"] = "button";
        Reject(() => DemoModelBinding.Create(StationeryStyleSettings.Parse(wrapped.ToJsonString())));

        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "stationery-model-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var config = System.IO.Path.Combine(directory, "config.json");
            var style = System.IO.Path.Combine(directory, "style.json");
            File.WriteAllText(config, "{\"styleFile\":\"style.json\"}");
            File.WriteAllText(style, source);
            var file = new StationeryStyleFile(config, DemoModelBinding.Fallback, value => { _ = DemoModelBinding.Create(value); });
            Require(ReferenceEquals(file.Current, DemoModelBinding.Fallback) && file.LastError is not null, "invalid startup retains fallback");
            name["type"] = "textBox";
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(DemoModelBinding.Create(file.Current).Main["nameField"].Path == "/demo/topDemoPage/inputs/nameField" && file.LastError is null, "recovery");
            var good = file.Current;
            wrapped["layouts"]![1]!["row-definitions"]![0] = "-1rate";
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(ReferenceEquals(good, file.Current) && file.LastError is not null, "invalid rate retains whole snapshot");
            wrapped["layouts"]![1]!["row-definitions"]![0] = "1.5rate";
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(file.LastError is null && file.Current.Layouts[1].Rows[0].Value == 1.5, "fractional rate reload");
            wrapped["layouts"]![3]!["ratio"] = .65;
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(file.LastError is null && file.Current.Layouts[3].Split!.Ratio == .65, "split ratio reload");
            var goodSplit = file.Current;
            wrapped["layouts"]![3]!["ratio"] = 2;
            File.WriteAllText(style, wrapped.ToJsonString());
            file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5));
            Require(ReferenceEquals(goodSplit, file.Current) && file.LastError is not null, "invalid split ratio preserves snapshot");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Reject(Action action)
    {
        try { action(); } catch (JsonException) { return; }
        throw new Exception("Expected invalid models/layouts/bindings to be rejected.");
    }
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
