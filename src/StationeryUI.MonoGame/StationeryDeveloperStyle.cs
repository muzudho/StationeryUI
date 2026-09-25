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
        if (!settings.BindingsV2.TryGetValue("ctrlInspectorSplit", out var splitBinding) ||
            splitBinding.ModelPath != "/developerViewport/developerWindow/inspectorSplit" ||
            splitBinding.LayoutPath is not { } splitRoute)
            throw new JsonException("bindingsV2 must bind ctrlInspectorSplit to the inspectorSplit model.");
        var splitLayout = settings.Layouts.SingleOrDefault(layout => layout.Type == "split-pane" &&
            splitRoute.Contains(":" + layout.Id, StringComparison.Ordinal));
        if (splitLayout?.Split is not { } splitOptions ||
            !settings.BindingsV2.Values.Any(binding => binding.ModelPath == "/developerViewport/developerWindow/inspectorSplit/stationeryTree" &&
                binding.LayoutPath?.EndsWith("/first", StringComparison.Ordinal) == true) ||
            !settings.BindingsV2.Values.Any(binding => binding.ModelPath == "/developerViewport/developerWindow/inspectorSplit/details" &&
                binding.LayoutPath?.EndsWith("/second", StringComparison.Ordinal) == true))
            throw new JsonException("The inspector split must bind stationeryTree first and details second in bindingsV2.");
        SplitOptions = splitOptions;
        foreach (var path in new[] { "/developerViewport/developerWindow/inspectorPanel", "/developerViewport/developerWindow/instructions", "/developerViewport/developerWindow/inspectorSplit", "/developerViewport/developerWindow/copyPath", "/developerViewport/developerWindow/inspectorPanel/toolHint" })
            if (!settings.BindingsV2.Values.Any(binding => binding.ModelPath == path))
                throw new JsonException($"Required bindingsV2 route: {path}.");
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
