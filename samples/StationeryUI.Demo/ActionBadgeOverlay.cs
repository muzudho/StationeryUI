using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.MonoGame;
using StationeryUI.MonoGame.Controls.ActionBadge;
using StationeryUI.Windows;

/// <summary>移植済みバッジを、DesktopUi のウィンドウ座標へ重ねるデモ用アダプター。</summary>
internal sealed class ActionBadgeOverlay : IDisposable
{
    private readonly Texture2D fontTexture;
    private readonly ScreenCanvas canvas;
    private readonly StationeryDrawingTools drawing;
    private readonly Dictionary<string, ActionBadgeComponent> badges = [];

    public ActionBadgeOverlay(GraphicsDevice graphics)
    {
        // Content パイプラインや配布フォントを不要にするため、インストール済みフォントから生成する。
        var rasterizer = new WindowsTextRasterizer();
        var characters = Enumerable.Range(32, 95).Select(value => (char)value).ToList();
        var glyphs = new List<Rectangle>();
        var cropping = new List<Rectangle>();
        var kerning = new List<Vector3>();
        const int cell = 128;
        const int columns = 16;
        var width = columns * cell;
        var pixels = new Color[width * cell * 6];
        for (var index = 0; index < characters.Count; index++)
        {
            using var stream = new MemoryStream(rasterizer.RasterizePng(characters[index].ToString(), 64, true));
            using var texture = Texture2D.FromStream(graphics, stream);
            var source = new Color[texture.Width * texture.Height];
            texture.GetData(source);
            var x = index % columns * cell;
            var y = index / columns * cell;
            for (var row = 0; row < texture.Height; row++)
                for (var column = 0; column < texture.Width; column++)
                {
                    var color = source[row * texture.Width + column];
                    pixels[(y + row) * width + x + column] = Color.FromNonPremultiplied(color.ToVector4());
                }
            glyphs.Add(new(x, y, texture.Width, texture.Height));
            cropping.Add(new(0, 0, texture.Width, texture.Height));
            kerning.Add(new(0, texture.Width, 0));
        }
        fontTexture = new(graphics, width, cell * 6);
        fontTexture.SetData(pixels);
        var font = new SpriteFont(fontTexture, glyphs, cropping, characters, 96, 0, kerning, '?');
        canvas = new(graphics, font);
        drawing = new(canvas, rasterizer);
    }

    public void Draw(DesktopUi ui, DesktopUi.Element element, string label, Point pointer, bool active)
    {
        var bounds = ui.Viewport.ToWindow(element.Bounds);
        var hovered = active && pointer.X >= bounds.X && pointer.X < bounds.X + bounds.Width
            && pointer.Y >= bounds.Y && pointer.Y < bounds.Y + bounds.Height;
        if (!badges.TryGetValue(element.Path, out var badge))
            badges[element.Path] = badge = ActionBadgeComponent.Create(label, Rectangle.Empty);
        var logical = element.Bounds;
        badge.SetAnchorBounds(new((int)logical.X, (int)logical.Y, (int)logical.Width, (int)logical.Height));
        if (hovered && (element.Editor is null || ui.Focus.FocusedId != element.Path)) badge.Show();
        else badge.Hide();
        // DesktopUi と同じ倍率・オフセットで描き、バッジも拡大率に追従させる。
        canvas.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp,
            transformMatrix: Matrix.CreateScale((float)ui.Viewport.Scale)
                * Matrix.CreateTranslation((float)ui.Viewport.Offset.X, (float)ui.Viewport.Offset.Y, 0));
        badge.Draw(drawing);
        drawing.End();
    }

    public void DrawDialogBackground(DesktopUi popup)
    {
        var bounds = popup.Viewport.ToWindow(new(0, 0, 800, 320));
        var topLeft = canvas.ToVirtualPoint(new((int)bounds.X, (int)bounds.Y));
        var bottomRight = canvas.ToVirtualPoint(new((int)(bounds.X + bounds.Width), (int)(bounds.Y + bounds.Height)));
        drawing.Begin();
        drawing.FillRectangle(new(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 180));
        drawing.FillRoundedRectangle(new(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y),
            12, DesktopUi.Convert(popup.Theme.Background));
        drawing.End();
    }

    public void Dispose()
    {
        drawing.Dispose();
        canvas.Dispose();
        fontTexture.Dispose();
    }
}
