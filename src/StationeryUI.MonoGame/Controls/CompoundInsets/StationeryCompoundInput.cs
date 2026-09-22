namespace StationeryUI.MonoGame;

using StationeryUI.Canvas;

/// <summary>A reusable visual editor for the four margin and padding sides.</summary>
public sealed class StationeryCompoundInput
{
    private readonly Dictionary<string, StationeryUiHost.Element> fields = new(StringComparer.Ordinal);

    internal StationeryCompoundInput(StationeryUiHost host, string idPrefix, ScreenRectangle bounds,
        IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrWhiteSpace(idPrefix)) throw new ArgumentException("An id prefix is required.", nameof(idPrefix));
        if (bounds.Width < 490 || bounds.Height < 220) throw new ArgumentException("Compound input bounds are too small.", nameof(bounds));
        AddLabel(host, idPrefix + "Title", bounds with { Height = 36 }, "margin / padding (px)");
        AddLabel(host, idPrefix + "Margin", new(bounds.X, bounds.Y + 38, 180, 40), "margin");
        AddLabel(host, idPrefix + "Padding", new(bounds.X, bounds.Y + 74, 180, 40), "padding");
        AddField(host, idPrefix, values, "margin", "top", new(bounds.X + 218, bounds.Y + 38, 100, 34));
        AddField(host, idPrefix, values, "padding", "top", new(bounds.X + 218, bounds.Y + 74, 100, 34));
        AddField(host, idPrefix, values, "margin", "left", new(bounds.X + 84, bounds.Y + 110, 100, 34));
        AddField(host, idPrefix, values, "padding", "left", new(bounds.X + 176, bounds.Y + 110, 100, 34));
        AddField(host, idPrefix, values, "padding", "right", new(bounds.X + 298, bounds.Y + 110, 100, 34));
        AddField(host, idPrefix, values, "margin", "right", new(bounds.X + 390, bounds.Y + 110, 100, 34));
        AddField(host, idPrefix, values, "padding", "bottom", new(bounds.X + 218, bounds.Y + 146, 100, 34));
        AddField(host, idPrefix, values, "margin", "bottom", new(bounds.X + 218, bounds.Y + 182, 100, 34));
    }

    /// <summary>Editable fields keyed by <c>margin.top</c>, <c>padding.left</c>, and so on.</summary>
    public IReadOnlyDictionary<string, StationeryUiHost.Element> Fields => fields;

    /// <summary>Returns the current text values of all fields.</summary>
    public IReadOnlyDictionary<string, string> ReadValues()
        => fields.ToDictionary(pair => pair.Key, pair => pair.Value.Editor?.Text ?? "", StringComparer.Ordinal);

    private static void AddLabel(StationeryUiHost host, string id, ScreenRectangle bounds, string text)
        => host.AddTextBlock(host.Root.AddChild(id, "textBlock"), bounds, text);

    private void AddField(StationeryUiHost host, string idPrefix, IReadOnlyDictionary<string, string> values,
        string group, string side, ScreenRectangle bounds)
    {
        var key = group + "." + side;
        var id = idPrefix + char.ToUpperInvariant(group[0]) + group[1..] + char.ToUpperInvariant(side[0]) + side[1..];
        fields.Add(key, host.AddTextBox(id, bounds, key, values.GetValueOrDefault(key, "0"), 18));
    }
}
