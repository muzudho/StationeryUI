namespace StationeryUI.Editor;

/// <summary>Read-only document snapshot. A failed refresh keeps the last valid design.</summary>
internal sealed class ReadStyleSnapshot
{
    public string FilePath { get; }
    public string Text { get; private set; }
    public StyleBlueprint Blueprint { get; private set; }

    private ReadStyleSnapshot(string path, string text, StyleBlueprint blueprint)
    { FilePath = path; Text = text; Blueprint = blueprint; }

    public static ReadStyleSnapshot Open(string path)
    {
        path = Path.GetFullPath(path);
        var text = File.ReadAllText(path);
        return new(path, text, StyleBlueprint.Parse(text));
    }

    public bool Refresh()
    {
        var text = File.ReadAllText(FilePath);
        if (text == Text) return false;
        var blueprint = StyleBlueprint.Parse(text);
        Text = text;
        Blueprint = blueprint;
        return true;
    }
}
