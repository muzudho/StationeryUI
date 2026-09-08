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

/// <summary>A single-window desktop UI host. Call Update before game input and Draw after the game.</summary>
public sealed class DesktopUi : IDisposable
{
    private readonly GraphicsDevice graphics;
    private readonly ITextInputService input;
    private readonly Func<string, ITextRasterizer> rasterizerFactory;
    private readonly SpriteBatch sprites;
    private readonly Texture2D pixel;
    private readonly RasterizerState clipState = new() { ScissorTestEnable = true };
    private readonly Dictionary<(string Text, string Font, int Size), Texture2D> textures = [];
    private readonly List<Element> elements = [];
    private readonly Dictionary<Keys, double> repeats = [];
    private KeyboardState previousKeyboard;
    private MouseState previousMouse;
    private Element? editing;
    private bool wasActive;
    private bool disposed;
    public StationeryTheme Theme { get; set; } = StationeryTheme.Dark;
    public UiViewport Viewport { get; } = new();
    public FocusManager Focus { get; } = new();
    public bool KeyboardConsumed { get; private set; }
    public bool PointerConsumed { get; private set; }

    public sealed class Element
    {
        internal Element(string id, ScreenRectangle bounds, string label) { Id = id; Bounds = bounds; Label = label; }
        public string Id { get; }
        public ScreenRectangle Bounds { get; set; }
        public string Label { get; set; }
        public string AccessibleName => Label;
        public StationeryTheme? Theme { get; set; }
        public UnderlineTextEditor? Editor { get; internal set; }
        internal TextInputSession? Session;
        internal Action? Click;
        internal double Scroll;
    }
    public DesktopUi(GraphicsDevice graphics, ITextInputService input, Func<string, ITextRasterizer> rasterizerFactory)
    {
        this.graphics = graphics;
        this.input = input;
        this.rasterizerFactory = rasterizerFactory;
        sprites = new(graphics);
        pixel = new(graphics, 1, 1);
        pixel.SetData(new[] { Color.White });
    }
    public Element AddTextBox(string id, ScreenRectangle bounds, string accessibleName, string text = "", int maximumLength = 1024)
    {
        var element = new Element(id, bounds, accessibleName) { Editor = new(text, maximumLength) };
        element.Session = new(input, element.Editor);
        Focus.Register(id);
        elements.Add(element);
        return element;
    }
    public Element AddButton(string id, ScreenRectangle bounds, string label, Action clicked)
    {
        var element = new Element(id, bounds, label) { Click = clicked };
        Focus.Register(id);
        elements.Add(element);
        return element;
    }
    public void Update(GameTime time, bool active, KeyboardState keyboard, MouseState mouse)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        KeyboardConsumed = PointerConsumed = false;
        if (!active)
        {
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
        PointerConsumed = hit is not null || Focus.CapturedId is not null || Focus.HasModal;
        var pressedMouse = mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        if (pressedMouse && hit is not null)
        {
            Focus.Focus(hit.Id);
            Focus.CapturePointer(hit.Id, 0);
        }
        else if (pressedMouse && !Focus.HasModal) Focus.ClearFocus();
        if (Pressed(Keys.Tab) && (editing?.Session?.Composition.Text.Length ?? 0) == 0) Focus.Move(shift);
        var next = elements.FirstOrDefault(e => e.Id == Focus.FocusedId && e.Editor is not null);
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
                else if (Focus.CapturedId == field.Id && mouse.LeftButton == ButtonState.Pressed) editor.MoveTo(CaretAt(field, pointer.X), true);
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
        var release = mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed;
        if (release)
        {
            var captured = Focus.CapturedId;
            Focus.ReleasePointer();
            if (hit?.Id == captured) hit?.Click?.Invoke();
        }
        if ((Pressed(Keys.Enter) || Pressed(Keys.Space)) && editing is null)
            elements.FirstOrDefault(e => e.Id == Focus.FocusedId)?.Click?.Invoke();
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
    private double Measure(string text, StationeryTheme theme) => rasterizerFactory(theme.FontFamily).MeasureTextWidth(text, theme.FontSize, false);
    public void Draw()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var oldScissor = graphics.ScissorRectangle;
        try
        {
            foreach (var e in elements)
            {
                var theme = e.Theme ?? Theme;
                var bounds = RectangleOf(Viewport.ToWindow(e.Bounds));
                graphics.ScissorRectangle = Rectangle.Intersect(bounds, graphics.Viewport.Bounds);
                if (graphics.ScissorRectangle.Width <= 0 || graphics.ScissorRectangle.Height <= 0) continue;
                sprites.Begin(blendState: BlendState.NonPremultiplied, samplerState: SamplerState.LinearClamp, rasterizerState: clipState);
                var focused = e.Id == Focus.FocusedId;
                var hovered = Contains(e.Bounds, Viewport.ToLogical(new(previousMouse.X, previousMouse.Y)));
                Fill(e.Bounds, e.Editor is null ? theme.ButtonFill(true, Focus.CapturedId == e.Id, false, hovered) : theme.Surface);
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
        finally { graphics.ScissorRectangle = oldScissor; }
    }
    private void DrawText(string text, double x, double y, StationeryTheme theme, ButtonColor color)
    {
        if (text.Length == 0) return;
        var key = (text, theme.FontFamily, theme.FontSize);
        if (!textures.TryGetValue(key, out var texture))
        {
            if (textures.Count >= 128)
            {
                var oldest = textures.First(); oldest.Value.Dispose(); textures.Remove(oldest.Key);
            }
            using var stream = new MemoryStream(rasterizerFactory(theme.FontFamily).RasterizePng(text, theme.FontSize, false));
            texture = Texture2D.FromStream(graphics, stream);
            textures.Add(key, texture);
        }
        sprites.Draw(texture, RectangleOf(Viewport.ToWindow(new(x, y, texture.Width, texture.Height))), Convert(color));
    }
    private void Fill(ScreenRectangle rect, ButtonColor color) => sprites.Draw(pixel, RectangleOf(Viewport.ToWindow(rect)), Convert(color));
    private static Rectangle RectangleOf(ScreenRectangle b) => new((int)Math.Round(b.X), (int)Math.Round(b.Y), Math.Max(1, (int)Math.Round(b.Width)), Math.Max(1, (int)Math.Round(b.Height)));
    public static Color Convert(ButtonColor c) => new(c.R, c.G, c.B, c.A);
    private static bool Contains(ScreenRectangle b, ScreenPoint p) => p.X >= b.X && p.Y >= b.Y && p.X < b.X + b.Width && p.Y < b.Y + b.Height;
    public void Dispose()
    {
        if (disposed) return;
        editing?.Session?.Blur();
        foreach (var texture in textures.Values) texture.Dispose();
        textures.Clear(); sprites.Dispose(); pixel.Dispose(); clipState.Dispose(); disposed = true;
    }
}
