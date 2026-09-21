namespace StationeryUI.MonoGame;

using System.Reflection;
using System.Text.Json;
using StationeryUI.Controls;
using StationeryUI.Styling;

/// <summary>Developer-window startup settings, backed by a resource shipped inside the library.</summary>
public sealed class StationeryDeveloperStyle
{
    public StationeryStyleSettings Settings { get; }
    public int Width { get; }
    public int Height { get; }
    public SplitPaneOptions SplitOptions { get; }
    public string? FilePath { get; private set; }
    public string? LastError { get; private set; }

    private StationeryDeveloperStyle(StationeryStyleSettings settings, int width, int height)
    {
        Settings = settings; Width = width; Height = height;
        var root = settings.Models[0].CreateTree();
        foreach (var (path, kind) in new[] {
            ("", "page"), ("/instructions", "textBlock"), ("/inspectorSplit", "splitPane"),
            ("/inspectorPanel", "container"), ("/inspectorPanel/toolHint", "textBlock"),
            ("/inspectorSplit/stationeryTree", "tree"), ("/inspectorSplit/details", "textBlock"), ("/copyPath", "button") })
            if (root.Resolve("/developerViewport/developerWindow" + path)?.Kind != kind)
                throw new JsonException($"Required developer model: /developerViewport/developerWindow{path} ({kind}).");
        var binding = settings.Bindings.SingleOrDefault(b => b.ModelPath == "/developerViewport/developerWindow/inspectorSplit" &&
            settings.Layouts.Any(l => l.Id == b.Layout && l.Type == "split-pane"));
        if (binding is null || binding.FirstModel != "/developerViewport/developerWindow/inspectorSplit/stationeryTree" ||
            binding.SecondModel != "/developerViewport/developerWindow/inspectorSplit/details")
            throw new JsonException("The inspector split must bind stationeryTree first and details second.");
        SplitOptions = settings.Layouts.Single(l => l.Id == binding.Layout).Split!;
        if (!settings.Bindings.Any(b => b.ModelPath == "/developerViewport/developerWindow" &&
            b.InspectorModel == "/developerViewport/developerWindow/inspectorPanel" &&
            settings.Layouts.Any(l => l.Id == b.Layout && l.Type == "work-page-layout")))
            throw new JsonException("The developer page requires an inspectorPanel work-page binding.");
        foreach (var path in new[] { "/developerViewport/developerWindow/instructions", "/developerViewport/developerWindow/inspectorSplit", "/developerViewport/developerWindow/copyPath", "/developerViewport/developerWindow/inspectorPanel/toolHint" })
            if (!settings.Bindings.Any(b => b.Children.Any(c => c.ModelPath == path)))
                throw new JsonException($"Required floating-layout placement: {path}.");
    }

    public static StationeryDeveloperStyle Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("window", out var window) || window.ValueKind != JsonValueKind.Object)
            throw new JsonException("window must contain width and height in pixels.");
        int Dimension(string name)
        {
            if (!window.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt32(out var pixels) || pixels < 1 || pixels > 16384)
                throw new JsonException($"window.{name} must be an integer from 1 to 16384.");
            return pixels;
        }
        return new(StationeryStyleSettings.Parse(json), Dimension("width"), Dimension("height"));
    }

    /// <summary>Load at window creation. Invalid external settings fall back to the embedded defaults.</summary>
    public static StationeryDeveloperStyle Load(string? filePath = null)
    {
        var assembly = typeof(StationeryDeveloperStyle).Assembly;
        using var stream = assembly.GetManifestResourceStream("StationeryUI.dev-window.stationery-style.json")
            ?? throw new InvalidOperationException("Missing embedded developer-window style.");
        using var reader = new StreamReader(stream);
        var fallback = Parse(reader.ReadToEnd());
        filePath ??= Environment.GetEnvironmentVariable("STATIONERYUI_DEV_WINDOW_STYLE_PATH");
        if (string.IsNullOrWhiteSpace(filePath))
            filePath = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "StationeryDeveloperStyleSource")?.Value
                ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "dev-window.stationery-style.json");
        try
        {
            fallback.FilePath = Path.GetFullPath(filePath);
            if (!File.Exists(fallback.FilePath)) return fallback;
            var loaded = Parse(File.ReadAllText(fallback.FilePath));
            loaded.FilePath = fallback.FilePath;
            return loaded;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            fallback.LastError = $"{filePath}: {ex.Message}";
            System.Diagnostics.Trace.WriteLine(fallback.LastError);
            return fallback;
        }
    }
}
