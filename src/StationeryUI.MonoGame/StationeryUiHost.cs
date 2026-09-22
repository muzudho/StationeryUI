namespace StationeryUI.MonoGame;

using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
using StationeryUI.Input;
using StationeryUI.Platform;
using StationeryUI.Text;
using StationeryUI.Theming;
using StationeryUI.Inspection;

/// <summary>A single-window desktop UI host. Call Update before game input and Draw after the game.</summary>
public sealed partial class StationeryUiHost : IDisposable
{
    private readonly GraphicsDevice graphics;
    private readonly ITextInputService input;
    private readonly Func<string, ITextRasterizer> rasterizerFactory;
    private readonly SpriteBatch sprites;
    private readonly Texture2D pixel;
    private readonly RasterizerState clipState = new() { ScissorTestEnable = true };
    private readonly Dictionary<(string Text, string Font, int Size), Texture2D> textures = [];
    private readonly List<Texture2D> retiredTextTextures = [];
    private readonly List<Element> elements = [];
    private readonly Dictionary<Keys, double> repeats = [];
    private KeyboardState previousKeyboard;
    private MouseState previousMouse;
    private Element? editing;
    private bool wasActive;
    private bool disposed;
    public StationeryTheme Theme { get; set; } = StationeryTheme.Dark;
    /// <summary>Use the shared framed stationery button renderer instead of underline-style actions.</summary>
    public bool UseStationeryButtons { get; set; }
    public UiViewport Viewport { get; } = new();
    public FocusManager Focus { get; private set; } = new();
    public bool KeyboardConsumed { get; private set; }
    public bool PointerConsumed { get; private set; }
    public bool IsComposing => (editing?.Session?.Composition.Text.Length ?? 0) > 0;
    public StationeryNode Root { get; private set; }

    public string? HoveredToolHint { get; private set; }
    /// <summary>Optional application hints; an element's explicit ToolHint takes precedence.</summary>
    public Func<Element, string?>? ToolHintProvider { get; set; }
    public sealed class Element
    {
        public string? ToolHint { get; set; }
        public StationeryBoxModel? BoxModel { get; set; }
        internal Element(StationeryNode node, ScreenRectangle bounds, string label) { Node = node; Bounds = bounds; Label = label; }
        public StationeryNode Node { get; internal set; }
        public string Id => Node.Id;
        public string Path => Node.Path;
        public ScreenRectangle Bounds { get; set; }
        public string Label { get; set; }
        public string AccessibleName => Label;
        public StationeryTheme? Theme { get; set; }
        public UnderlineTextEditor? Editor { get; internal set; }
        public TreeView? Tree { get; internal set; }
        public SplitPane? Split { get; internal set; }
        internal Element? FirstPane, SecondPane;
        internal bool DraggingSplit;
        internal double SplitGrab;
        internal double TreeScroll;
        internal double TreeHorizontalScroll;
        internal bool DraggingTreeHorizontalScroll;
        internal TreeItem? PressedTreeItem;
        internal bool PressedTreeToggle;
        internal bool DraggingTreeScroll;
        internal double TreeThumbGrab;
        internal TextInputSession? Session;
        internal Action? Click;
        internal double Scroll;
        internal bool DraggingTextScroll;
        internal double TextThumbGrab;
        internal string? WrappedText;
        internal double WrapWidth;
        internal StationeryTheme? WrapTheme;
        internal List<string> WrappedLines = [];
    }
    public StationeryUiHost(GraphicsDevice graphics, ITextInputService input, Func<string, ITextRasterizer> rasterizerFactory,
        StationeryNode? root = null)
    {
        this.graphics = graphics;
        this.input = input;
        this.rasterizerFactory = rasterizerFactory;
        Root = root ?? new StationeryNode("viewport");
        sprites = new(graphics);
        pixel = new(graphics, 1, 1);
        pixel.SetData(new[] { Color.White });
    }
    public Element AddTextBox(string id, ScreenRectangle bounds, string accessibleName, string text = "", int maximumLength = 1024,
        StationeryNode? parent = null)
        => AddTextBox(AddNode(id, "textBox", parent), bounds, accessibleName, text, maximumLength);

