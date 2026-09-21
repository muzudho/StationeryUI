namespace StationeryUI.Styling;

using System.Text.Json;

/// <summary>Loads styles on the calling thread. Call Update from the game loop; no background callbacks are used.</summary>
public sealed class StationeryStyleFile
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private TimeSpan elapsed;
    private string? acceptedText;
    private string? pendingText;

    public string FilePath { get; }
    public StationeryStyleSettings Current { get; private set; } = StationeryStyleSettings.Default;
    public string? LastError { get; private set; }

    public StationeryStyleFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        Reload();
    }

    /// <summary>Reads immediately, even with auto reload disabled. Failure preserves the last good settings.</summary>
    public bool Reload() => TryLoad(requireStableText: false);

    /// <summary>Polls at most twice per second, accepting a change after two identical reads.</summary>
    public bool Update(TimeSpan delta)
    {
        if (!Current.AutoReload) return false;
        elapsed += delta;
        if (elapsed < PollInterval) return false;
        elapsed = TimeSpan.Zero;
        return TryLoad(requireStableText: true);
    }

    private bool TryLoad(bool requireStableText)
    {
        try
        {
            var text = File.ReadAllText(FilePath);
            if (text == acceptedText)
            {
                pendingText = null;
                LastError = null;
                return false;
            }
            if (requireStableText && text != pendingText)
            {
                pendingText = text;
                return false;
            }
            var next = StationeryStyleSettings.Parse(text);
            Current = next;
            acceptedText = text;
            pendingText = null;
            LastError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            LastError = $"{FilePath}: {ex.Message}";
            pendingText = null;
            return false;
        }
    }
}
