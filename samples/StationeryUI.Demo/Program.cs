using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.MonoGame;
using StationeryUI.Theming;
using StationeryUI.Windows;
using StationeryUI.Styling;
using System.Reflection;
using StationeryUI.Inspection;
using StationeryUI.Controls;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--stationery-inspector")
        {
            using var inspector = new InspectorGame(args[1]); inspector.Run();
        }
        else { using var game = new Demo(); game.Run(); }
    }
}

internal sealed partial class Demo : Game
{
    private int smokeFrames;
    private readonly GraphicsDeviceManager manager;
    private WindowsTextInputService? input;
    private StationeryUiHost? ui;
    private StationeryUiHost.Element? name;
    private StationeryUiHost.Element? memo;
    private StationeryUiHost.Element? sampleTree;
    private StationeryUiHost.Element? popupLink;
    private StationeryUiHost? popupUi;
    private StationeryUiHost.Element? popupText;
    private ActionBadgeOverlay? badges;
    private bool popupOpen;
    private string popupValue = "ダイアログで編集するテキスト";
    private Point pointer;
    private int updateFrames;
    private StationeryStyleFile styles = null!;
    private readonly List<StationeryUiHost.Element> styledElements = [];
    private double requestedScale = 1;
    private bool previousReloadKey;
    private bool hasContentArea;
    private string? reportedStyleError;
    private DemoModelBinding modelBinding = null!;
    private StationeryStyleSettings? appliedStyle;
    private readonly StationeryDeveloperWindow developerWindow = new();
    private bool previousDeveloperKey;
    private double inspectionElapsed;
    public Demo()
    {
        manager = new(this) { PreferredBackBufferWidth = 1000, PreferredBackBufferHeight = 780 };
        Window.AllowUserResizing = true;
        // MonoGame 3.8.5.1 recreates the SDL window using an ANSI-marshaled title.
        // Keep this initial title ASCII; set Japanese after native window creation.
        Window.Title = "StationeryUI";
        IsMouseVisible = true;
    }
    protected override void LoadContent()
    {
        // The existing-window title setter encodes UTF-8 correctly.
        Window.Title = "StationeryUI — ホバーで EDIT / POPUP、クリックで編集";
        var source = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "StationeryConfigurationSource")?.Value;
        var configurationPath = Environment.GetEnvironmentVariable("STATIONERYUI_CONFIG_PATH");
        if (string.IsNullOrWhiteSpace(configurationPath))
            configurationPath = source is not null && Directory.Exists(Path.GetDirectoryName(source))
                ? source : Path.Combine(AppContext.BaseDirectory, "App_Data", "demo.stationery-config.json");
        styles = new(configurationPath, DemoModelBinding.Fallback, settings => { _ = DemoModelBinding.Create(settings); });
        modelBinding = DemoModelBinding.Create(styles.Current);
        appliedStyle = styles.Current;
        System.Diagnostics.Trace.WriteLine($"StationeryUI configuration: {styles.ConfigurationFilePath}");
        System.Diagnostics.Trace.WriteLine($"StationeryUI style: {styles.FilePath}");
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
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family), modelBinding.TopPage);
        badges = new(GraphicsDevice);
        popupUi = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family), modelBinding.Dialog);
        popupText = popupUi.AddTextBox(modelBinding.DialogControls["nameField"], new(32, 70, 736, 64), "ダイアログのテキスト");
        popupUi.AddButton(modelBinding.DialogControls["cancelButton"], new(32, 180, 320, 64), "キャンセル", () => popupOpen = false);
        popupUi.AddButton(modelBinding.DialogControls["saveButton"], new(380, 180, 388, 64), "保存して閉じる", () =>
        {
            popupValue = popupText.Editor!.Text;
            popupLink!.Label = popupValue;
            popupOpen = false;
        });
        if (Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_THEME") == "light") ui.Theme = StationeryTheme.Light;
        if (double.TryParse(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_SCALE"),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var smokeScale)
            && double.IsFinite(smokeScale) && smokeScale > 0) requestedScale = smokeScale;
        name = ui.AddTextBox(modelBinding.Main["nameField"], new(32, 40, 700, 64), "名前", "文房具UIへようこそ");
        memo = ui.AddTextBox(modelBinding.Main["memoField"], new(32, 130, 700, 64), "メモ", "日本語・結合文字 e\u0301 を編集できます");
        var themeButton = ui.AddButton(modelBinding.Main["themeButton"], new(32, 230, 300, 64), "明るい／暗いテーマ", () => ui.Theme = ui.Theme == StationeryTheme.Dark ? StationeryTheme.Light : StationeryTheme.Dark);
        var scaleButton = ui.AddButton(modelBinding.Main["scaleButton"], new(360, 230, 270, 64), "拡大率を変える", () => requestedScale = requestedScale >= 1.5 ? 1 : requestedScale + .25);
        var acceptButton = ui.AddButton(modelBinding.Main["applyTitleButton"], new(32, 330, 600, 64), "入力内容をタイトルに反映", () => Window.Title = name.Editor!.Text);
        popupLink = ui.AddButton(modelBinding.Main["openDialogButton"], new(32, 420, 700, 64), popupValue, () =>
        {
            popupText.Editor!.SelectAll();
            popupText.Editor.Insert(popupValue);
            popupUi.Focus.Focus(popupText.Path);
            popupOpen = true;
        });
        var tree = new TreeView();
        var stationery = tree.AddNode("stationery", "文房具");
        var writing = tree.AddNode("writing", "筆記用具", stationery);
        tree.AddNode("pencil", "鉛筆", writing);
        tree.AddNode("pen", "ボールペン", writing);
        var paper = tree.AddNode("paper", "紙製品", stationery, expanded: false);
        tree.AddNode("notebook", "ノート", paper);
        tree.AddNode("stickyNote", "付箋", paper);
        sampleTree = ui.AddTree(modelBinding.Main["sampleTree"], new(0, 0, 400, 160), "文房具のツリー", tree);
        styledElements.AddRange(new[] { name, memo, themeButton, scaleButton, acceptButton, popupLink, sampleTree });
        CreatePages();
        ApplyStyles();
        // 起動時は未編集にして、名前・メモのホバーバッジを試せるようにする。
    }

    private void ApplyStyles()
    {
        if (!ReferenceEquals(appliedStyle, styles.Current))
        {
            var next = DemoModelBinding.Create(styles.Current);
            if (next.Signature != modelBinding.Signature)
            {
                ui!.RebindModel(next.TopPage, element => next.Main[element.Id]);
                popupUi!.RebindModel(next.Dialog, element => next.DialogControls[element.Id]);
                splitUi!.RebindModel(next.SplitPage, element => next.SplitControls[element.Id]);
                modelBinding = next;
            }
            appliedStyle = styles.Current;
        }
        var arranged = StationeryLayoutEngine.Arrange(styles.Current, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        var content = arranged.ContentBounds[modelBinding.Root.Path];
        hasContentArea = content.Width >= 1 && content.Height >= 1;
        if (!hasContentArea) return;
        // Layout owns window-pixel rectangles. UI zoom changes text rendering within those rectangles.
        ui!.Viewport.Scale = requestedScale;
        ui.Viewport.Offset = new(0, 0);
        foreach (var element in styledElements)
        {
            var bounds = arranged.Bounds[element.Path];
            element.Bounds = new(bounds.X / requestedScale, bounds.Y / requestedScale,
                bounds.Width / requestedScale, bounds.Height / requestedScale);
        }
        ApplySplitStyles(arranged);
        popupUi!.Viewport.Scale = Math.Min(1, Math.Min(content.Width / 800, content.Height / 320));
        popupUi.Viewport.Offset = new(content.X + (content.Width - 800 * popupUi.Viewport.Scale) / 2,
            content.Y + (content.Height - 320 * popupUi.Viewport.Scale) / 2);
    }
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState(); var mouse = Mouse.GetState();
        var reloadKey = keyboard.IsKeyDown(Keys.F5);
        if (IsActive && reloadKey && !previousReloadKey) styles.Reload();
        else styles.Update(gameTime.ElapsedGameTime);
        previousReloadKey = reloadKey;
        if (styles.LastError != reportedStyleError)
        {
            reportedStyleError = styles.LastError;
            System.Diagnostics.Trace.WriteLine(reportedStyleError ?? "StationeryUI style reload succeeded.");
            Window.Title = reportedStyleError is null ? "StationeryUI — スタイル読み込み成功" : "StationeryUI — スタイル読み込み失敗（F5 で再試行）";
        }
        ApplyStyles();
        var developerKey = keyboard.IsKeyDown(Keys.F12);
        if (IsActive && developerKey && !previousDeveloperKey) developerWindow.Show(InspectStationery());
        previousDeveloperKey = developerKey;
        inspectionElapsed += gameTime.ElapsedGameTime.TotalSeconds;
        if (developerWindow.IsOpen && inspectionElapsed >= .25)
        {
            developerWindow.Update(InspectStationery());
            inspectionElapsed = 0;
        }
        var smokeCase = Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_CASE");
        var smoke = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG"));
        updateFrames++;
        if (ui is not null)
        {
            popupUi!.Theme = ui.Theme;
            if (!hasContentArea)
            {
                ui.Update(gameTime, false, keyboard, mouse);
                popupUi.Update(gameTime, false, keyboard, mouse);
                splitUi!.Update(gameTime, false, keyboard, mouse);
                base.Update(gameTime);
                return;
            }
            if (smoke && smokeCase is not null)
            {
                if (smokeCase.StartsWith("tree-", StringComparison.Ordinal))
                {
                    var bounds = ui.Viewport.ToWindow(sampleTree!.Bounds);
                    var outside = smokeCase == "tree-cancel" && updateFrames >= 3;
                    var down = smokeCase is not ("tree-scroll" or "tree-end") &&
                        (updateFrames == 2 || smokeCase == "tree-reopen" && updateFrames == 4);
                    mouse = new MouseState((int)(outside ? bounds.X - 10 : bounds.X + 20 * ui.Viewport.Scale),
                        (int)(bounds.Y + 16 * ui.Viewport.Scale), smokeCase == "tree-scroll" && updateFrames >= 2 ? -120 : 0,
                        down ? ButtonState.Pressed : ButtonState.Released,
                        ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                    if (smokeCase is "tree-keyboard" or "tree-end")
                    {
                        ui.Focus.Focus(sampleTree.Path);
                        mouse = new MouseState();
                        keyboard = updateFrames == 2 ? new KeyboardState(smokeCase == "tree-end" ? Keys.End : Keys.Left) : new KeyboardState();
                    }
                    if (smokeCase is "tree-drag" or "tree-drag-up" or "tree-track")
                    {
                        var dragging = smokeCase != "tree-track";
                        var returnUp = smokeCase == "tree-drag-up" && updateFrames >= 4;
                        var moving = updateFrames >= 3;
                        var y = !dragging ? bounds.Y + bounds.Height - 3
                            : returnUp ? bounds.Y - 30
                            : moving && updateFrames <= 4 ? bounds.Y + bounds.Height + 30 : bounds.Y + 10;
                        mouse = new MouseState((int)(moving && dragging ? bounds.X + bounds.Width + 30 : bounds.X + bounds.Width - 8),
                            (int)y, 0,
                            (updateFrames == 2 || dragging && updateFrames == 3 || smokeCase == "tree-drag-up" && updateFrames == 4)
                                ? ButtonState.Pressed : ButtonState.Released,
                            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                    }
                }
                else
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
            }
            PreparePageSmoke(smokeCase, smoke, ref keyboard, ref mouse);
            pointer = mouse.Position;
            if (activePage == "splitPaneDemoPage")
            {
                ui.Update(gameTime, false, keyboard, mouse);
                popupUi.Update(gameTime, false, keyboard, mouse);
                splitUi!.Update(gameTime, IsActive || smoke, keyboard, mouse);
                if (activePage != "splitPaneDemoPage") splitUi.Update(gameTime, false, keyboard, mouse);
            }
            else if (popupOpen)
            {
                popupUi.Update(gameTime, IsActive || smoke, keyboard, mouse);
                if (!popupOpen) popupUi.Update(gameTime, false, keyboard, mouse);
            }
            else
            {
                splitUi!.Update(gameTime, false, keyboard, mouse);
                ui.Update(gameTime, IsActive || smoke, keyboard, mouse);
                // モーダルへ入力の所有権を渡す前に、元のテキスト入力を終了する。
                if (popupOpen || activePage != "topDemoPage") ui.Update(gameTime, false, keyboard, mouse);
            }
        }
        UpdateToolHints();
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(StationeryUiHost.Convert(ui?.Theme.Background ?? StationeryTheme.Dark.Background));
        var smoke = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_SMOKE_PNG"));
        if (hasContentArea && activePage == "splitPaneDemoPage") splitUi?.Draw();
        else if (hasContentArea && popupOpen)
        {
            ui?.Draw();
            badges?.DrawDialogBackground(popupUi!);
            popupUi?.Draw();
        }
        else if (hasContentArea)
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
            ValidatePageSmoke(smokeCase);
            if (smokeCase is not null && smokeCase.StartsWith("tree-", StringComparison.Ordinal))
            {
                var collapsed = smokeCase is "tree-close" or "tree-keyboard";
                if (sampleTree!.Tree!.Roots[0].IsExpanded == collapsed ||
                    sampleTree.Tree.VisibleRows().Count != (collapsed ? 1 : 5))
                    throw new InvalidOperationException($"Failed tree smoke scenario: {smokeCase}");
                var leaf = ui!.Inspect().Single(entry => entry.Path.EndsWith("/stationery/writing/pencil", StringComparison.Ordinal));
                if (collapsed && leaf.Visible) throw new InvalidOperationException("Collapsed tree descendant remained visible in inspector.");
                if (smokeCase is "tree-scroll" or "tree-end" or "tree-drag" or "tree-track")
                {
                    var last = ui.Inspect().Single(entry => entry.Path.EndsWith("/stationery/paper", StringComparison.Ordinal));
                    if (!last.Visible || smokeCase == "tree-end" && sampleTree.Tree.SelectedItem?.Id != "paper")
                        throw new InvalidOperationException("Tree scrolling failed to reveal the last node.");
                }
                if (smokeCase is "tree-drag" or "tree-drag-up")
                {
                    var rootEntry = ui.Inspect().Single(entry => entry.Path.EndsWith("/stationery", StringComparison.Ordinal));
                    if (rootEntry.Visible != (smokeCase == "tree-drag-up") || ui.Focus.CapturedId is not null)
                        throw new InvalidOperationException("Tree thumb drag/clamp/release failed.");
                }
            }
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
    private IReadOnlyList<StationeryInspectionEntry> InspectStationery()
    {
        var entries = ui!.Inspect(hasContentArea && activePage == "topDemoPage").ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        foreach (var entry in splitUi!.Inspect(hasContentArea && activePage == "splitPaneDemoPage")) entries[entry.Path] = entry;
        foreach (var entry in popupUi!.Inspect(hasContentArea && popupOpen && activePage == "topDemoPage")) entries[entry.Path] = entry;
        var root = modelBinding.Root;
        entries[root.Path] = new(root.Id, root.Path, null, root.Kind, "デモ画面", true,
            new ScreenRectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
        return entries.Values.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) developerWindow.Dispose();
        base.Dispose(disposing);
    }

    protected override void UnloadContent() { splitUi?.Dispose(); popupUi?.Dispose(); badges?.Dispose(); ui?.Dispose(); input?.Dispose(); base.UnloadContent(); }
}
