using StationeryUI.Controls;
using StationeryUI.Editor;
using StationeryUI.Styling;

internal static class ReadJsonTreeTests
{
    public static void Run()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json"));
        var document = ReadJsonTree.Create(json);
        var all = Flatten(document.Tree.Roots).ToArray();
        var model = all.Single(item => document.CopyPath(item) == "/modelTree");
        Check(document.InspectionPath(model) == "/mdlDemo", "model root maps to its inspection path");
        var field = all.First(item => document.InspectionPath(item) == "/mdlDemo/mdlTopDemoPage/mdlBody/mdlNameField"
            && item.Children.Count > 0);
        var id = field.Children.First(item => item.Label.StartsWith("id:", StringComparison.Ordinal));
        Check(document.InspectionPath(id) == document.InspectionPath(field), "property maps to its owning model");
        var path = document.CopyPath(id)!;
        Check(document.Details(id).Contains(path, StringComparison.Ordinal), "details show the JSON path");
        Check(document.SelectPath(path.Split('/', StringSplitOptions.RemoveEmptyEntries)), "JSON path can be selected");
        var viewports = all.Single(item => document.CopyPath(item) == "/viewports");
        var gridReference = all.Single(item => item.Label == "ref: grid");
        Check(document.LayoutInspectionPaths(gridReference).Count > 0, "referenced layout in demo maps to inspection");
        document.Tree.SetExpanded(viewports, false);
        var reloaded = ReadJsonTree.Create(json + " ", document);
        Check(reloaded.CopyPath(reloaded.Tree.TargetItem) == path, "selection survives document reload");
        var reloadedViewports = Flatten(reloaded.Tree.Roots).Single(item => reloaded.CopyPath(item) == "/viewports");
        Check(!reloadedViewports.IsExpanded, "collapsed branches survive document reload");
        CheckSharedLayoutMapping();
    }

    private static void CheckSharedLayoutMapping()
    {
        const string json = """
        {
          "layouts": [{"id":"shared","type":"box-layout","children":[{"id":"inner","type":"box-layout"}]}],
          "viewports": [{"name":"first","layout":{"ref":"shared"}}],
          "modelTree": {"id":"root","type":"viewport"}
        }
        """;
        var settings = StationeryStyleSettings.Parse("""{"modelTree":{"id":"root","type":"viewport"},"layouts":[],"controlTree":{}}""")
            with { Bindings = [new("/shared", "/root/first", []), new("/shared", "/root/second", [])] };
        var document = ReadJsonTree.Create(json, settings: settings);
        var items = Flatten(document.Tree.Roots).ToArray();
        var definition = items.Single(item => document.CopyPath(item) == "/layouts/0");
        var nested = items.Single(item => document.CopyPath(item) == "/layouts/0/children/0/type");
        var reference = items.Single(item => document.CopyPath(item) == "/viewports/0/layout/ref");
        var expected = new[] { "/root/first:/shared", "/root/second:/shared" };
        Check(document.LayoutInspectionPaths(definition).SequenceEqual(expected), "one layout definition lists both owners");
        Check(document.ContainsInspectionPath(expected[0]) && document.ContainsInspectionPath(expected[1])
            && !document.ContainsInspectionPath("/root/third:/shared"), "document distinguishes mapped and runtime-only layouts");
        Check(document.LayoutInspectionPaths(reference).SequenceEqual(expected), "layout ref resolves to the same owners");
        Check(document.LayoutInspectionPaths(nested).SequenceEqual(new[] { "/root/first:/shared/inner", "/root/second:/shared/inner" }),
            "nested layout properties keep the nested definition path");
        Check(document.Details(definition).Contains("2 件", StringComparison.Ordinal)
            && document.InspectionPath(definition) == expected[0], "details and preview selection expose shared instances");
    }

    private static IEnumerable<TreeItem> Flatten(IEnumerable<TreeItem> items)
        => items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
