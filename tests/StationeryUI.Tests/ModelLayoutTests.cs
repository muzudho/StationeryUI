using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ModelLayoutTests
{
    public static void Run()
    {
        const string source = """
        {"modelTree":{"id":"demo","type":"viewport","children":[
          {"id":"leftPage","type":"page","children":[{"id":"nameField","type":"textBox"}]},
          {"id":"rightPage","type":"page","children":[{"id":"nameField","type":"textBox"}]}]},
         "layouts":[{"id":"unrelatedLayoutId","type":"box-layout","padding":{"left":"24px"}}],
         "controlTree":{"arbitraryHandle":{"modelPath":{"in /":"/demo"},"layoutPath":{"in /":"unrelatedLayoutId"}}}}
        """;
        var settings = StationeryStyleSettings.Parse(source);
        var tree = settings.ModelTree.CreateTree();
        Require(tree.Resolve("/demo/leftPage/nameField")?.Kind == "textBox" && tree.Resolve("/demo/rightPage/nameField")?.Kind == "textBox", "scoped model identity");
        Require(settings.Padding.Left == 24, "layout padding found through explicit model route");
        var alias = StationeryStyleSettings.Parse(source.Replace("box-layout", "panel"));
        Require(alias.Padding == settings.Padding && alias.Layouts.Single().Type == "box-layout", "legacy type alias preserves semantics");
        foreach (var bad in new[] { "{}", "null", "[]", source.Replace("modelTree", "models"), source.Replace("rightPage", "leftPage"), source.Replace("leftPage", "left-page"), source.Replace("/demo\"", "/missing\""), source.Replace("box-layout", "viewport") })
            StyleTestData.Reject(() => StationeryStyleSettings.Parse(bad));
        foreach (var value in new[] { "null", "[]", "{}", "1" })
            StyleTestData.Reject(() => StationeryStyleSettings.Parse("{\"modelTree\":" + value + "}"));
        foreach (var legacySection in new[] { "models", "bindings", "bindingsV2" })
        {
            var bad = JsonNode.Parse(source)!; bad[legacySection] = new JsonArray();
            StyleTestData.Reject(() => StationeryStyleSettings.Parse(bad.ToJsonString()));
        }
        var shipped = StyleTestData.Demo();
        var shippedSettings = StationeryStyleSettings.Parse(shipped.ToJsonString());
        var navigator = new StationeryViewNavigator(shippedSettings);
        const string partialTarget = "/vMainViewport/vPartialDemoPage/vBody/vContentSwitcher";
        Require(navigator.SelectedChild(partialTarget) == "vSummaryPane", "inactive partial region loads its saved initial selection");
        navigator.SelectLink("/vMainViewport/vPartialDemoPage/vBody/vDetailsLink");
        Require(navigator.SelectedChild(partialTarget) == "vDetailsPane" &&
            shippedSettings.Layouts.Single(layout => layout.Id == "csPartialContentTabs").SelectedTabIndex == 1,
            "configured link selects only its target region");
        Require(shippedSettings.Layouts.Single(layout => layout.Id == "csTabbedPages").SelectedTabIndex == 0,
            "partial transition keeps the outer page selected");
        StyleTestData.Reject(() => navigator.Select(partialTarget, "vMissingPane"));
        Require(DemoModelBinding.Create(shippedSettings).Main["nameField"].Kind == "textBox", "shipped code binding");
        Require(DemoModelBinding.Create(DemoModelBinding.Fallback).Signature == DemoModelBinding.Create(shippedSettings).Signature, "fallback identities");
        var view = StyleTestData.Objects(shipped["viewports"]!).First(n => (string?)n["name"] == "vNameField");
        var originalLayouts = StationeryUI.Editor.StyleBlueprint.LayoutNodes(shipped).ToDictionary(p => p.Path, p => p.Node.ToJsonString());
        // Model and control IDs are independent: change the model ID and its explicit reference only.
        var model = shipped["modelTree"]!["children"]![0]!["children"]![0]!["children"]![0]!;
        model["id"] = "nameField";
        foreach (var entry in view["modelPath"]!.AsObject().ToArray()) view["modelPath"]![entry.Key] = "/mdlDemo/mdlTopDemoPage/mdlBody/nameField";
        var renamed = StationeryStyleSettings.Parse(shipped.ToJsonString());
        Require(DemoModelBinding.Create(renamed).Main["nameField"].Path.EndsWith("/nameField"), "explicit model reference preserves code role");
        Require(StationeryUI.Editor.StyleBlueprint.LayoutNodes(shipped).All(p => originalLayouts[p.Path] == p.Node.ToJsonString()), "model change leaves layouts untouched");
        model["type"] = "button";
        StyleTestData.Reject(() => DemoModelBinding.Create(StationeryStyleSettings.Parse(shipped.ToJsonString())));
        var directory = Path.Combine(Path.GetTempPath(), "model-layout-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var config = Path.Combine(directory, "config.json"); var path = Path.Combine(directory, "style.json");
            File.WriteAllText(config, """{"styleFile":"style.json","autoReload":true}"""); File.WriteAllText(path, source);
            var file = new StationeryStyleFile(config, DemoModelBinding.Fallback, value => DemoModelBinding.Create(value));
            Require(ReferenceEquals(file.Current, DemoModelBinding.Fallback) && file.LastError is not null, "semantic startup failure keeps fallback");
            model["type"] = "textBox";
            void Reload() { File.WriteAllText(path, shipped.ToJsonString()); file.Update(TimeSpan.FromSeconds(.5)); file.Update(TimeSpan.FromSeconds(.5)); }
            Reload(); Require(file.LastError is null, "recovers valid model tree");
            var good = file.Current;
            StyleTestData.Layout(shipped, "/csTopDemoGridLayout")["row-definitions"]![0] = "-1rate";
            Reload(); Require(ReferenceEquals(good, file.Current) && file.LastError is not null, "invalid rate preserves snapshot");
            StyleTestData.Layout(shipped, "/csTopDemoGridLayout")["row-definitions"]![0] = "1.5rate";
            Reload(); Require(file.LastError is null && file.Current.Layouts.Single(l => l.Id == "csTopDemoGridLayout").Rows[0].Value == 1.5, "fractional reload");
            StyleTestData.Layout(shipped, "/csVerticalSplitPaneLayout")["ratio"] = .65;
            Reload(); Require(file.LastError is null && file.Current.Layouts.Single(l => l.Id == "csVerticalSplitPaneLayout").Split!.Ratio == .65, "split ratio reload");
            good = file.Current; StyleTestData.Layout(shipped, "/csVerticalSplitPaneLayout")["ratio"] = 2;
            Reload(); Require(ReferenceEquals(good, file.Current) && file.LastError is not null, "invalid split retains snapshot");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
