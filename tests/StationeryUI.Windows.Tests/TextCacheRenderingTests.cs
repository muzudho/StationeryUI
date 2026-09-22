using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Inspection;
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
        using var inspectorTarget = new RenderTarget2D(GraphicsDevice, 1000, 660);
        using var inspector = new StationeryDeveloperView(GraphicsDevice, input, family => new WindowsTextRasterizer(family));
        inspector.Refresh([
            new("demo", "/demo", null, "viewport", "Demo", true, new(0, 0, 800, 600)),
            new("body", "/demo/body", "/demo", "container", "Margin / padding", true, new(10, 10, 780, 580))
            { BoxModel = new(new(10, 10, 10, 10), new(5, 5, 5, 5)), LayoutTypes = ["grid-layout"],
              LayoutNodes = [
                new("grid", "/demo/body:/grid", "/demo/body", "layout", "/grid", true, null)
                { LayoutTypes = ["grid-layout"], BoxModel = new(new(10, 10, 10, 10), new(5, 5, 5, 5)) },
                new("box", "/demo/body:/grid/box", "/demo/body:/grid", "layout", "/grid/box", true, null)
                { LayoutTypes = ["box-layout"], Cell = new(0, 0, 1, 1), BoxModel = new(new(6, 6, 6, 6), new(24, 24, 24, 24)) }
              ] }
        ]);
        inspector.SelectCaptured("/demo/body");
        Directory.CreateDirectory("artifacts/text-cache-test");
        foreach (var theme in new[] { StationeryTheme.Dark, StationeryTheme.Light })
        {
            inspector.Theme = theme;
            inspector.Update(new GameTime(), false, new(), new(), 1000, 660);
            GraphicsDevice.SetRenderTarget(inspectorTarget);
            GraphicsDevice.Clear(Color.Black);
            inspector.Draw();
            GraphicsDevice.SetRenderTarget(null);
            using var output = File.Create("artifacts/text-cache-test/box-model-" + (theme == StationeryTheme.Dark ? "dark" : "light") + ".png");
            inspectorTarget.SaveAsPng(output, 1000, 660);
        }
        void Click(StationeryUI.Canvas.ScreenRectangle bounds)
        {
            var x = (int)(bounds.X + bounds.Width / 2);
            var y = (int)(bounds.Y + bounds.Height / 2);
            inspector.Update(new GameTime(), true, new(), new(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), 1000, 660);
            inspector.Update(new GameTime(), true, new(), new(x, y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), 1000, 660);
            inspector.Update(new GameTime(), true, new(), new(x, y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), 1000, 660);
        }
        Click(inspector.LayoutTreeButtonBounds);
        if (inspector.Model.TreeMode != DeveloperTreeMode.Layout || !inspector.Model.Select("/demo/body:/grid/box"))
            throw new Exception("Layout tree button did not expose nested layouts.");
        if (inspector.Model.Tree.SelectedItem!.Parent!.Label != "(body : Container) (- : gridLayout)"
            || inspector.Model.Tree.VisibleRows().Count != 3)
            throw new Exception("Owning model and root layout were not merged.");
        inspector.Update(new GameTime(), true, new(), new(), 1000, 660);
        if (inspector.TreeBounds.Y < inspector.LayoutTreeButtonBounds.Y + inspector.LayoutTreeButtonBounds.Height)
            throw new Exception("Toolbar overlaps tree.");
        GraphicsDevice.SetRenderTarget(inspectorTarget);
        GraphicsDevice.Clear(Color.Black);
        inspector.Draw();
        GraphicsDevice.SetRenderTarget(null);
        using (var output = File.Create("artifacts/text-cache-test/layout-tree.png"))
            inspectorTarget.SaveAsPng(output, 1000, 660);
        Click(inspector.ModelTreeButtonBounds);
        if (inspector.Model.TreeMode != DeveloperTreeMode.Model || inspector.Model.Tree.SelectedItem!.Label != "(body : Container)")
            throw new Exception("Model tree button did not restore simple model labels.");
        using var outlineHost = new StationeryUiHost(GraphicsDevice, input, family => new WindowsTextRasterizer(family));
        foreach (var scale in new[] { 1.0, 1.5 })
        {
            outlineHost.Viewport.Scale = scale;
            outlineHost.Viewport.Offset = new(17, 29);
            StationeryInspectionEntry[] outlineSnapshot = [
                new("owner", "/owner", null, "container", "", true, null) {
                    LayoutNodes = [new("box", "/owner:/grid/box", "/owner:/grid", "layout", "", true, new(40, 50, 180, 120)) { MarginBounds = new(30, 40, 190, 130), BoxModel = new(new(10, 0, 0, 10), default), PartitionLines = [new(new(120, 60), new(120, 157)), new(new(50, 110), new(207, 110))] }]
                }
            ];
            var selected = DeveloperInspectionLayout.FindVisibleEntry(outlineSnapshot, "/owner:/grid/box")!;
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Black);
            outlineHost.DrawInspectionSelection(selected);
            GraphicsDevice.SetRenderTarget(null);
            var pixels = new Color[640 * 480];
            target.GetData(pixels);
            var pink = new Color(255, 105, 180);
            for (var y = 0; y < 480; y++)
            for (var x = 0; x < 640; x++)
            {
                var edge = x >= 40 && x < 220 && y >= 50 && y < 170
                    && (x < 43 || x >= 217 || y < 53 || y >= 167);
                var dash = x >= 119 && x < 121 && y >= 60 && y < 157 && (y - 60) % 10 < 5
                    || y >= 109 && y < 111 && x >= 50 && x < 207 && (x - 50) % 10 < 5;
                var marginEdge = y == 40 && x >= 30 && x < 220 || x == 30 && y >= 40 && y < 170;
                if (pixels[y * 640 + x] != (edge || dash || marginEdge ? pink : Color.Black))
                    throw new Exception($"Layout outline misplaced at {x}, {y}, scale {scale}.");
            }
        }
        using (var output = File.Create("artifacts/text-cache-test/partition-lines.png"))
            target.SaveAsPng(output, 640, 480);
        Console.WriteLine("PASS inspector box model, tree mode buttons, layout outlines and dashed partitions at 100%/150%.");
        Exit();
    }
}
