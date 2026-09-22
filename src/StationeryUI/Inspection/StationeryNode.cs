namespace StationeryUI.Inspection;

using StationeryUI.Canvas;

/// <summary>A stable, code-assigned identity. IDs are unique among siblings; paths are unique in a tree.</summary>
public sealed class StationeryNode
{
    private readonly List<StationeryNode> children = [];
    public string Id { get; }
    public string Kind { get; }
    public StationeryNode? Parent { get; }
    public IReadOnlyList<StationeryNode> Children { get; }
    public string Path { get; }

    public StationeryNode(string id, string kind = "viewport") : this(id, kind, null) { }

    private StationeryNode(string id, string kind, StationeryNode? parent)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (id.Any(c => !(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')))
            throw new ArgumentException("Stationery IDs may contain only ASCII letters, digits and underscores.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        Id = id;
        Kind = kind;
        Parent = parent;
        Path = (parent?.Path ?? "") + "/" + id;
        Children = children.AsReadOnly();
    }

    public StationeryNode AddChild(string id, string kind = "container")
    {
        if (children.Any(child => child.Id == id))
            throw new ArgumentException($"Duplicate stationery ID '{id}' under '{Path}'.", nameof(id));
        var child = new StationeryNode(id, kind, this);
        children.Add(child);
        return child;
    }

    /// <summary>Resolves an absolute, case-sensitive path from the outermost node. No wildcards or short-ID search.</summary>
    public StationeryNode? Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var root = this;
        while (root.Parent is not null) root = root.Parent;
        var segments = path.Split('/');
        if (segments.Length < 2 || segments[0] != "" || segments[1] != root.Id) return null;
        var current = root;
        foreach (var segment in segments.Skip(2))
        {
            current = current.children.FirstOrDefault(child => child.Id == segment);
            if (current is null) return null;
        }
        return current;
    }

    public bool IsWithin(StationeryNode ancestor)
    {
        for (var node = this; node is not null; node = node.Parent)
            if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }
}

/// <summary>An immutable copy of game-thread state for the developer window.</summary>
public sealed record StationeryInspectionEntry(string Id, string Path, string? ParentPath, string Kind,
    string Label, bool Visible, ScreenRectangle? WindowBounds)
{
    public IReadOnlyList<string>? LayoutTypes { get; init; }
    public StationeryInspectionCell? Cell { get; init; }
    public string? LayoutError { get; init; }
}

public sealed record StationeryInspectionCell(int Column, int Row, int ColumnSpan, int RowSpan);
