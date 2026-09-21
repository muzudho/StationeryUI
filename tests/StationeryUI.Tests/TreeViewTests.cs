using StationeryUI.Controls;

internal static class TreeViewTests
{
    public static void Run()
    {
        var tree = new TreeView();
        var root = tree.AddNode("root", "文房具");
        var branch = tree.AddNode("branch", "筆記用具", root);
        var leaf = tree.AddNode("leaf", "鉛筆", branch);
        var other = tree.AddNode("other", "紙", root, expanded: false);
        tree.AddNode("leaf", "ノート", other);
        Check(leaf.Path == "/root/branch/leaf", "hierarchical identity");
        Check(tree.VisibleRows().Count == 4 && tree.VisibleRows()[2].Depth == 2, "expanded rows and indentation");
        tree.Select(leaf);
        tree.SetExpanded(root, false);
        Check(tree.VisibleRows().Count == 1 && tree.SelectedItem == root, "collapse hides descendants and restores selection");
        tree.Toggle(root);
        Check(branch.IsExpanded && !other.IsExpanded && tree.VisibleRows().Count == 4, "nested expansion state survives");
        tree.Right(); Check(tree.SelectedItem == branch, "right enters expanded branch");
        tree.Left(); Check(!branch.IsExpanded, "left collapses branch");
        tree.Left(); Check(tree.SelectedItem == root, "left moves to parent");
        tree.Move(int.MaxValue); Check(tree.SelectedItem == other, "end clamps");
        tree.Move(int.MaxValue); Check(tree.SelectedItem == other, "end does not overflow");
        tree.Right(); Check(other.IsExpanded, "right expands");
        tree.Right(); Check(tree.SelectedItem!.Label == "ノート", "right enters child");
        tree.Toggle(tree.SelectedItem!); Check(tree.VisibleRows().Count == 4, "leaf toggle does nothing");
        tree.Move(-int.MaxValue); Check(tree.SelectedItem == root, "home clamps");
        tree.SetExpanded(root, false); tree.Select(leaf);
        Check(root.IsExpanded && branch.IsExpanded && tree.SelectedItem == leaf, "select reveals ancestors");
        Reject(() => tree.AddNode("leaf", "duplicate", branch));
        Reject(() => tree.AddNode("bad-id", "invalid"));
        Reject(() => new TreeView().Select(leaf));
        Reject(() => new TreeView().AddNode("foreign", "invalid", root));
        new TreeView().Move(1);
        var independent = new TreeView { SelectOnInteraction = false };
        var a = independent.AddNode("a", "selected");
        var b = independent.AddNode("b", "target");
        var child = independent.AddNode("child", "child", b);
        independent.Select(a); independent.SetTarget(child);
        Check(independent.SelectedItem == a && independent.TargetItem == child, "selection and target are independent");
        independent.SetExpanded(b, false);
        Check(independent.SelectedItem == a && independent.TargetItem == b, "collapse moves only target");
        independent.Move(-1);
        Check(independent.SelectedItem == a && independent.TargetItem == a, "keyboard moves target independently");
        independent.ClearSelection(); independent.SetTarget(b);
        Check(independent.SelectedItem is null && independent.TargetItem == b, "target-only tree");
        Reject(() => independent.SetTarget(leaf));
    }

    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Reject(Action action)
    {
        try { action(); } catch (ArgumentException) { return; }
        throw new Exception("Expected invalid tree operation to be rejected.");
    }
}
