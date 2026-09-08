using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.MonoGame;
using StationeryUI.Theming;
using StationeryUI.Windows;

internal static class Program
{
    [STAThread]
    private static void Main() { using var game = new Demo(); game.Run(); }
}

internal sealed class Demo : Game
{
    private int smokeFrames;
    private readonly GraphicsDeviceManager manager;
    private WindowsTextInputService? input;
    private DesktopUi? ui;
    private DesktopUi.Element? name;
    private DesktopUi.Element? memo;
    public Demo()
    {
        manager = new(this) { PreferredBackBufferWidth = 1000, PreferredBackBufferHeight = 620 };
        Window.AllowUserResizing = true;
        Window.Title = "StationeryUI — 日本語入力とテーマ";
        IsMouseVisible = true;
    }
    protected override void LoadContent()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG")))
        {
            using var batch = new Microsoft.Xna.Framework.Graphics.SpriteBatch(GraphicsDevice);
            using var cache = new RasterTextRenderer(GraphicsDevice, batch, new WindowsTextRasterizer());
            batch.Begin();
            for (var i = 0; i < 140; i++) cache.Draw($"cache {i}", new Rectangle(0, 0, 10, 10), Color.White);
            batch.End();
            cache.EndFrame();
        }
        input = new(Window.Handle);
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family));
        if (Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_THEME") == "light") ui.Theme = StationeryTheme.Light;
        if (double.TryParse(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_SCALE"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var smokeScale)) ui.Viewport.Scale = smokeScale;
        name = ui.AddTextBox("name", new(32, 40, 700, 64), "名前", "文房具UIへようこそ");
        memo = ui.AddTextBox("memo", new(32, 130, 700, 64), "メモ", "日本語・結合文字 e\u0301 を編集できます");
        ui.AddButton("theme", new(32, 230, 300, 64), "明るい／暗いテーマ", () => ui.Theme = ui.Theme == StationeryTheme.Dark ? StationeryTheme.Light : StationeryTheme.Dark);
        ui.AddButton("scale", new(360, 230, 270, 64), "拡大率を変える", () => ui.Viewport.Scale = ui.Viewport.Scale >= 1.5 ? 1 : ui.Viewport.Scale + .25);
        ui.AddButton("accept", new(32, 330, 600, 64), "入力内容をタイトルに反映", () => Window.Title = name.Editor!.Text);
        ui.Focus.Focus("name");
    }
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState(); var mouse = Mouse.GetState();
        if (ui is not null)
        {
            if (name is not null) name.Bounds = name.Bounds with { Width = Math.Max(160, GraphicsDevice.Viewport.Width / ui.Viewport.Scale - 64) };
            if (memo is not null) memo.Bounds = memo.Bounds with { Width = Math.Max(160, GraphicsDevice.Viewport.Width / ui.Viewport.Scale - 64) };
            ui.Update(gameTime, IsActive, keyboard, mouse);
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(DesktopUi.Convert(ui?.Theme.Background ?? StationeryTheme.Dark.Background));
        ui?.Draw();
        var screenshot = Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG");
        if (!string.IsNullOrEmpty(screenshot) && ++smokeFrames == 5)
        {
            var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height];
            GraphicsDevice.GetBackBufferData(pixels);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice,width,height);
            texture.SetData(pixels);
            using var output = File.Create(screenshot);
            texture.SaveAsPng(output,width,height);
            Exit();
        }
        base.Draw(gameTime);
    }
    protected override void UnloadContent() { ui?.Dispose(); input?.Dispose(); base.UnloadContent(); }
}
