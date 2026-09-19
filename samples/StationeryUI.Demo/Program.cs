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
    private DesktopUi.Element? popupLink;
    private DesktopUi? popupUi;
    private DesktopUi.Element? popupText;
    private ActionBadgeOverlay? badges;
    private bool popupOpen;
    private string popupValue = "ダイアログで編集するテキスト";
    private Point pointer;
    private int updateFrames;
    public Demo()
    {
        manager = new(this) { PreferredBackBufferWidth = 1000, PreferredBackBufferHeight = 780 };
        Window.AllowUserResizing = true;
        Window.Title = "StationeryUI — ホバーで EDIT / POPUP、クリックで編集";
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
        badges = new(GraphicsDevice);
        popupUi = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family));
        popupText = popupUi.AddTextBox("popup-text", new(32, 70, 736, 64), "ダイアログのテキスト");
        popupUi.AddButton("cancel", new(32, 180, 320, 64), "キャンセル", () => popupOpen = false);
        popupUi.AddButton("save", new(380, 180, 388, 64), "保存して閉じる", () =>
        {
            popupValue = popupText.Editor!.Text;
            popupLink!.Label = popupValue;
            popupOpen = false;
        });
        if (Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_THEME") == "light") ui.Theme = StationeryTheme.Light;
        if (double.TryParse(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_SCALE"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var smokeScale)) ui.Viewport.Scale = smokeScale;
        name = ui.AddTextBox("name", new(32, 40, 700, 64), "名前", "文房具UIへようこそ");
        memo = ui.AddTextBox("memo", new(32, 130, 700, 64), "メモ", "日本語・結合文字 e\u0301 を編集できます");
        ui.AddButton("theme", new(32, 230, 300, 64), "明るい／暗いテーマ", () => ui.Theme = ui.Theme == StationeryTheme.Dark ? StationeryTheme.Light : StationeryTheme.Dark);
        ui.AddButton("scale", new(360, 230, 270, 64), "拡大率を変える", () => ui.Viewport.Scale = ui.Viewport.Scale >= 1.5 ? 1 : ui.Viewport.Scale + .25);
        ui.AddButton("accept", new(32, 330, 600, 64), "入力内容をタイトルに反映", () => Window.Title = name.Editor!.Text);
        popupLink = ui.AddButton("popup", new(32, 420, 700, 64), popupValue, () =>
        {
            popupText.Editor!.SelectAll();
            popupText.Editor.Insert(popupValue);
            popupUi.Focus.Focus("popup-text");
            popupOpen = true;
        });
        // 起動時は未編集にして、名前・メモのホバーバッジを試せるようにする。
    }
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState(); var mouse = Mouse.GetState();
        var smokeCase = Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_CASE");
        var smoke = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG"));
        updateFrames++;
        if (ui is not null)
        {
            if (name is not null) name.Bounds = name.Bounds with { Width = Math.Max(160, GraphicsDevice.Viewport.Width / ui.Viewport.Scale - 64) };
            if (memo is not null) memo.Bounds = memo.Bounds with { Width = Math.Max(160, GraphicsDevice.Viewport.Width / ui.Viewport.Scale - 64) };
            if (popupLink is not null) popupLink.Bounds = popupLink.Bounds with { Width = Math.Max(160, GraphicsDevice.Viewport.Width / ui.Viewport.Scale - 64) };
            popupUi!.Theme = ui.Theme;
            popupUi.Viewport.Scale = Math.Min(1.0, Math.Min(GraphicsDevice.Viewport.Width / 800.0, GraphicsDevice.Viewport.Height / 320.0));
            popupUi.Viewport.Offset = new((GraphicsDevice.Viewport.Width - 800 * popupUi.Viewport.Scale) / 2,
                (GraphicsDevice.Viewport.Height - 320 * popupUi.Viewport.Scale) / 2);
            if (smoke && smokeCase is not null)
            {
                var target = smokeCase == "edit-hover" ? name! : popupLink!;
                var bounds = ui.Viewport.ToWindow(target.Bounds);
                var dialogCase = smokeCase is "popup-open" or "popup-save" or "popup-cancel";
                if (smokeCase is "popup-save" or "popup-cancel" && updateFrames >= 4)
                {
                    bounds = popupUi.Viewport.ToWindow(new(smokeCase == "popup-save" ? 380 : 32, 180, 300, 64));
                    if (updateFrames == 4)
                    {
                        popupText!.Editor!.SelectAll();
                        popupText.Editor.Insert("保存とキャンセルの検証");
                    }
                }
                mouse = new MouseState((int)(bounds.X + 40), (int)(bounds.Y + 25), 0,
                    dialogCase && updateFrames == 2 || smokeCase is "popup-save" or "popup-cancel" && updateFrames == 5
                        ? ButtonState.Pressed : ButtonState.Released,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            }
            pointer = mouse.Position;
            if (popupOpen)
            {
                popupUi.Update(gameTime, IsActive || smoke, keyboard, mouse);
                if (!popupOpen) popupUi.Update(gameTime, false, keyboard, mouse);
            }
            else
            {
                ui.Update(gameTime, IsActive || smoke, keyboard, mouse);
                // モーダルへ入力の所有権を渡す前に、元のテキスト入力を終了する。
                if (popupOpen) ui.Update(gameTime, false, keyboard, mouse);
            }
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(DesktopUi.Convert(ui?.Theme.Background ?? StationeryTheme.Dark.Background));
        var smoke = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG"));
        if (popupOpen)
        {
            ui?.Draw();
            badges?.DrawDialogBackground(popupUi!);
            popupUi?.Draw();
        }
        else
        {
            ui?.Draw();
            if (ui is not null && badges is not null)
            {
                badges.Draw(ui, name!, "EDIT", pointer, IsActive || smoke);
                badges.Draw(ui, memo!, "EDIT", pointer, IsActive || smoke);
                badges.Draw(ui, popupLink!, "POPUP", pointer, IsActive || smoke);
            }
        }
        var screenshot = Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG");
        if (!string.IsNullOrEmpty(screenshot) && ++smokeFrames == 8)
        {
            var smokeCase = Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_CASE");
            if (smokeCase == "popup-open" && !popupOpen
                || smokeCase == "popup-save" && (popupOpen || popupValue != "保存とキャンセルの検証")
                || smokeCase == "popup-cancel" && (popupOpen || popupValue != "ダイアログで編集するテキスト"))
                throw new InvalidOperationException($"Failed demo smoke scenario: {smokeCase}");
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
    protected override void UnloadContent() { popupUi?.Dispose(); badges?.Dispose(); ui?.Dispose(); input?.Dispose(); base.UnloadContent(); }
}
