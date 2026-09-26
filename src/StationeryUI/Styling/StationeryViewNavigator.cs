namespace StationeryUI.Styling;

using System.Text.Json;

/// <summary>Applies the initial viewport snapshot and link-driven selections to tabbed view regions.</summary>
public sealed class StationeryViewNavigator
{
    private readonly StationeryStyleSettings settings;
    private readonly Dictionary<string, ViewNode> views = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> selected = new(StringComparer.Ordinal);

    public StationeryViewNavigator(StationeryStyleSettings settings)
    {
        this.settings = settings;
        foreach (var root in settings.ViewportsTree.ChildNodes) Index(root, "");
        foreach (var root in settings.ViewportSnapshotTree.ChildNodes) ReadSnapshot(root, "");
        foreach (var (target, child) in settings.ViewportsSnapshot) Select(target, child);
        foreach (var link in views.Values.Where(view => view.OnClick is not null))
            _ = ResolveSelection(link.OnClick!.Target, link.OnClick.Child);
        Apply();
    }

    public string? SelectedChild(string target) => selected.GetValueOrDefault(target);

    public void SelectLink(string linkPath)
    {
        if (!views.TryGetValue(linkPath, out var link) || link.OnClick is null)
            throw new JsonException($"'{linkPath}' is not a configured navigation link.");
        var transition = link.OnClick;
        Select(transition.Target, transition.Child);
    }

    public void Select(string target, string child)
    {
        var (layout, index) = ResolveSelection(target, child);
        selected[target] = child;
        layout.SelectedTabIndex = index;
    }

    public void Apply()
    {
        foreach (var (target, child) in selected)
        {
            var (layout, index) = ResolveSelection(target, child);
            layout.SelectedTabIndex = index;
        }
    }

    private void Index(ViewNode node, string parent)
    {
        var path = parent + "/" + node.Name;
        if (!views.TryAdd(path, node)) throw new JsonException($"Duplicate view path '{path}'.");
        foreach (var child in node.ChildNodes) Index(child, path);
    }

    private void ReadSnapshot(ViewNode snapshot, string parent)
    {
        var path = parent + "/" + snapshot.Name;
        if (!views.ContainsKey(path)) throw new JsonException($"Snapshot references unknown view '{path}'.");
        if (snapshot.ChildNodes.Count > 0 && IsTabbed(views[path]))
        {
            if (snapshot.ChildNodes.Count != 1)
                throw new JsonException($"Snapshot for tabbed view '{path}' must select one child.");
            Select(path, snapshot.ChildNodes[0].Name);
        }
        foreach (var child in snapshot.ChildNodes) ReadSnapshot(child, path);
    }

    private bool IsTabbed(ViewNode view)
    {
        var name = LayoutName(view);
        return name is not null && settings.Layouts.Any(layout => layout.Id == name && layout.Type == "tabbed-box-layout");
    }

    private (StationeryLayoutNode Layout, int Index) ResolveSelection(string target, string child)
    {
        if (!views.TryGetValue(target, out var view)) throw new JsonException($"Unknown view target '{target}'.");
        var name = LayoutName(view);
        var layouts = settings.Layouts.Where(layout => layout.Id == name && layout.Type == "tabbed-box-layout").ToArray();
        if (layouts.Length != 1) throw new JsonException($"View '{target}' must own one TabbedBoxLayout.");
        var candidates = view.ChildNodes.Where(node => node.Name == child).ToArray();
        if (candidates.Length != 1) throw new JsonException($"'{child}' must be one direct child of '{target}'.");
        var place = candidates[0].Place;
        if (place is not { ValueKind: JsonValueKind.Number } || !place.Value.TryGetInt32(out var index) || index < 0 ||
            index >= view.ChildNodes.Count || view.ChildNodes.Count(node => node.Place is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var n) && n == index) != 1)
            throw new JsonException($"'{child}' needs a unique zero-based tab place in '{target}'.");
        return (layouts[0], index);
    }

    private static string? LayoutName(ViewNode view)
    {
        if (view.Layout is not { ValueKind: JsonValueKind.Object } layout) return view.Style;
        if (layout.TryGetProperty("ref", out var reference)) return reference.GetString();
        if (layout.TryGetProperty("name", out var name)) return name.GetString();
        if (layout.TryGetProperty("id", out var id)) return id.GetString();
        return view.Style;
    }
}
