using StationeryUI.Inspection;
using StationeryUI.Styling;
using System.Text.Json;

/// <summary>Connects stable code-defined roles to models nodes. Layouts wrappers never change these roles.</summary>
internal sealed record DemoModelBinding(StationeryNode Root, StationeryNode TopPage, StationeryNode SplitPage, StationeryNode LayoutPage, StationeryNode Dialog,
    IReadOnlyDictionary<string, StationeryNode> LayoutControls,
    IReadOnlyDictionary<string, StationeryNode> SplitControls,
    IReadOnlyDictionary<string, StationeryNode> Main, IReadOnlyDictionary<string, StationeryNode> DialogControls, string Signature)
{
    public static StationeryStyleSettings Fallback { get; } = LoadFallback();

    private static StationeryStyleSettings LoadFallback()
    {
        using var stream = typeof(DemoModelBinding).Assembly.GetManifestResourceStream("StationeryUI.Demo.demo.stationery-ui.json")
            ?? throw new InvalidOperationException("The embedded demo style fallback is missing.");
        using var reader = new StreamReader(stream);
        return StationeryStyleSettings.Parse(reader.ReadToEnd());
    }

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.ModelTree.CreateTree();
        var all = Descendants(root).ToArray();
        var topPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "topDemoPage" && node.Kind == "page")
            ?? throw new JsonException("topDemoPage is required.");
        var splitPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "splitPaneDemoPage" && node.Kind == "page")
            ?? throw new JsonException("splitPaneDemoPage is required.");
        var layoutPage = root.Children.Single(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "layoutDemoPage" && node.Kind == "page");
        foreach (var page in new[] { topPage, splitPage, layoutPage })
            if (!settings.ControlTree.Values.Any(binding =>
                (binding.ModelPath is { } modelPath && modelPath.Contains("/" + page.Id + "/mdlInspectorPanel/", StringComparison.Ordinal)) ||
                binding.ModelPaths.Values.Any(modelPath => modelPath.Contains("/" + page.Id + "/mdlInspectorPanel/", StringComparison.Ordinal))))
                throw new JsonException($"{page.Path} requires a page layout bound to inspectorPanel.");
        var dialogs = all.Where(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "editDialog" && node.Kind == "dialog").ToArray();
        if (dialogs.Length != 1) throw new JsonException("Demo models requires one editDialog of type dialog.");
        var dialog = dialogs[0];
        if (!dialog.IsWithin(topPage)) throw new JsonException("editDialog must be in topDemoPage.");
        var main = Bind(all.Where(node => node.IsWithin(topPage) && !node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["nameField"] = "textBox", ["memoField"] = "textBox", ["themeButton"] = "button",
            ["scaleButton"] = "button", ["applyTitleButton"] = "button", ["openDialogButton"] = "button", ["sampleTree"] = "tree", ["splitPaneDemoLink"] = "link", ["layoutDemoLink"] = "link"
        });
        var splitControls = Bind(Descendants(splitPage), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["topDemoLink"] = "link", ["verticalSplit"] = "splitPane", ["horizontalSplit"] = "splitPane",
            ["leftPane"] = "textBox", ["rightPane"] = "textBox", ["topPane"] = "textBox", ["bottomPane"] = "textBox"
        });
        var layoutControls = Bind(Descendants(layoutPage), new Dictionary<string, string>
        {
            ["topDemoLink"] = "link", ["toolHint"] = "textBlock",
            ["title"] = "textBlock",
            ["boxTitle"] = "textBlock",
            ["boxContent"] = "textBlock",
            ["gridTitle"] = "textBlock",
            ["spanCell"] = "textBlock",
            ["nestedA"] = "textBlock",
            ["nestedB"] = "textBlock",
            ["nestedC"] = "textBlock",
            ["nestedD"] = "textBlock",
            ["gridFooter"] = "textBlock"
        });
        var dialogControls = Bind(all.Where(node => node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["cancelButton"] = "button", ["saveButton"] = "button"
        });
        foreach (var node in main.Values.Concat(splitControls.Values).Concat(layoutControls.Values))
        {
            var handle = StationeryControlHandle.FromModelId(node.Id);
            if (!settings.ControlTree.TryGetValue(handle, out var controlBinding))
                throw new JsonException($"Demo control {node.Path} requires a controlTree entry for '{handle}'.");
            var resolvedPaths = controlBinding.ModelPaths.Values.ToArray();
            if (!resolvedPaths.Contains(node.Path, StringComparer.Ordinal))
                throw new JsonException($"controlTree entry '{handle}' does not resolve to demo control {node.Path}.");
        }
        if (settings.ControlTree.Values.Any(binding =>
            (binding.ModelPath is { } modelPath && root.Resolve(modelPath)?.IsWithin(dialog) == true) ||
            binding.ModelPaths.Values.Any(path => root.Resolve(path)?.IsWithin(dialog) == true)))
            throw new JsonException("The demo dialog currently uses its code-defined layout; bind the main controls only.");
        foreach (var id in new[] { "verticalSplit", "horizontalSplit" })
        {
            var node = splitControls[id];
            var expected = id == "verticalSplit" ? new[] { "leftPane", "rightPane" } : new[] { "topPane", "bottomPane" };
            var first = HasRoute(splitControls[expected[0]].Path, "/first");
            var second = HasRoute(splitControls[expected[1]].Path, "/second");
            var hasSplitLayout = Routes().Any(binding => binding.ModelPath == node.Path &&
                LayoutIds(binding.Route).Any(layoutId => settings.Layouts.Any(layout =>
                    layout.Id == layoutId && layout.Type == "split-pane")));
            if (!hasSplitLayout || !first || !second)
                throw new JsonException("Split content must match the demo roles.");
        }

        bool HasRoute(string modelPath, string suffix) => Routes().Any(binding => binding.ModelPath == modelPath &&
            binding.Route.EndsWith(suffix, StringComparison.Ordinal));

        IEnumerable<(string ModelPath, string Route)> Routes()
        {
            foreach (var binding in settings.ControlTree.Values)
                foreach (var (context, route) in binding.LayoutPaths)
                    if (binding.ModelPaths.TryGetValue(context, out var modelPath)) yield return (modelPath, route);
        }

        var bound = main.Values.Concat(dialogControls.Values).Concat(splitControls.Values).Concat(layoutControls.Values).ToHashSet();
        foreach (var node in all)
        {
            if (bound.Contains(node))
            {
                if (node.Kind != "splitPane" && node.Children.Count != 0) throw new JsonException($"Control {node.Path} cannot own children in this demo.");
            }
            else if (node != root && node != dialog && node.Kind is not ("page" or "container"))
                throw new JsonException($"The demo has no code binding for {node.Path} ({node.Kind}).");
        }
        return new(root, topPage, splitPage, layoutPage, dialog, layoutControls, splitControls, main, dialogControls, string.Join('\n', all.Select(node => node.Path + ":" + node.Kind)));
    }

    private static IEnumerable<string> LayoutIds(string layoutPath)
    {
        foreach (var segment in layoutPath.Split('/').Skip(1))
        {
            var colon = segment.IndexOf(':');
            if (colon >= 0) yield return segment[(colon + 1)..];
        }
    }

    private static IReadOnlyDictionary<string, StationeryNode> Bind(IEnumerable<StationeryNode> nodes, Dictionary<string, string> roles)
    {
        var all = nodes.ToArray();
        var result = new Dictionary<string, StationeryNode>(StringComparer.Ordinal);
        foreach (var (id, kind) in roles)
        {
            var matches = all.Where(node => StationeryControlHandle.ModelRoleFromId(node.Id) == id).ToArray();
            if (matches.Length != 1 || matches[0].Kind != kind)
                throw new JsonException($"Demo models requires exactly one {id} of type {kind} in its main/dialog scope.");
            result.Add(id, matches[0]);
        }
        return result;
    }

    private static IEnumerable<StationeryNode> Descendants(StationeryNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}

