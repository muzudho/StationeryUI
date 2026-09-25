namespace StationeryUI.Styling;

using System.Text.Json;

/// <summary>Loading policy, stored separately from visual styles.</summary>
public sealed record StationeryStyleConfiguration(string StyleFile, bool AutoReload)
{
    public static StationeryStyleConfiguration Default { get; } = new("demo.stationery-ui.json", true);

    public static StationeryStyleConfiguration Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Configuration must be an object.");
        var path = Default.StyleFile;
        if (root.TryGetProperty("styleFile", out var file))
        {
            if (file.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(file.GetString()))
                throw new JsonException("styleFile must be a nonempty path string.");
            path = file.GetString()!;
        }
        var autoReload = Default.AutoReload;
        if (root.TryGetProperty("autoReload", out var reload))
        {
            if (reload.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new JsonException("autoReload must be true or false.");
            autoReload = reload.GetBoolean();
        }
        return new(path, autoReload);
    }
}
