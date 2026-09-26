using StationeryUI.Controls;
using StationeryUI.Editor;

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
        document.Tree.SetExpanded(viewports, false);
        var reloaded = ReadJsonTree.Create(json + " ", document);
        Check(reloaded.CopyPath(reloaded.Tree.TargetItem) == path, "selection survives document reload");
        var reloadedViewports = Flatten(reloaded.Tree.Roots).Single(item => reloaded.CopyPath(item) == "/viewports");
        Check(!reloadedViewports.IsExpanded, "collapsed branches survive document reload");
    }

    private static IEnumerable<TreeItem> Flatten(IEnumerable<TreeItem> items)
        => items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
