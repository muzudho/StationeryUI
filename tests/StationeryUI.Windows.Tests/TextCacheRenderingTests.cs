using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.MonoGame;
using StationeryUI.Theming;
using StationeryUI.Windows;

// GPU regression: selecting captured nodes changes the inspector's multiline details.
// A cache eviction must not destroy a texture already queued by SpriteBatch.Draw.
internal sealed class TextCacheRenderingTests : Game
{
    private readonly GraphicsDeviceManager manager;

    public TextCacheRenderingTests()
    {
        manager = new(this) { PreferredBackBufferWidth = 640, PreferredBackBufferHeight = 480 };
        IsFixedTimeStep = false;
    }

    protected override void LoadContent()
    {
        using var input = new WindowsTextInputService(Window.Handle);
        using var target = new RenderTarget2D(GraphicsDevice, 640, 480);
        foreach (var theme in new[] { StationeryTheme.Dark, StationeryTheme.Light })
        foreach (var scale in new[] { 1.0, 1.5 })
        {
            using var cached = new StationeryUiHost(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme };
            using var fresh = new StationeryUiHost(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme };
            cached.Viewport.Scale = scale;
            fresh.Viewport.Scale = scale;
            var actualText = cached.AddTextBlock(cached.Root.AddChild("details", "textBlock"), new(0, 0, 420, 300), "");
            var referenceText = fresh.AddTextBlock(fresh.Root.AddChild("details", "textBlock"), new(0, 0, 420, 300), "");

            Color[] Render(StationeryUiHost host)
            {
                GraphicsDevice.SetRenderTarget(target);
                GraphicsDevice.Clear(Color.CornflowerBlue);
                host.Draw();
                GraphicsDevice.SetRenderTarget(null);
                var pixels = new Color[640 * 480];
                target.GetData(pixels);
                return pixels;
            }

            // Make the first detail line the oldest cached texture, then fill all 128 entries.
            for (var i = 0; i < 128; i++)
            {
                actualText.Label = $"Id: component{i}";
                Render(cached);
            }
            // First line is queued, then the new second line evicts it before End().
            actualText.Label = referenceText.Label = "Id: component0\nPath: /demo/body/captured\nKind: Button";
            var expected = Render(fresh);
            var actual = Render(cached);
            var differences = expected.Zip(actual).Count(pair => pair.First != pair.Second);
            if (differences != 0)
                throw new Exception($"Text cache eviction corrupted {differences} pixels at scale {scale}.");
            // Also check subsequent frames after retired textures have been released.
            if (!Render(cached).SequenceEqual(expected)) throw new Exception("Text changed after eviction frame.");
        }
        Console.WriteLine("PASS text cache eviction: exact rendered pixels in light/dark themes at 100%/150%.");
        Exit();
    }
}
