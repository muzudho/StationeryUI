namespace StationeryUI.Styling;

using System.Text.Json;

/// <summary>Always watches loading configuration; watches visual styles only when enabled.
/// Call Update from the game loop. All reads and state changes occur on the calling thread.</summary>
public sealed class StationeryStyleFile
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private TimeSpan elapsed;
    private string? acceptedConfigurationText;
    private string? pendingConfigurationText;
    private string? acceptedStyleText;
    private string? pendingStyleText;
    private string? configurationError;
    private string? styleError;
    private bool needsInitialStyle = true;
    private readonly Action<StationeryStyleSettings>? validate;

    public string ConfigurationFilePath { get; }
    public string FilePath { get; private set; }
    public StationeryStyleConfiguration Configuration { get; private set; } = StationeryStyleConfiguration.Default;
    public StationeryStyleSettings Current { get; private set; } = StationeryStyleSettings.Default;
    public string? LastError => configurationError ?? styleError;

    public StationeryStyleFile(string configurationFilePath, StationeryStyleSettings? fallback = null,
        Action<StationeryStyleSettings>? validate = null)
    {
        this.validate = validate;
        Current = fallback ?? StationeryStyleSettings.Default;
        validate?.Invoke(Current);
        ConfigurationFilePath = Path.GetFullPath(configurationFilePath);
        FilePath = ResolveStylePath(Configuration.StyleFile);
        Reload();
    }

    /// <summary>Reads both files immediately, even with style auto reload disabled.</summary>
    public bool Reload()
    {
        ReadConfiguration(requireStableText: false);
        return ReadStyle(requireStableText: false);
    }

    /// <summary>Configuration is checked even when style auto reload is off.</summary>
    public bool Update(TimeSpan delta)
    {
        elapsed += delta;
        if (elapsed < PollInterval) return false;
        elapsed = TimeSpan.Zero;
        ReadConfiguration(requireStableText: true);
        return (Configuration.AutoReload || needsInitialStyle) && ReadStyle(requireStableText: true);
    }

    private string ResolveStylePath(string path) => Path.GetFullPath(path, Path.GetDirectoryName(ConfigurationFilePath)!);

    private void ReadConfiguration(bool requireStableText)
    {
        try
        {
            var text = File.ReadAllText(ConfigurationFilePath);
            if (text == acceptedConfigurationText)
            {
                pendingConfigurationText = null;
                configurationError = null;
                return;
            }
            if (requireStableText && text != pendingConfigurationText)
            {
                pendingConfigurationText = text;
                return;
            }
            var next = StationeryStyleConfiguration.Parse(text);
            var path = ResolveStylePath(next.StyleFile);
            if (path != FilePath)
            {
                FilePath = path;
                acceptedStyleText = null;
                needsInitialStyle = true;
            }
            // Never reuse a pre-disable snapshot when resuming or changing paths.
            pendingStyleText = null;
            Configuration = next;
            acceptedConfigurationText = text;
            pendingConfigurationText = null;
            configurationError = null;
        }
        catch (Exception ex) when (IsReadError(ex))
        {
            configurationError = $"{ConfigurationFilePath}: {ex.Message}";
            pendingConfigurationText = null;
        }
    }

    private bool ReadStyle(bool requireStableText)
    {
        try
        {
            var text = File.ReadAllText(FilePath);
            if (text == acceptedStyleText)
            {
                pendingStyleText = null;
                styleError = null;
                needsInitialStyle = false;
                return false;
            }
            if (requireStableText && text != pendingStyleText)
            {
                pendingStyleText = text;
                return false;
            }
            var next = StationeryStyleSettings.Parse(text);
            validate?.Invoke(next);
            Current = next;
            acceptedStyleText = text;
            pendingStyleText = null;
            styleError = null;
            needsInitialStyle = false;
            return true;
        }
        catch (Exception ex) when (IsReadError(ex))
        {
            styleError = $"{FilePath}: {ex.Message}";
            pendingStyleText = null;
            return false;
        }
    }

    private static bool IsReadError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException;
}
