namespace StationeryUI.Inspection;

using StationeryUI.Controls;
using System.Globalization;

public sealed record DeveloperViewState(string? SelectedPath, string[] CollapsedPaths, double SplitRatio = .4, bool Visible = true, bool CaptureEnabled = false);
public sealed record DeveloperInspectionMessage(StationeryInspectionEntry[] Entries, long ShowSequence, DeveloperViewState? RestoreState, string? CapturePath = null, long CaptureSequence = 0);

/// <summary>Read-only inspector model. Refreshes snapshots without losing tree expansion or selection.</summary>
public sealed class DeveloperInspectionModel
{
    private Dictionary<string, StationeryInspectionEntry> entries = new(StringComparer.Ordinal);
    private Dictionary<string, TreeItem> items = new(StringComparer.Ordinal);
    private Dictionary<TreeItem, string> paths = [];
    public TreeView Tree { get; private set; } = new();
    public string? SelectedPath => Tree.SelectedItem is { } item ? paths.GetValueOrDefault(item) : null;
    public StationeryInspectionEntry? SelectedEntry => SelectedPath is { } path ? entries.GetValueOrDefault(path) : null;
    public string? PathFor(TreeItem item) => paths.GetValueOrDefault(item);

    public void Refresh(IReadOnlyList<StationeryInspectionEntry> snapshot)
    {
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
            Restore(state);
        }
        foreach (var (path, entry) in entries)
            items[path].Label = DeveloperInspectionLayout.FormatLabel(entry);
    }

    public bool Select(string path)
    {
        if (!items.TryGetValue(path, out var item)) return false;
        Tree.Select(item); Tree.SetTarget(item); return true;
    }
    public DeveloperViewState Capture(double splitRatio = .4, bool visible = true)
        => new(SelectedPath, items.Where(pair => pair.Value.Children.Count > 0 && !pair.Value.IsExpanded).Select(pair => pair.Key).ToArray(), splitRatio, visible);
    public void Restore(DeveloperViewState? state)
    {
        if (state?.SelectedPath is not { } selected || !Select(selected)) Tree.Move(0);
        foreach (var path in state?.CollapsedPaths ?? [])
            if (items.TryGetValue(path, out var item)) Tree.SetExpanded(item, false);
    }
    public string? IdPath => SelectedPath?.TrimStart('/').Replace('/', '.');

    public string Details
    {
        get
        {
            if (SelectedEntry is not { } entry) return "文房具を選択してください。";
            var bounds = entry.WindowBounds is { } b
                ? string.Create(CultureInfo.InvariantCulture, $"X={b.X:0.##}\nY={b.Y:0.##}\n幅={b.Width:0.##}\n高さ={b.Height:0.##}") : "—";
            return $"文房具 Id: {entry.Id}\n\nId path: {IdPath}\n\n完全パス: {entry.Path}\n\n種類: {entry.Kind}\n名前: {entry.Label}\n表示: {(entry.Visible ? "表示中" : "非表示")}\n\nウィンドウ内の位置（px）:\n{bounds}";
        }
    }
}