    public Element AddTextBox(StationeryNode node, ScreenRectangle bounds, string accessibleName, string text = "", int maximumLength = 1024)
    {
        ValidateNode(node, "textBox");
        var element = new Element(node, bounds, accessibleName) { Editor = new(text, maximumLength) };
        element.Session = new(input, element.Editor);
        Focus.Register(element.Path);
        elements.Add(element);
        return element;
    }
    public Element AddButton(string id, ScreenRectangle bounds, string label, Action clicked, StationeryNode? parent = null)
        => AddButton(AddNode(id, "button", parent), bounds, label, clicked);

    public Element AddButton(StationeryNode node, ScreenRectangle bounds, string label, Action clicked)
    {
        ValidateNode(node, "button");
        var element = new Element(node, bounds, label) { Click = clicked };
        Focus.Register(element.Path);
        elements.Add(element);
        return element;
    }
    private void ValidateNode(StationeryNode node, string kind)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!node.IsWithin(Root) || node.Kind != kind || elements.Any(element => element.Node == node))
            throw new ArgumentException("Node must be an unbound node of the expected type in this UI's model.", nameof(node));
    }

    /// <summary>Replaces model identities atomically while preserving controls, editing state and callbacks.</summary>
    public void RebindModel(StationeryNode root, Func<Element, StationeryNode> resolve)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var bindings = elements.Select(element => (Element: element, Node: resolve(element))).ToArray();
        if (bindings.Any(binding => !binding.Node.IsWithin(root) || binding.Node.Kind != binding.Element.Node.Kind) ||
            bindings.Select(binding => binding.Node.Path).Distinct(StringComparer.Ordinal).Count() != bindings.Length)
            throw new ArgumentException("Model bindings must have matching types and unique paths.", nameof(resolve));
        var focused = elements.FirstOrDefault(element => element.Path == Focus.FocusedId);
        var nextFocus = new FocusManager();
        foreach (var binding in bindings) nextFocus.Register(binding.Node.Path);
        Root = root;
        foreach (var binding in bindings)
        {
            binding.Element.Node = binding.Node;
            binding.Element.DraggingTreeScroll = false;
            binding.Element.DraggingTreeHorizontalScroll = false;
            binding.Element.DraggingSplit = false;
            binding.Element.DraggingTextScroll = false;
            binding.Element.PressedTreeItem = null;
        }
        Focus = nextFocus;
        if (focused is not null) Focus.Focus(focused.Path);
    }
    private StationeryNode AddNode(string id, string kind, StationeryNode? parent)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        parent ??= Root;
        if (!parent.IsWithin(Root)) throw new ArgumentException("Parent must belong to this UI's subtree.", nameof(parent));
        return parent.AddChild(id, kind);
    }

    /// <summary>Take a snapshot on the game thread; inspectors never read mutable UI elements directly.</summary>
    /// <summary>Includes layout types and grid cells from the current style settings.</summary>
    public IReadOnlyList<StationeryInspectionEntry> Inspect(StationeryUI.Styling.StationeryStyleSettings settings, bool visible = true)
        => DeveloperInspectionLayout.Apply(Inspect(visible), settings);

    /// <summary>Includes nested layout rectangles from the current arrangement, in window pixels.</summary>
    public IReadOnlyList<StationeryInspectionEntry> Inspect(StationeryUI.Styling.StationeryStyleSettings settings,
        StationeryUI.Styling.StationeryLayoutResult arranged, bool visible = true)
        => DeveloperInspectionLayout.Apply(Inspect(visible), settings, arranged);

    public IReadOnlyList<StationeryInspectionEntry> Inspect(bool visible = true)
    {
        ArrangeSplitPanes();
        var result = new List<StationeryInspectionEntry>();
        var byNode = elements.ToDictionary(element => element.Node);
        void Visit(StationeryNode node)
        {
            byNode.TryGetValue(node, out var element);
            result.Add(new(node.Id, node.Path, node.Parent?.Path, node.Kind, element?.AccessibleName ?? node.Id,
                visible && (element is null || element.Bounds.Width > 0 && element.Bounds.Height > 0),
                element is null ? null : Viewport.ToWindow(element.Bounds)));
            if (element?.Tree is not null) InspectTree(element, visible, result);
            foreach (var child in node.Children) Visit(child);
        }
        Visit(Root);
        return result;
    }
    /// <summary>Updates layout and input. Pass the owning Game.IsActive (and any modal input gate) as active.</summary>
    /// <remarks>Keep calling with active=false while inactive to cancel drags and synchronize input history.
    /// The host ignores input then; the application must also gate its own mouse handlers and GameComponents.</remarks>
    public void Update(GameTime time, bool active, KeyboardState keyboard, MouseState mouse)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        KeyboardConsumed = PointerConsumed = false;
        HoveredToolHint = null;
        ArrangeSplitPanes();
        foreach (var element in elements)
            Focus.SetVisible(element.Path, element.Bounds.Width > 0 && element.Bounds.Height > 0);
        if (!active)
        {
            foreach (var element in elements)
            {
                element.PressedTreeItem = null;
                element.DraggingTreeScroll = false;
                element.DraggingTreeHorizontalScroll = false;
                element.DraggingSplit = false;
                element.DraggingTextScroll = false;
            }
            editing?.Session?.Blur(); editing = null; Focus.Deactivate();
            repeats.Clear();
            previousKeyboard = keyboard; previousMouse = mouse; wasActive = false;
            return;
        }
        if (!wasActive) { previousKeyboard = keyboard; previousMouse = mouse; wasActive = true; }
        bool Pressed(Keys key) => keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);
        bool Repeated(Keys key)
        {
            if (keyboard.IsKeyUp(key)) { repeats.Remove(key); return false; }
            if (previousKeyboard.IsKeyUp(key)) { repeats[key] = .42; return true; }
            var remaining = repeats.GetValueOrDefault(key, .42) - time.ElapsedGameTime.TotalSeconds;
            repeats[key] = remaining > 0 ? remaining : .055;
            return remaining <= 0;
        }
        var shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        var control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        var pointer = Viewport.ToLogical(new(mouse.X, mouse.Y));
        var hit = elements.LastOrDefault(e => Contains(e.Bounds, pointer));
        HoveredToolHint = hit is null ? null : hit.ToolHint ?? ToolHintProvider?.Invoke(hit);
        PointerConsumed = hit is not null || Focus.CapturedId is not null || Focus.HasModal;
        var pressedMouse = mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        if (pressedMouse && hit is not null)
        {
            Focus.Focus(hit.Path);
            Focus.CapturePointer(hit.Path, 0);
        }
        else if (pressedMouse && !Focus.HasModal) Focus.ClearFocus();
        if (Pressed(Keys.Tab) && (editing?.Session?.Composition.Text.Length ?? 0) == 0) Focus.Move(shift);
        foreach (var element in elements.Where(e => e.Split is not null))
            UpdateSplit(element, pointer, pressedMouse, mouse.LeftButton == ButtonState.Pressed, Repeated);
        ArrangeSplitPanes();
        var next = elements.FirstOrDefault(e => e.Path == Focus.FocusedId && e.Editor is not null);
        if (next != editing)
        {
            editing?.Session?.Blur(); editing = next; editing?.Session?.Focus();
            repeats.Clear();
        }
        if (editing is { Editor: { } editor, Session: { } session } field)
        {
            session.Update(keyboard.IsKeyDown(Keys.Enter), keyboard.IsKeyDown(Keys.Escape));
            if (session.Composition.Text.Length == 0)
            {
                if (pressedMouse && hit == field) editor.MoveTo(CaretAt(field, pointer.X), shift);
                else if (Focus.CapturedId == field.Path && mouse.LeftButton == ButtonState.Pressed) editor.MoveTo(CaretAt(field, pointer.X), true);
                if (Repeated(Keys.Left)) editor.Move(-1, shift);
                if (Repeated(Keys.Right)) editor.Move(1, shift);
                if (Pressed(Keys.Home)) editor.MoveTo(0, shift);
                if (Pressed(Keys.End)) editor.MoveTo(editor.Text.Length, shift);
                if (Repeated(Keys.Back)) editor.Delete(true);
                if (Repeated(Keys.Delete)) editor.Delete(false);
                if (control)
                {
                    if (Pressed(Keys.A)) editor.SelectAll();
                    if (Pressed(Keys.Z)) editor.Undo();
                    if (Pressed(Keys.Y)) editor.Redo();
                    if ((Pressed(Keys.C) || Pressed(Keys.X)) && editor.SelectionLength > 0)
                    {
                        input.WriteClipboard(editor.SelectedText);
                        if (Pressed(Keys.X)) editor.Delete(false);
                    }
                    if (Pressed(Keys.V)) editor.Insert(input.ReadClipboard());
                }
            }
            EnsureCaretVisible(field);
            var theme = field.Theme ?? Theme;
            var display = DisplayText(field, out var caretIndex);
            var text = display[..caretIndex];
            var caret = field.Bounds.X + theme.Padding + Measure(text, theme) - field.Scroll;
            input.SetInputArea(Viewport.ToWindow(new(caret, field.Bounds.Y, 2, field.Bounds.Height)));
        }
        foreach (var element in elements.Where(e => e.Node.Kind == "textBlock"))
            UpdateTextBlock(element, pointer, hit == element, pressedMouse, mouse.LeftButton == ButtonState.Pressed,
                mouse.ScrollWheelValue - previousMouse.ScrollWheelValue, Pressed, control);
        var release = mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed;
        if (release)
        {
            var captured = Focus.CapturedId;
            foreach (var element in elements.Where(e => e.Tree is not null))
                ReleaseTree(element, pointer, hit == element && captured == element.Path);
            Focus.ReleasePointer();
            if (hit?.Path == captured) hit?.Click?.Invoke();
        }
        foreach (var element in elements.Where(e => e.Tree is not null))
            UpdateTree(element, pointer, hit == element, pressedMouse, mouse.ScrollWheelValue - previousMouse.ScrollWheelValue,
                mouse.HorizontalScrollWheelValue - previousMouse.HorizontalScrollWheelValue, shift, Pressed, Repeated);
        if ((Pressed(Keys.Enter) || Pressed(Keys.Space)) && editing is null)
            elements.FirstOrDefault(e => e.Path == Focus.FocusedId)?.Click?.Invoke();
        KeyboardConsumed = Focus.ConsumesKeyboard;
        previousKeyboard = keyboard; previousMouse = mouse;
    }
    private int CaretAt(Element field, double x)
    {
        var theme = field.Theme ?? Theme;
        var text = field.Editor!.Text;
        return StringInfo.ParseCombiningCharacters(text).Append(text.Length)
            .MinBy(i => Math.Abs(field.Bounds.X + theme.Padding + Measure(text[..i], theme) - field.Scroll - x));
    }
    private void EnsureCaretVisible(Element field)
    {
        var theme = field.Theme ?? Theme;
        var width = Math.Max(1, field.Bounds.Width - theme.Padding * 2 - 2);
        var display = DisplayText(field, out var caretIndex);
        var caret = Measure(display[..caretIndex], theme);
        if (caret - field.Scroll > width) field.Scroll = caret - width;
        if (caret < field.Scroll) field.Scroll = caret;
        field.Scroll = Math.Max(0, Math.Min(field.Scroll, Math.Max(0, Measure(display, theme) - width)));
    }
    private static string DisplayText(Element field, out int caret)
    {
        var editor = field.Editor!;
        var composition = field.Session!.Composition;
        if (composition.Text.Length == 0) { caret = editor.Caret; return editor.Text; }
        caret = editor.SelectionStart + Math.Clamp(composition.CaretIndex, 0, composition.Text.Length);
        return editor.Text.Remove(editor.SelectionStart, editor.SelectionLength).Insert(editor.SelectionStart, composition.Text);
    }
    private int TextPixelSize(StationeryTheme theme) => Math.Max(1, (int)Math.Round(theme.FontSize * Viewport.Scale));
    private double Measure(string text, StationeryTheme theme) =>
        rasterizerFactory(theme.FontFamily).MeasureTextWidth(text, TextPixelSize(theme), false) / Viewport.Scale;
    public void Draw()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArrangeSplitPanes();
        var oldScissor = graphics.ScissorRectangle;
        try
        {
            foreach (var e in elements)
            {
                if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) continue;
                var theme = e.Theme ?? Theme;
                var bounds = RectangleOf(Viewport.ToWindow(e.Bounds));
                graphics.ScissorRectangle = Rectangle.Intersect(bounds, graphics.Viewport.Bounds);
                if (graphics.ScissorRectangle.Width <= 0 || graphics.ScissorRectangle.Height <= 0) continue;
                sprites.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.LinearClamp, rasterizerState: clipState);
                if (e.Tree is not null)
                {
                    DrawTree(e, theme);
                    sprites.End();
                    continue;
                }
                var focused = e.Path == Focus.FocusedId;
                if (e.Node.Kind == "textBlock")
                {
                    DrawTextBlock(e, theme);
                    sprites.End();
                    continue;
                }
                var hovered = Contains(e.Bounds, Viewport.ToLogical(new(previousMouse.X, previousMouse.Y)));
                if (e.Split is not null)
                {
                    var divider = FromWindow(e.Split.Arrange(Viewport.ToWindow(e.Bounds)).Divider);
                    Fill(divider, focused || e.DraggingSplit ? theme.Accent : theme.Border);
                    sprites.End();
                    continue;
                }
                if (e.Node.Kind == "link")
                {
                    Fill(e.Bounds, theme.Background);
                    var x = e.Bounds.X + theme.Padding;
                    var y = e.Bounds.Y + Math.Min(theme.Padding, Math.Max(0, (e.Bounds.Height - theme.FontSize * 1.5 - 2) / 2));
                    DrawText(e.Label, x, y, theme, theme.Accent);
                    Fill(new(x, y + theme.FontSize * 1.5, Math.Max(1, Measure(e.Label, theme)), focused || hovered ? 2 : 1), theme.Accent);
                    sprites.End();
                    continue;
                }
                if (e.Node.Kind == "button" && UseStationeryButtons)
                {
                    var model = new IconButtonModel(e.Bounds, string.IsNullOrWhiteSpace(e.Label) ? e.Id : e.Label)
                    { IsEnabled = Focus.IsEnabled(e.Path), IsFocused = focused };
                    var pointer = Viewport.ToLogical(new(previousMouse.X, previousMouse.Y));
                    model.UpdatePointer(pointer);
                    if (Focus.CapturedId == e.Path && previousMouse.LeftButton == ButtonState.Pressed) model.Press(pointer);
                    StationeryButtonRenderer.Draw(model, Fill, DrawButtonOutline, (bounds, color) =>
                    {
                        var x = bounds.X + Math.Max(theme.Padding, (bounds.Width - Measure(e.Label, theme)) / 2);
                        var y = bounds.Y + Math.Max(0, (bounds.Height - theme.FontSize * 1.5) / 2);
                        DrawText(e.Label, x, y, theme, color);
                    }, theme);
                    sprites.End();
                    continue;
                }
                Fill(e.Bounds, e.Editor is null ? theme.ButtonFill(true, Focus.CapturedId == e.Path, false, hovered) : theme.Surface);
                var line = focused ? theme.Accent : theme.Border;
                Fill(new(e.Bounds.X, e.Bounds.Y + e.Bounds.Height - theme.BorderWidth, e.Bounds.Width, theme.BorderWidth), line);
                if (e.Editor is { } editor)
                {
                    var composition = e.Session!.Composition;
                    var composing = composition.Text.Length > 0;
                    var insertion = composing ? editor.SelectionStart : editor.Caret;
                    var display = composing ? editor.Text.Remove(editor.SelectionStart, editor.SelectionLength).Insert(insertion, composition.Text) : editor.Text;
                    var x = e.Bounds.X + theme.Padding - e.Scroll;
                    var y = e.Bounds.Y + Math.Max(0, (e.Bounds.Height - theme.FontSize * 1.5) / 2);
                    if (focused && editor.SelectionLength > 0 && !composing)
                    {
                        var left = Measure(editor.Text[..editor.SelectionStart], theme);
                        var right = Measure(editor.Text[..(editor.SelectionStart + editor.SelectionLength)], theme);
                        Fill(new(x + left, y, Math.Max(1, right - left), theme.FontSize * 1.5), theme.Selection);
                    }
                    DrawText(display, x, y, theme, theme.Text);
                    var caret = Measure(display[..Math.Min(display.Length, insertion + (composing ? composition.CaretIndex : 0))], theme);
                    if (composing)
                    {
                        var left = Measure(display[..insertion], theme);
                        var right = Measure(display[..(insertion + composition.Text.Length)], theme);
                        Fill(new(x + left, y + theme.FontSize * 1.5, Math.Max(1, right - left), 2), theme.Composition);
                    }
                    if (focused) Fill(new(x + caret, y, 2, theme.FontSize * 1.5), theme.Accent);
                }
                else DrawText(e.Label, e.Bounds.X + theme.Padding, e.Bounds.Y + theme.Padding, theme, theme.Text);
                sprites.End();
            }
        }
        finally
        {
            // SpriteBatch queues Draw calls until End. Evicted text may still be in
            // that queue, so keep its GPU resource alive until drawing has finished.
            ReleaseRetiredTextTextures();
            graphics.ScissorRectangle = oldScissor;
        }
    }
    private void DrawText(string text, double x, double y, StationeryTheme theme, ButtonColor color)
    {
        if (text.Length == 0) return;
        // Rasterize at the final window size. Resampling small glyphs loses thin strokes,
        // especially white text on a dark surface at fractional viewport scales.
        var pixelSize = TextPixelSize(theme);
        var key = (text, theme.FontFamily, pixelSize);
        if (!textures.TryGetValue(key, out var texture))
        {
            if (textures.Count >= 128)
            {
                var oldest = textures.First();
                retiredTextTextures.Add(oldest.Value);
                textures.Remove(oldest.Key);
            }
            using var stream = new MemoryStream(rasterizerFactory(theme.FontFamily).RasterizePng(text, pixelSize, false));
            texture = Texture2D.FromStream(graphics, stream);
            textures.Add(key, texture);
        }
        var position = Viewport.ToWindow(new(x, y, 0, 0));
        sprites.Draw(texture, new Rectangle((int)Math.Round(position.X), (int)Math.Round(position.Y), texture.Width, texture.Height), Convert(color));
    }
    private void Fill(ScreenRectangle rect, ButtonColor color) => sprites.Draw(pixel, RectangleOf(Viewport.ToWindow(rect)), Convert(color));
    private void DrawButtonOutline(ScreenRectangle bounds, double width, ButtonColor color)
    {
        width = Math.Min(width, Math.Min(bounds.Width, bounds.Height) / 2);
        if (width <= 0) return;
        Fill(new(bounds.X, bounds.Y, bounds.Width, width), color);
        Fill(new(bounds.X, bounds.Y + bounds.Height - width, bounds.Width, width), color);
        Fill(new(bounds.X, bounds.Y, width, bounds.Height), color);
        Fill(new(bounds.X + bounds.Width - width, bounds.Y, width, bounds.Height), color);
    }
    private static Rectangle RectangleOf(ScreenRectangle b) => new((int)Math.Round(b.X), (int)Math.Round(b.Y), Math.Max(1, (int)Math.Round(b.Width)), Math.Max(1, (int)Math.Round(b.Height)));
    public static Color Convert(ButtonColor c) => new(c.R, c.G, c.B, c.A);
    private static bool Contains(ScreenRectangle b, ScreenPoint p) => p.X >= b.X && p.Y >= b.Y && p.X < b.X + b.Width && p.Y < b.Y + b.Height;
    private void ReleaseRetiredTextTextures()
    {
        foreach (var texture in retiredTextTextures) texture.Dispose();
        retiredTextTextures.Clear();
    }
    public void Dispose()
    {
        if (disposed) return;
        editing?.Session?.Blur();
        foreach (var texture in textures.Values) texture.Dispose();
        ReleaseRetiredTextTextures();
        textures.Clear(); sprites.Dispose(); pixel.Dispose(); clipState.Dispose(); disposed = true;
    }
}
