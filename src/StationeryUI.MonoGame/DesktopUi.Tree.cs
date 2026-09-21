namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Inspection;
using StationeryUI.Theming;

public sealed partial class DesktopUi
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

    private void ClampTreeScroll(Element element)
    {
        var height = TreeRowHeight(element.Theme ?? Theme);
        element.TreeScroll = Math.Clamp(element.TreeScroll, 0,
            Math.Max(0, element.Tree!.VisibleRows().Count * height - element.Bounds.Height));
    }

    private (TreeItem? Item, bool Toggle) TreeHit(Element element, ScreenPoint pointer)
    {
        ClampTreeScroll(element);
        if (!Contains(element.Bounds, pointer)) return (null, false);
        var theme = element.Theme ?? Theme;
        var height = TreeRowHeight(theme);
        var rows = element.Tree!.VisibleRows();
        var index = (int)((pointer.Y - element.Bounds.Y + element.TreeScroll) / height);
        if (index < 0 || index >= rows.Count) return (null, false);
        var row = rows[index];
        var x = element.Bounds.X + theme.Padding + row.Depth * 24;
        return (row.Item, row.Item.Children.Count > 0 && pointer.X >= x && pointer.X < x + 28);
    }

    private void ReleaseTree(Element element, ScreenPoint pointer, bool captured)
    {
        var hit = TreeHit(element, pointer);
        if (captured && hit.Item is not null && hit.Item == element.PressedTreeItem &&
            hit.Toggle && element.PressedTreeToggle) element.Tree!.Toggle(hit.Item);
        element.PressedTreeItem = null;
        ClampTreeScroll(element);
    }

    private void UpdateTree(Element element, ScreenPoint pointer, bool hovered, bool pressed, int wheel,
        Func<Keys, bool> key, Func<Keys, bool> repeated)
    {
        var tree = element.Tree!;
        ClampTreeScroll(element);
        if (hovered && wheel != 0)
        {
            element.TreeScroll -= wheel / 120.0 * TreeRowHeight(element.Theme ?? Theme) * 3;
            ClampTreeScroll(element);
        }
        if (hovered && pressed && Focus.CapturedId == element.Path)
        {
            var hit = TreeHit(element, pointer);
            element.PressedTreeItem = hit.Item;
            element.PressedTreeToggle = hit.Toggle;
            if (hit.Item is not null) tree.Select(hit.Item);
        }
        if (Focus.FocusedId != element.Path) return;
        if (tree.SelectedItem is null) tree.Move(0);
        var navigating = false;
        if (repeated(Keys.Up)) { tree.Move(-1); navigating = true; }
        if (repeated(Keys.Down)) { tree.Move(1); navigating = true; }
        if (key(Keys.Left)) { tree.Left(); navigating = true; }
        if (key(Keys.Right)) { tree.Right(); navigating = true; }
        if (key(Keys.Home)) { tree.Move(-int.MaxValue); navigating = true; }
        if (key(Keys.End)) { tree.Move(int.MaxValue); navigating = true; }
        if ((key(Keys.Enter) || key(Keys.Space)) && tree.SelectedItem is { } selected)
        { tree.Toggle(selected); navigating = true; }
        if (navigating)
        {
            var index = tree.VisibleRows().ToList().FindIndex(row => row.Item == tree.SelectedItem);
            var height = TreeRowHeight(element.Theme ?? Theme);
            if (index * height < element.TreeScroll) element.TreeScroll = index * height;
            if ((index + 1) * height > element.TreeScroll + element.Bounds.Height)
                element.TreeScroll = (index + 1) * height - element.Bounds.Height;
            ClampTreeScroll(element);
        }
    }

    private void InspectTree(Element element, bool visible, List<StationeryInspectionEntry> entries)
    {
        ClampTreeScroll(element);
        var height = TreeRowHeight(element.Theme ?? Theme);
        var indices = element.Tree!.VisibleRows().Select((row, index) => (row.Item, index)).ToDictionary(pair => pair.Item, pair => pair.index);
        void Visit(TreeItem item)
        {
            ScreenRectangle? bounds = null;
            if (indices.TryGetValue(item, out var index))
            {
                var top = Math.Max(element.Bounds.Y, element.Bounds.Y + index * height - element.TreeScroll);
                var bottom = Math.Min(element.Bounds.Y + element.Bounds.Height, element.Bounds.Y + (index + 1) * height - element.TreeScroll);
                if (bottom > top && element.Bounds.Width > 0) bounds = Viewport.ToWindow(new(element.Bounds.X, top, element.Bounds.Width, bottom - top));
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
        Fill(element.Bounds, theme.Surface);
        var rows = element.Tree!.VisibleRows();
        var height = TreeRowHeight(theme);
        var pointer = Viewport.ToLogical(new(previousMouse.X, previousMouse.Y));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var y = element.Bounds.Y + i * height - element.TreeScroll;
            if (y + height <= element.Bounds.Y || y >= element.Bounds.Y + element.Bounds.Height) continue;
            var rect = new ScreenRectangle(element.Bounds.X, y, element.Bounds.Width, height);
            if (row.Item == element.Tree.SelectedItem) Fill(rect, theme.Selection);
            else if (Contains(element.Bounds, pointer) && Contains(rect, pointer)) Fill(rect, theme.ButtonFill(true, false, false, true));
            var x = element.Bounds.X + theme.Padding + row.Depth * 24;
            if (row.Item.Children.Count > 0)
            {
                // Draw the box and +/- as geometry: the affordance does not depend on font glyphs.
                var box = new ScreenRectangle(x + 2, y + (height - 20) / 2, 20, 20);
                Fill(box, theme.Border);
                Fill(new(box.X + 1, box.Y + 1, 18, 18), theme.Surface);
                Fill(new(box.X + 5, box.Y + 9, 10, 2), theme.Text);
                if (!row.Item.IsExpanded) Fill(new(box.X + 9, box.Y + 5, 2, 10), theme.Text);
            }
            DrawText(row.Item.Label, x + 30, y + 4, theme, theme.Text);
        }
        var total = rows.Count * height;
        if (total > element.Bounds.Height)
        {
            var thumb = Math.Max(8, element.Bounds.Height * element.Bounds.Height / total);
            thumb = Math.Min(thumb, element.Bounds.Height);
            var y = element.Bounds.Y + (element.Bounds.Height - thumb) * element.TreeScroll / (total - element.Bounds.Height);
            Fill(new(element.Bounds.X + element.Bounds.Width - 4, y, 3, thumb), theme.Border);
        }
        Fill(new(element.Bounds.X, element.Bounds.Y + element.Bounds.Height - theme.BorderWidth, element.Bounds.Width, theme.BorderWidth),
            Focus.FocusedId == element.Path ? theme.Accent : theme.Border);
    }
}
