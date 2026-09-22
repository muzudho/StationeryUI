namespace StationeryUI.Inspection;

using StationeryUI.Controls;
using System.Globalization;

public enum DeveloperTreeMode { Model, Layout }

public sealed record DeveloperViewState(string? SelectedPath, string[] CollapsedPaths, double SplitRatio = .4, bool Visible = true, bool CaptureEnabled = false)
{
    public DeveloperTreeMode TreeMode { get; init; }
    public DeveloperViewState? OtherTreeState { get; init; }
}
public sealed record DeveloperInspectionMessage(StationeryInspectionEntry[] Entries, long ShowSequence, DeveloperViewState? RestoreState, string? CapturePath = null, long CaptureSequence = 0);

/// <summary>Read-only inspector model. Refreshes snapshots without losing tree expansion or selection.</summary>
public sealed class DeveloperInspectionModel
{
    private IReadOnlyList<StationeryInspectionEntry> source = [];
    private DeveloperViewState? otherTreeState;
    private Dictionary<string, string> mergedLayoutPaths = new(StringComparer.Ordinal);
    public DeveloperTreeMode TreeMode { get; private set; }
    public void SetTreeMode(DeveloperTreeMode mode)
    {
        if (TreeMode == mode) return;
        var previous = Capture() with { OtherTreeState = null };
        var restore = otherTreeState;
        TreeMode = mode;
        otherTreeState = previous;
        Refresh(source);
        foreach (var item in items.Values) Tree.SetExpanded(item, true);
        var selection = restore?.SelectedPath is not null ? restore : previous;
        if (mode == DeveloperTreeMode.Model && selection.SelectedPath is { } layoutPath && layoutPath.IndexOf(':') is var separator && separator >= 0)
            selection = selection with { SelectedPath = layoutPath[..separator] };
        RestoreSelection(selection);
    }

    private Dictionary<string, StationeryInspectionEntry> entries = new(StringComparer.Ordinal);
    private Dictionary<string, TreeItem> items = new(StringComparer.Ordinal);
    private Dictionary<TreeItem, string> paths = [];
    public TreeView Tree { get; private set; } = new();
    public string? SelectedPath => Tree.SelectedItem is { } item ? paths.GetValueOrDefault(item) : null;
    public StationeryInspectionEntry? SelectedEntry => SelectedPath is { } path ? entries.GetValueOrDefault(path) : null;
    public string? PathFor(TreeItem item) => paths.GetValueOrDefault(item);

    public void Refresh(IReadOnlyList<StationeryInspectionEntry> snapshot)
    {
        source = snapshot;
        mergedLayoutPaths = new(StringComparer.Ordinal);
        if (TreeMode == DeveloperTreeMode.Layout)
        {
            // A model owns at most one root layout. Keep its model identity and move
            // the root layout's children directly beneath that model.
            foreach (var owner in snapshot)
            {
                var roots = (owner.LayoutNodes ?? []).Where(layout => layout.ParentPath == owner.Path).ToArray();
                if (roots.Length == 1) mergedLayoutPaths.Add(roots[0].Path, owner.Path);
            }
            snapshot = snapshot.Select(entry => entry with { ParentPath = ResolveMergedPath(entry.LayoutParentPath ?? entry.ParentPath) })
                .Concat(snapshot.SelectMany(entry => entry.LayoutNodes ?? [])
                    .Where(entry => !mergedLayoutPaths.ContainsKey(entry.Path))
                    .Select(entry => entry with { ParentPath = ResolveMergedPath(entry.ParentPath) })).ToArray();
        }
        var next = snapshot.ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        var changed = entries.Count != next.Count || next.Any(pair => !entries.TryGetValue(pair.Key, out var old) || old.ParentPath != pair.Value.ParentPath);
        var state = Capture();
        entries = next;
        if (changed)
        {
            Tree = new(); items = new(StringComparer.Ordinal); paths = [];
            var pending = snapshot.ToList();
            while (pending.Count > 0)
            {
                var progress = false;
                foreach (var entry in pending.ToArray())
                {
                    TreeItem? parent = null;
                    if (entry.ParentPath is { } parentPath && entries.ContainsKey(parentPath) && !items.TryGetValue(parentPath, out parent)) continue;
                    // The full source path is the identity; generated IDs also allow partial snapshots with duplicate root IDs.
                    var item = Tree.AddNode("node" + items.Count.ToString(CultureInfo.InvariantCulture), "", parent);
                    items.Add(entry.Path, item); paths.Add(item, entry.Path); pending.Remove(entry); progress = true;
                }
                if (!progress) throw new ArgumentException("Inspection hierarchy contains a cycle.", nameof(snapshot));
            }
            RestoreSelection(state);
        }
        foreach (var (path, entry) in entries)
            items[path].Label = TreeMode == DeveloperTreeMode.Layout
                ? DeveloperInspectionLayout.FormatLabel(entry) : DeveloperInspectionLayout.FormatModelLabel(entry);
    }

