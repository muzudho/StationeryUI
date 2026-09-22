namespace StationeryUI.Inspection;

using StationeryUI.Styling;

/// <summary>Attaches current style bindings to inspection snapshots without changing model identities.</summary>
public static class DeveloperInspectionLayout
{
    public static IReadOnlyList<StationeryInspectionEntry> Apply(
        IReadOnlyList<StationeryInspectionEntry> entries, StationeryStyleSettings settings)
    {
        var layouts = settings.Layouts.ToDictionary(layout => layout.Path, StringComparer.Ordinal);
        var types = settings.Bindings.GroupBy(binding => binding.ModelPath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(binding => layouts[("/" + binding.Layout.Split('/')[1])].Type)
                .Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var cells = settings.Bindings.SelectMany(binding => binding.Children)
            .ToDictionary(cell => cell.ModelPath, cell => new StationeryInspectionCell(cell.Column, cell.Row, cell.ColumnSpan, cell.RowSpan), StringComparer.Ordinal);
        var errors = settings.Bindings.Where(b => b.LayoutError is not null).GroupBy(b => b.ModelPath)
            .ToDictionary(g => g.Key, g => string.Join("\n", g.Select(b => b.LayoutError)), StringComparer.Ordinal);
        var roots = settings.Bindings.GroupBy(b => b.ModelPath).ToDictionary(g => g.Key, g => layouts["/" + g.First().Layout.Split('/')[1]]);
        var margins = settings.Bindings.SelectMany(b => b.Children.Select(c => (c.ModelPath, c.Margin))
            .Concat(b.DockChildren.Select(c => (c.ModelPath, c.Margin)))).ToDictionary(c => c.ModelPath, c => c.Margin);
        StationeryBoxModel BoxModel(string path)
        {
            var margin = margins.GetValueOrDefault(path);
            var root = roots.GetValueOrDefault(path);
            var own = root?.Margin ?? default;
            return new(new(margin.Top + own.Top, margin.Right + own.Right, margin.Bottom + own.Bottom, margin.Left + own.Left), root?.Padding ?? default);
        }
        return entries.Select(entry => entry with
        {
            LayoutTypes = types.GetValueOrDefault(entry.Path),
            Cell = cells.GetValueOrDefault(entry.Path),
            LayoutError = errors.GetValueOrDefault(entry.Path),
            BoxModel = BoxModel(entry.Path)
        }).ToArray();
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
