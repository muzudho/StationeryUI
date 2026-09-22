namespace StationeryUI.Inspection;

using StationeryUI.Styling;

/// <summary>Attaches current style bindings to inspection snapshots without changing model identities.</summary>
public static class DeveloperInspectionLayout
{
    public static IReadOnlyList<StationeryInspectionEntry> Apply(
        IReadOnlyList<StationeryInspectionEntry> entries, StationeryStyleSettings settings, StationeryLayoutResult? arranged = null)
    {
        var modelMargins = settings.GetModelMargins();
        var layouts = settings.Layouts.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var types = settings.Bindings.GroupBy(binding => binding.ModelPath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(binding => layouts[("/" + binding.Layout.Split('/')[1])].Type)
                .Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var cells = settings.Bindings.SelectMany(binding => binding.Children)
            .ToDictionary(cell => cell.ModelPath, cell => new StationeryInspectionCell(cell.Column, cell.Row, cell.ColumnSpan, cell.RowSpan), StringComparer.Ordinal);
        var errors = settings.Bindings.Where(b => b.LayoutError is not null).GroupBy(b => b.ModelPath)
            .ToDictionary(g => g.Key, g => string.Join("\n", g.Select(b => b.LayoutError)), StringComparer.Ordinal);
        var roots = settings.Bindings.GroupBy(b => b.ModelPath).ToDictionary(g => g.Key, g => layouts["/" + g.First().Layout.Split('/')[1]]);
        StationeryBoxModel BoxModel(string path)
        {
            var root = roots.GetValueOrDefault(path);
            var modelMargin = modelMargins.GetValueOrDefault(path);
            var layoutMargin = root?.Margin ?? default;
            return new(new(modelMargin.Top + layoutMargin.Top, modelMargin.Right + layoutMargin.Right,
                modelMargin.Bottom + layoutMargin.Bottom, modelMargin.Left + layoutMargin.Left),
                root?.Padding ?? default, root?.Border ?? default);
        }
        var parents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var binding in settings.Bindings)
        {
            var parent = binding.ModelPath + ":" + binding.Layout;
            foreach (var child in binding.Children) parents[child.ModelPath] = parent;
            foreach (var child in binding.DockChildren) parents[child.ModelPath] = parent;
            foreach (var child in new[] { binding.FirstModel, binding.SecondModel, binding.InspectorModel })
                if (child is not null) parents[child] = parent;
        }
        IReadOnlyList<StationeryInspectionLine> Partitions(string owner, StationeryLayoutNode layout)
            => !errors.ContainsKey(owner) && arranged?.LayoutContentBounds.TryGetValue(owner + ":" + layout.Path, out var content) == true
                ? DeveloperInspectionPartitions.Create(layout, content) : [];
        StationeryInspectionEntry[] LayoutNodes(StationeryInspectionEntry owner)
        {
            if (!roots.TryGetValue(owner.Path, out var root)) return [];
            return settings.Layouts.Where(l => l.Path == root.Path || l.Path.StartsWith(root.Path + "/", StringComparison.Ordinal))
                .Select(l => new StationeryInspectionEntry(l.Id, owner.Path + ":" + l.Path,
                    l.Path == root.Path ? owner.Path : owner.Path + ":" + l.ParentPath,
                    "layout", l.Path, owner.Visible,
                    arranged?.LayoutBounds.TryGetValue(owner.Path + ":" + l.Path, out var bounds) == true ? bounds : null)
                {
                    MarginBounds = arranged?.LayoutMarginBounds.TryGetValue(owner.Path + ":" + l.Path, out var marginBounds) == true ? marginBounds : null,
                    PartitionLines = Partitions(owner.Path, l),
                    LayoutTypes = [l.Type],
                    Cell = l.Path == root.Path ? null : new(l.Column, l.Row, l.ColumnSpan, l.RowSpan),
                    BoxModel = new(l.Margin, l.Padding, l.Border)
                }).ToArray();
        }
        return entries.Select(entry => entry with
        {
            MarginBounds = arranged?.MarginBounds.TryGetValue(entry.Path, out var marginBounds) == true ? marginBounds : null,
            PartitionLines = roots.TryGetValue(entry.Path, out var rootLayout) ? Partitions(entry.Path, rootLayout) : [],
            LayoutNodes = LayoutNodes(entry),
            LayoutParentPath = parents.GetValueOrDefault(entry.Path),
            LayoutTypes = types.GetValueOrDefault(entry.Path),
            Cell = cells.GetValueOrDefault(entry.Path),
            LayoutError = errors.GetValueOrDefault(entry.Path),
            BoxModel = BoxModel(entry.Path)
        }).ToArray();
    }

    /// <summary>Finds a visible model or layout for an outline; does not change capture hit testing.</summary>
    public static StationeryInspectionEntry? FindVisibleEntry(IReadOnlyList<StationeryInspectionEntry> entries, string path)
        => entries.Where(e => e.Visible).SelectMany(e => new[] { e }.Concat(e.LayoutNodes ?? []))
            .FirstOrDefault(e => e.Path == path && e.Visible && (e.WindowBounds is { Width: > 0, Height: > 0 } || e.MarginBounds is { Width: > 0, Height: > 0 }));

    public static string FormatModelLabel(StationeryInspectionEntry entry)
    {
        var kind = string.IsNullOrEmpty(entry.Kind) ? "—" : char.ToUpperInvariant(entry.Kind[0]) + entry.Kind[1..];
        return $"({entry.Id} : {kind})";
    }

    public static string FormatLabel(StationeryInspectionEntry entry)
    {
        var kind = string.IsNullOrEmpty(entry.Kind) ? "—" : char.ToUpperInvariant(entry.Kind[0]) + entry.Kind[1..];
        var layout = entry.LayoutTypes is { Count: > 0 }
            ? string.Join(", ", entry.LayoutTypes.Select(type => type switch
            {
                "grid-layout" => "gridLayout", "box-layout" => "boxLayout",
                "dock-layout" => "dockLayout",
                "fullscreen-layout" => "fullscreenLayout", "work-page-layout" => "workPageLayout",
                "split-pane" => "splitPane", _ => type
            }))
            : "-";
        var placement = entry.Cell is { } cell
            ? FormattableString.Invariant($"{cell.Column}, {cell.Row}, {cell.ColumnSpan}, {cell.RowSpan}") : "-";
        return $"({entry.Id} : {kind}) ({placement} : {layout})" + (entry.LayoutError is null ? "" : " （レイアウトエラー）") + (entry.Visible ? "" : "  （非表示）");
    }
}
