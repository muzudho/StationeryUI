namespace StationeryUI.Controls;

using StationeryUI.Inspection;

/// <summary>A tree's items and expansion state, independent of the renderer.</summary>
public sealed class TreeView
{
    private readonly List<TreeItem> roots = [];
    public IReadOnlyList<TreeItem> Roots { get; }
    public TreeItem? SelectedItem { get; private set; }
    public TreeItem? TargetItem { get; private set; }
    public bool SelectOnInteraction { get; set; } = true;
    public void ClearSelection() => SelectedItem = null;
    public void ClearTarget() => TargetItem = null;

    public void SetTarget(TreeItem item)
    {
        RequireOwned(item);
        for (var parent = item.Parent; parent is not null; parent = parent.Parent) parent.IsExpanded = true;
        TargetItem = item;
        if (SelectOnInteraction) SelectedItem = item;
    }

    public TreeView() => Roots = roots.AsReadOnly();

    public TreeItem AddNode(string id, string label, TreeItem? parent = null, bool expanded = true)
    {
        if (parent is not null) RequireOwned(parent);
        _ = new StationeryNode(id); // Use the same ID alphabet as other stationery.
        ArgumentNullException.ThrowIfNull(label);
        var siblings = parent?.Items ?? roots;
        if (siblings.Any(item => item.Id == id)) throw new ArgumentException("Duplicate sibling tree ID.", nameof(id));
        var item = new TreeItem(this, id, label, parent, expanded);
        siblings.Add(item);
        return item;
    }

    public IReadOnlyList<TreeRow> VisibleRows()
    {
        var rows = new List<TreeRow>();
        void Visit(TreeItem item, int depth)
        {
            rows.Add(new(item, depth));
            if (item.IsExpanded) foreach (var child in item.Children) Visit(child, depth + 1);
        }
        foreach (var root in roots) Visit(root, 0);
        return rows;
    }

    public void Select(TreeItem item)
    {
        RequireOwned(item);
        for (var parent = item.Parent; parent is not null; parent = parent.Parent) parent.IsExpanded = true;
        SelectedItem = item;
    }

    public void SetExpanded(TreeItem item, bool expanded)
    {
        RequireOwned(item);
        if (item.Children.Count == 0) return;
        item.IsExpanded = expanded;
        if (!expanded)
        {
            for (var current = TargetItem?.Parent; current is not null; current = current.Parent)
                if (current == item) { TargetItem = item; break; }
            if (SelectOnInteraction)
                for (var current = SelectedItem?.Parent; current is not null; current = current.Parent)
                    if (current == item) { SelectedItem = item; break; }
        }
    }

    public void Toggle(TreeItem item) => SetExpanded(item, !item.IsExpanded);

    public void Move(int delta)
    {
        var rows = VisibleRows();
        if (rows.Count == 0) return;
        var index = rows.ToList().FindIndex(row => row.Item == (TargetItem ?? SelectedItem));
        SetTarget(rows[index < 0 ? 0 : (int)Math.Clamp((long)index + delta, 0, rows.Count - 1)].Item);
    }

    public void Left()
    {
        if ((TargetItem ?? SelectedItem) is not { } item) { Move(0); return; }
        if (item.IsExpanded && item.Children.Count > 0) SetExpanded(item, false);
        else if (item.Parent is { } parent) SetTarget(parent);
    }

    public void Right()
    {
        if ((TargetItem ?? SelectedItem) is not { } item) { Move(0); return; }
        if (!item.IsExpanded) SetExpanded(item, true);
        else if (item.Children.Count > 0) SetTarget(item.Children[0]);
    }

    private void RequireOwned(TreeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Owner != this) throw new ArgumentException("Item belongs to another tree.", nameof(item));
    }
}

public sealed class TreeItem
{
    internal TreeView Owner { get; }
    internal List<TreeItem> Items { get; } = [];
    public string Id { get; }
    public string Label { get; set; }
    public TreeItem? Parent { get; }
    public string Path => (Parent?.Path ?? "") + "/" + Id;
    public IReadOnlyList<TreeItem> Children { get; }
    public bool IsExpanded { get; internal set; }
    internal TreeItem(TreeView owner, string id, string label, TreeItem? parent, bool expanded)
    {
        Owner = owner; Id = id; Label = label; Parent = parent; IsExpanded = expanded;
        Children = Items.AsReadOnly();
    }
}

public sealed record TreeRow(TreeItem Item, int Depth);