    private string? ResolveMergedPath(string? path) => path is null ? null : mergedLayoutPaths.GetValueOrDefault(path, path);

    public bool Select(string path)
    {
        if (!items.TryGetValue(ResolveMergedPath(path)!, out var item)) return false;
        Tree.Select(item); Tree.SetTarget(item); return true;
    }
    public DeveloperViewState Capture(double splitRatio = .4, bool visible = true)
        => new(SelectedPath, items.Where(pair => pair.Value.Children.Count > 0 && !pair.Value.IsExpanded).Select(pair => pair.Key).ToArray(), splitRatio, visible) { TreeMode = TreeMode, OtherTreeState = otherTreeState };
    public void Restore(DeveloperViewState? state)
    {
        if (state is not null) SetTreeMode(state.TreeMode);
        otherTreeState = state?.OtherTreeState;
        RestoreSelection(state);
    }
    private void RestoreSelection(DeveloperViewState? state)
    {
        if (state?.SelectedPath is not { } selected || !Select(selected)) Tree.Move(0);
        foreach (var path in state?.CollapsedPaths ?? [])
            if (items.TryGetValue(ResolveMergedPath(path)!, out var item)) Tree.SetExpanded(item, false);
    }
    public string? IdPath => SelectedPath?.TrimStart('/').Replace('/', '.');

    public string Details
    {
        get
        {
            if (SelectedEntry is not { } entry) return "文房具を選択してください。";
            var bounds = entry.WindowBounds is { } b
                ? string.Create(CultureInfo.InvariantCulture, $"X={b.X:0.##}\nY={b.Y:0.##}\n幅={b.Width:0.##}\n高さ={b.Height:0.##}") : "—";
            if (entry.Kind == "layout" && entry.Path.IndexOf(':') is var separator && separator >= 0)
                return $"レイアウト Id: {entry.Id}\n\nレイアウトパス: {entry.Label}\n\n所有モデル: {entry.Path[..separator]}\n\n識別パス: {entry.Path}\n\n種類: {string.Join(", ", entry.LayoutTypes ?? [])}\n\nウィンドウ内の位置（px）:\n{bounds}";
            return $"文房具 Id: {entry.Id}\n\nId path: {IdPath}\n\n完全パス: {entry.Path}\n\n種類: {entry.Kind}\n名前: {entry.Label}\n表示: {(entry.Visible ? "表示中" : "非表示")}\n\nウィンドウ内の位置（px）:\n{bounds}"
                + (entry.LayoutNodes?.SingleOrDefault(layout => layout.ParentPath == entry.Path) is { } rootLayout
                    ? $"\n\n所有レイアウト: {rootLayout.Label}" : "")
                + (entry.LayoutError is null ? "" : "\n\nレイアウトエラー:\n" + entry.LayoutError);
        }
    }
}
