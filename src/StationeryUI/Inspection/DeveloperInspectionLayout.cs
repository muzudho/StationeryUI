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
            .ToDictionary(group => group.Key, group => group.Select(binding => layouts[binding.Layout].Type)
                .Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var cells = settings.Bindings.SelectMany(binding => binding.Children)
            .ToDictionary(cell => cell.ModelPath, cell => new StationeryInspectionCell(cell.Column, cell.Row, cell.ColumnSpan, cell.RowSpan), StringComparer.Ordinal);
        return entries.Select(entry => entry with
        {
            LayoutTypes = types.GetValueOrDefault(entry.Path),
            Cell = cells.GetValueOrDefault(entry.Path)
        }).ToArray();
    }

    public static string FormatLabel(StationeryInspectionEntry entry)
    {
        var kind = string.IsNullOrEmpty(entry.Kind) ? "—" : char.ToUpperInvariant(entry.Kind[0]) + entry.Kind[1..];
        var layout = entry.LayoutTypes is { Count: > 0 }
            ? string.Join(", ", entry.LayoutTypes.Select(type => type switch
            {
                "grid-layout" => "gridLayout", "box-layout" => "boxLayout",
                "fullscreen-layout" => "fullscreenLayout", "work-page-layout" => "workPageLayout",
                "split-pane" => "splitPane", _ => type
            }))
            : entry.Cell is { } cell
                ? FormattableString.Invariant($"{cell.Column}, {cell.Row}, {cell.ColumnSpan}, {cell.RowSpan}") : "—";
        return $"({entry.Id} : {kind}) ({layout})" + (entry.Visible ? "" : "  （非表示）");
    }
}
