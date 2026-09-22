namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Inspection;
using StationeryUI.Theming;

public sealed partial class StationeryUiHost
{
    public Element AddTree(string id, ScreenRectangle bounds, string accessibleName, TreeView tree, StationeryNode? parent = null)
        => AddTree(AddNode(id, "tree", parent), bounds, accessibleName, tree);

    public Element AddTree(StationeryNode node, ScreenRectangle bounds, string accessibleName, TreeView tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ValidateNode(node, "tree");
        if (node.Children.Count != 0) throw new ArgumentException("Tree items are supplied by TreeView, not model children.", nameof(node));
        var element = new Element(node, bounds, accessibleName) { Tree = tree };
        Focus.Register(element.Path);
        elements.Add(element);
        return element;
    }

    private static double TreeRowHeight(StationeryTheme theme) => Math.Max(32, theme.FontSize * 1.5 + 8);

    /// <summary>Replaces a tree snapshot while keeping its element identity and scroll position.</summary>
    public void ReplaceTree(Element element, TreeView tree)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(tree);
        if (!elements.Contains(element) || element.Tree is null) throw new ArgumentException("Expected a tree element owned by this UI.", nameof(element));
        if (Focus.CapturedId == element.Path) Focus.ReleasePointer();
        element.PressedTreeItem = null;
        element.DraggingTreeScroll = false;
        element.DraggingTreeHorizontalScroll = false;
        element.Tree = tree;
        ClampTreeScroll(element);
    }

    private readonly Dictionary<(string Text, string Font, int Size), double> treeTextWidths = [];
    private TreeScrollLayout TreeScrollbars(Element element)
    {
        var theme = element.Theme ?? Theme;
        var rows = element.Tree!.VisibleRows();
        var width = 0.0;
        foreach (var row in rows)
        {
            var key = (row.Item.Label, theme.FontFamily, TextPixelSize(theme));
            if (!treeTextWidths.TryGetValue(key, out var textWidth))
            {
                if (treeTextWidths.Count >= 4096) treeTextWidths.Clear();
                treeTextWidths[key] = textWidth = Measure(row.Item.Label, theme) * Viewport.Scale;
            }
            width = Math.Max(width, theme.Padding * 2 + row.Depth * 24 + 30 + textWidth / Viewport.Scale);
        }
        return TreeScrollLayout.Create(element.Bounds, width, rows.Count * TreeRowHeight(theme),
            17 / Viewport.Scale, element.TreeHorizontalScroll, element.TreeScroll);
    }

    private (ScreenRectangle Track, ScreenRectangle Thumb, double Maximum) Scrollbar(Element element, double total, double scroll)
    {
        var maximum = Math.Max(0, total - element.Bounds.Height);
        if (maximum == 0 || element.Bounds.Height <= 0 || element.Bounds.Width <= 0) return default;
        // Style px are window pixels, including when text/UI zoom is active.
        var width = Math.Min(element.Bounds.Width, 17 / Viewport.Scale);
        var track = new ScreenRectangle(element.Bounds.X + element.Bounds.Width - width, element.Bounds.Y, width, element.Bounds.Height);
        var height = Math.Min(track.Height, Math.Max(17 / Viewport.Scale, track.Height * track.Height / total));
        var y = track.Y + (track.Height - height) * scroll / maximum;
        return (track, new(track.X, y, width, height), maximum);
    }

    private void DragTreeScroll(Element element, ScreenPoint pointer)
    {
        var bars = TreeScrollbars(element);
        var horizontal = element.DraggingTreeHorizontalScroll;
        var track = horizontal ? bars.HorizontalTrack : bars.VerticalTrack;
        var thumb = horizontal ? bars.HorizontalThumb : bars.VerticalThumb;
        var length = horizontal ? thumb.Width : thumb.Height;
        var travel = (horizontal ? track.Width : track.Height) - length;
        if (travel <= 0) { element.DraggingTreeScroll = element.DraggingTreeHorizontalScroll = false; return; }
        var position = horizontal ? pointer.X - track.X : pointer.Y - track.Y;
        var offset = Math.Clamp((position - element.TreeThumbGrab * length) / travel, 0, 1);
        if (horizontal) element.TreeHorizontalScroll = offset * bars.MaximumX;
        else element.TreeScroll = offset * bars.MaximumY;
    }

    private void ClampTreeScroll(Element element)
    {
        var bars = TreeScrollbars(element);
        element.TreeScroll = bars.OffsetY;
        element.TreeHorizontalScroll = bars.OffsetX;
    }

    private (TreeItem? Item, bool Toggle) TreeHit(Element element, ScreenPoint pointer)
    {
        ClampTreeScroll(element);
        if (!Contains(TreeScrollbars(element).Content, pointer)) return (null, false);
        var theme = element.Theme ?? Theme;
        var height = TreeRowHeight(theme);
        var rows = element.Tree!.VisibleRows();
        var index = (int)((pointer.Y - element.Bounds.Y + element.TreeScroll) / height);
        if (index < 0 || index >= rows.Count) return (null, false);
        var row = rows[index];
        var x = element.Bounds.X + theme.Padding + row.Depth * 24 - element.TreeHorizontalScroll;
        return (row.Item, row.Item.Children.Count > 0 && pointer.X >= x && pointer.X < x + 28);
    }

    private void ReleaseTree(Element element, ScreenPoint pointer, bool captured)
    {
        if (element.DraggingTreeScroll || element.DraggingTreeHorizontalScroll)
        {
            if (Focus.CapturedId == element.Path) DragTreeScroll(element, pointer);
            element.DraggingTreeScroll = false;
            element.DraggingTreeHorizontalScroll = false;
            element.PressedTreeItem = null;
            return;
        }
        var hit = TreeHit(element, pointer);
        if (captured && hit.Item is not null && hit.Item == element.PressedTreeItem &&
            hit.Toggle && element.PressedTreeToggle) element.Tree!.Toggle(hit.Item);
        element.PressedTreeItem = null;
        ClampTreeScroll(element);
    }

    private void UpdateTree(Element element, ScreenPoint pointer, bool hovered, bool pressed, int wheel,
        int horizontalWheel, bool shift, Func<Keys, bool> key, Func<Keys, bool> repeated)
    {
        var tree = element.Tree!;
        ClampTreeScroll(element);
        if (Focus.CapturedId != element.Path) element.DraggingTreeScroll = element.DraggingTreeHorizontalScroll = false;
        if (element.DraggingTreeScroll || element.DraggingTreeHorizontalScroll) { DragTreeScroll(element, pointer); return; }
        if (hovered && (wheel != 0 || horizontalWheel != 0))
        {
            var step = TreeRowHeight(element.Theme ?? Theme) * 3 / 120.0;
            if (shift) element.TreeHorizontalScroll -= wheel * step;
            else element.TreeScroll -= wheel * step;
            element.TreeHorizontalScroll += horizontalWheel * step;
            ClampTreeScroll(element);
        }
        if (hovered && pressed && Focus.CapturedId == element.Path)
        {
            var bars = TreeScrollbars(element);
            var horizontal = Contains(bars.HorizontalTrack, pointer);
            var track = horizontal ? bars.HorizontalTrack : bars.VerticalTrack;
            var thumb = horizontal ? bars.HorizontalThumb : bars.VerticalThumb;
            if (Contains(track, pointer))
            {
                element.PressedTreeItem = null;
                if (Contains(thumb, pointer))
                {
                    element.DraggingTreeScroll = !horizontal;
                    element.DraggingTreeHorizontalScroll = horizontal;
                    element.TreeThumbGrab = horizontal ? (pointer.X - thumb.X) / thumb.Width : (pointer.Y - thumb.Y) / thumb.Height;
                }
                else
                {
                    if (horizontal) element.TreeHorizontalScroll += pointer.X < thumb.X ? -bars.Content.Width : bars.Content.Width;
                    else element.TreeScroll += pointer.Y < thumb.Y ? -bars.Content.Height : bars.Content.Height;
                    ClampTreeScroll(element);
                }
                return;
            }
            var hit = TreeHit(element, pointer);
            element.PressedTreeItem = hit.Item;
            element.PressedTreeToggle = hit.Toggle;
            if (hit.Item is not null) tree.SetTarget(hit.Item);
        }
        if (Focus.FocusedId != element.Path) return;
        if (tree.TargetItem is null) tree.Move(0);
        var navigating = false;
        if (repeated(Keys.Up)) { tree.Move(-1); navigating = true; }
        if (repeated(Keys.Down)) { tree.Move(1); navigating = true; }
        if (key(Keys.Left)) { tree.Left(); navigating = true; }
        if (key(Keys.Right)) { tree.Right(); navigating = true; }
        if (key(Keys.Home)) { tree.Move(-int.MaxValue); navigating = true; }
        if (key(Keys.End)) { tree.Move(int.MaxValue); navigating = true; }
        if ((key(Keys.Enter) || key(Keys.Space)) && tree.TargetItem is { } selected)
        { tree.Toggle(selected); navigating = true; }
        if (navigating)
        {
            var index = tree.VisibleRows().ToList().FindIndex(row => row.Item == tree.TargetItem);
            var height = TreeRowHeight(element.Theme ?? Theme);
            if (index * height < element.TreeScroll) element.TreeScroll = index * height;
            var content = TreeScrollbars(element).Content;
            if ((index + 1) * height > element.TreeScroll + content.Height)
                element.TreeScroll = (index + 1) * height - content.Height;
            ClampTreeScroll(element);
        }
    }

    private void InspectTree(Element element, bool visible, List<StationeryInspectionEntry> entries)
    {
        ClampTreeScroll(element);
        var content = TreeScrollbars(element).Content;
        var height = TreeRowHeight(element.Theme ?? Theme);
        var indices = element.Tree!.VisibleRows().Select((row, index) => (row.Item, index)).ToDictionary(pair => pair.Item, pair => pair.index);
        void Visit(TreeItem item)
        {
            ScreenRectangle? bounds = null;
            if (indices.TryGetValue(item, out var index))
            {
                var top = Math.Max(element.Bounds.Y, element.Bounds.Y + index * height - element.TreeScroll);
                var bottom = Math.Min(content.Y + content.Height, element.Bounds.Y + (index + 1) * height - element.TreeScroll);
                if (bottom > top && content.Width > 0) bounds = Viewport.ToWindow(new(content.X, top, content.Width, bottom - top));
            }
            entries.Add(new(item.Id, element.Path + item.Path, element.Path + (item.Parent?.Path ?? ""), "treeNode",
                item.Label, visible && bounds is not null, bounds));
            foreach (var child in item.Children) Visit(child);
        }
        foreach (var root in element.Tree.Roots) Visit(root);
    }

    private void DrawTree(Element element, StationeryTheme theme)
    {
        ClampTreeScroll(element);
        var bars = TreeScrollbars(element);
        Fill(element.Bounds, theme.Surface);
        var rows = element.Tree!.VisibleRows();
        var height = TreeRowHeight(theme);
        var pointer = Viewport.ToLogical(new(previousMouse.X, previousMouse.Y));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var y = element.Bounds.Y + i * height - element.TreeScroll;
            if (y + height <= element.Bounds.Y || y >= element.Bounds.Y + bars.Content.Height) continue;
            var rect = new ScreenRectangle(element.Bounds.X, y, bars.Content.Width, height);
            if (row.Item == element.Tree.SelectedItem) Fill(rect, theme.Selected);
            else if (wasActive && Contains(bars.Content, pointer) && Contains(rect, pointer)) Fill(rect, theme.ButtonFill(true, false, false, true));
            if (row.Item == element.Tree.TargetItem)
            {
                var color = theme.TreeTarget;
                Fill(new(rect.X, rect.Y, rect.Width, 2), color);
                Fill(new(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), color);
                Fill(new(rect.X, rect.Y, 2, rect.Height), color);
                Fill(new(rect.X + rect.Width - 2, rect.Y, 2, rect.Height), color);
            }
            var x = element.Bounds.X + theme.Padding + row.Depth * 24 - element.TreeHorizontalScroll;
            var textColor = row.Item.IsDimmed ? theme.DisabledText : theme.Text;
            if (row.Item.Children.Count > 0)
            {
                // Draw the box and +/- as geometry: the affordance does not depend on font glyphs.
                var box = new ScreenRectangle(x + 2, y + (height - 20) / 2, 20, 20);
                Fill(box, theme.Border);
                Fill(new(box.X + 1, box.Y + 1, 18, 18), theme.Surface);
                Fill(new(box.X + 5, box.Y + 9, 10, 2), textColor);
                if (!row.Item.IsExpanded) Fill(new(box.X + 9, box.Y + 5, 2, 10), textColor);
            }
            DrawText(row.Item.Label, x + 30, y + 4, theme, textColor);
        }
        if (bars.MaximumY > 0)
        {
            Fill(bars.VerticalTrack, theme.Background);
            Fill(bars.VerticalThumb, element.DraggingTreeScroll || wasActive && Contains(bars.VerticalThumb, pointer) ? theme.Accent : theme.Border);
        }
        if (bars.MaximumX > 0)
        {
            Fill(bars.HorizontalTrack, theme.Background);
            Fill(bars.HorizontalThumb, element.DraggingTreeHorizontalScroll || wasActive && Contains(bars.HorizontalThumb, pointer) ? theme.Accent : theme.Border);
        }
        if (bars.MaximumX > 0 && bars.MaximumY > 0)
            Fill(new(bars.VerticalTrack.X, bars.HorizontalTrack.Y, bars.VerticalTrack.Width, bars.HorizontalTrack.Height), theme.Background);
        Fill(new(element.Bounds.X, element.Bounds.Y + element.Bounds.Height - theme.BorderWidth, element.Bounds.Width, theme.BorderWidth),
            Focus.FocusedId == element.Path ? theme.Accent : theme.Border);
    }
}
