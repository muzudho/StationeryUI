namespace StationeryUI.Inspection;

using StationeryUI.Canvas;
using StationeryUI.Styling;

/// <summary>Internal partitions in window pixels, excluding padding and merged-cell interiors.</summary>
public static class DeveloperInspectionPartitions
{
    public static IReadOnlyList<StationeryInspectionLine> Create(StationeryLayoutNode layout, ScreenRectangle content)
    {
        var lines = new List<StationeryInspectionLine>();
        if (content.Width <= 0 || content.Height <= 0) return lines;
        if (layout.Type == "grid-layout")
        {
            var cells = StationeryLayoutEngine.ArrangeGridCells(layout, content);
            var merged = layout.Cells.Select(c => (c.Row, c.Column, c.RowSpan, c.ColumnSpan))
                .Concat(layout.Children.Select(c => (c.Row, c.Column, c.RowSpan, c.ColumnSpan)))
                .Where(c => c.RowSpan > 1 || c.ColumnSpan > 1)
                .Select(c => {
                    var first = cells.First(x => x.Row == c.Row && x.Column == c.Column).Bounds;
                    var last = cells.First(x => x.Row == c.Row + c.RowSpan - 1 && x.Column == c.Column + c.ColumnSpan - 1).Bounds;
                    return new ScreenRectangle(first.X, first.Y, last.X + last.Width - first.X, last.Y + last.Height - first.Y);
                }).ToArray();
            void Add(bool vertical, double coordinate)
            {
                var min = vertical ? content.Y : content.X;
                var max = min + (vertical ? content.Height : content.Width);
                var intervals = new List<(double Start, double End)> { (min, max) };
                foreach (var box in merged)
                {
                    var across = vertical ? box.X : box.Y;
                    if (coordinate <= across || coordinate >= across + (vertical ? box.Width : box.Height)) continue;
                    var start = vertical ? box.Y : box.X;
                    var end = start + (vertical ? box.Height : box.Width);
                    intervals = intervals.SelectMany(i => new[] { (i.Start, Math.Min(i.End, start)), (Math.Max(i.Start, end), i.End) })
                        .Where(i => i.Item2 > i.Item1).ToList();
                }
                foreach (var (start, end) in intervals)
                    lines.Add(vertical ? new(new(coordinate, start), new(coordinate, end)) : new(new(start, coordinate), new(end, coordinate)));
            }
            foreach (var x in cells.Where(c => c.Row == 0 && c.Column > 0).Select(c => c.Bounds.X).Distinct())
                if (x > content.X && x < content.X + content.Width) Add(true, x);
            foreach (var y in cells.Where(c => c.Column == 0 && c.Row > 0).Select(c => c.Bounds.Y).Distinct())
                if (y > content.Y && y < content.Y + content.Height) Add(false, y);
        }
        else if (layout.Type == "dock-layout" && layout.CellError is null)
        {
            var children = layout.Cells.Select((c, i) => new StationeryDockBinding(i.ToString(System.Globalization.CultureInfo.InvariantCulture), c.Dock!, c.Size)).ToArray();
            var areas = StationeryDockLayout.Arrange(content, children);
            foreach (var child in children.Where(c => c.Dock != "center"))
            {
                var area = areas[child.ModelPath];
                if (area.Width <= 0 || area.Height <= 0) continue;
                var vertical = child.Dock is "left" or "right";
                var coordinate = child.Dock switch {
                    "left" => area.X + area.Width, "right" => area.X,
                    "top" => area.Y + area.Height, _ => area.Y };
                if (vertical && coordinate > content.X && coordinate < content.X + content.Width)
                    lines.Add(new(new(coordinate, area.Y), new(coordinate, area.Y + area.Height)));
                if (!vertical && coordinate > content.Y && coordinate < content.Y + content.Height)
                    lines.Add(new(new(area.X, coordinate), new(area.X + area.Width, coordinate)));
            }
        }
        return lines.Distinct().ToArray();
    }
}

public sealed record StationeryInspectionLine(ScreenPoint Start, ScreenPoint End);
