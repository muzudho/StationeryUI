namespace StationeryUI.Styling;

using System.Globalization;
using System.Text.Json;
using StationeryUI.Canvas;

/// <summary>Viewport padding in window pixels, independent of the UI zoom.</summary>
public readonly record struct ViewportPadding(double Top, double Right, double Bottom, double Left)
{
    public ScreenRectangle GetContentBounds(double width, double height)
    {
        var x = Math.Min(Left, Math.Max(0, width));
        var y = Math.Min(Top, Math.Max(0, height));
        return new(x, y, Math.Max(0, width - x - Right), Math.Max(0, height - y - Bottom));
    }
}

public sealed record StationeryStyleSettings(bool AutoReload, ViewportPadding Padding)
{
    public static StationeryStyleSettings Default { get; } = new(true, new(8, 8, 8, 8));

    /// <summary>Parses a complete snapshot. Unknown properties are reserved for future extensions.</summary>
    public static StationeryStyleSettings Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement, "root");
        var autoReload = Default.AutoReload;
        if (root.TryGetProperty("autoReload", out var reload))
        {
            if (reload.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new JsonException("autoReload must be true or false.");
            autoReload = reload.GetBoolean();
        }
        var padding = Default.Padding;
        if (root.TryGetProperty("viewport", out var viewport))
        {
            RequireObject(viewport, "viewport");
            if (viewport.TryGetProperty("padding", out var value))
            {
                RequireObject(value, "viewport.padding");
                padding = new(ReadPixels(value, "top", padding.Top), ReadPixels(value, "right", padding.Right),
                    ReadPixels(value, "bottom", padding.Bottom), ReadPixels(value, "left", padding.Left));
            }
        }
        return new(autoReload, padding);
    }

    private static JsonElement RequireObject(JsonElement value, string path) =>
        value.ValueKind == JsonValueKind.Object ? value : throw new JsonException($"{path} must be an object.");

    private static double ReadPixels(JsonElement padding, string side, double fallback)
    {
        if (!padding.TryGetProperty(side, out var value)) return fallback;
        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : "";
        if (!text.EndsWith("px", StringComparison.Ordinal) ||
            !double.TryParse(text.AsSpan(0, text.Length - 2), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var pixels) || !double.IsFinite(pixels) || pixels < 0)
            throw new JsonException($"viewport.padding.{side} must be a nonnegative pixel string, such as \"8px\".");
        return pixels;
    }
}
