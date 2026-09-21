namespace StationeryUI.Theming;

using StationeryUI.Controls;

/// <summary>Immutable application theme. Individual controls can override a theme or a state.</summary>
public sealed record StationeryTheme
{
    public static StationeryTheme Dark { get; } = new();
    public static StationeryTheme Light { get; } = new()
    {
        Background = new(247, 244, 234), Text = new(35, 40, 44), Border = new(112, 120, 116),
        Surface = new(235, 232, 218), Hover = new(214, 231, 223), Pressed = new(185, 212, 199),
        Selected = new(179, 222, 201), Disabled = new(225, 224, 218), DisabledText = new(132, 132, 125),
        Accent = new(27, 119, 89), Selection = new(165, 207, 229), Composition = new(132, 86, 12),
        Shadow = new(0, 0, 0, 32)
    };
    public ButtonColor Background { get; init; } = new(24, 29, 36);
    public ButtonColor Surface { get; init; } = new(36, 48, 58);
    public ButtonColor Text { get; init; } = new(255, 255, 255);
    public ButtonColor Border { get; init; } = new(126, 150, 164);
    public ButtonColor Accent { get; init; } = new(99, 223, 185);
    public ButtonColor Hover { get; init; } = new(58, 82, 94);
    public ButtonColor Pressed { get; init; } = new(40, 104, 83);
    public ButtonColor Selected { get; init; } = new(31, 151, 112);
    public ButtonColor Disabled { get; init; } = new(24, 27, 31);
    public ButtonColor DisabledText { get; init; } = new(91, 100, 106);
    public ButtonColor Selection { get; init; } = new(50, 108, 139, 210);
    public ButtonColor TreeTarget { get; init; } = new(80, 195, 245);
    public ButtonColor Composition { get; init; } = new(255, 225, 128);
    public ButtonColor Shadow { get; init; } = new(0, 0, 0, 95);
    public string FontFamily { get; init; } = "Meiryo";
    public int FontSize { get; init; } = 22;
    public int Padding { get; init; } = 12;
    public int BorderWidth { get; init; } = 2;
    public int CornerRadius { get; init; } = 0;
    public int ShadowOffset { get; init; } = 4;

    public ButtonColor ButtonFill(bool enabled, bool pressed, bool selected, bool hovered) =>
        !enabled ? Disabled : pressed ? Pressed : selected ? Selected : hovered ? Hover : Surface;
}
