using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.Editor;
using StationeryUI.Theming;
using StationeryUI.Windows;
using System.Globalization;
using System.Text.Json;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            if (args.Length > 1) throw new ArgumentException("引数には開く文房具UIファイルを1つ指定してください。");
            using var game = new EditorGame(args.FirstOrDefault());
            game.Run();
        }
        catch (Exception ex)
        {
            var testOutput = Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_OUTPUT");
            if (!string.IsNullOrEmpty(testOutput)) File.WriteAllText(Path.Combine(testOutput, "error.txt"), ex.ToString());
            throw;
        }
    }
}

internal sealed partial class EditorGame : Game
{
    private static readonly string AppVersion = typeof(EditorGame).Assembly.GetName().Version!.ToString(3);
    private readonly GraphicsDeviceManager manager;
    private readonly EditorOperationLog operationLog;
    private readonly System.Drawing.Rectangle startupWorkArea;
    private readonly int startupFrameWidth, startupFrameHeight;
    private StyleBlueprint blueprint = new();
    private WindowsTextInputService input = null!;
    private StationeryUiHost ui = null!;
    private StationeryUiHost.Element columns = null!, rows = null!, status = null!, output = null!;
    private readonly List<(StationeryUiHost.Element Field, StationeryUiHost.Element Unit, StyleBlueprint.Track Track)> tracks = [];
    private StationeryCompoundInput? compoundInput;
    private int selectedRow, selectedColumn;
    private bool rebuild;
    private string message = "列数・行数とサイズを指定し、エクスポートから保存先を設定できます。";
    private string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my-plan.stationery-ui.json");
    private StationeryTheme theme = StationeryTheme.Light with { FontSize = 16, Padding = 4 };
    private int frames;
    private ButtonState previousLoggedLeftButton;
    private readonly string? smokeOutput = Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_OUTPUT");
    private bool PageSmoke => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_PAGES"));
    private bool DeletePageSmoke => Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_PAGES") == "delete";
    private StationeryUiHost.Element pageButton = null!;
    private readonly string? startupFile;

    public EditorGame(string? startupFile = null)
    {
        this.startupFile = startupFile;
        operationLog = new();
        // WorkingArea excludes the taskbar, including taskbars on the top or left.
        startupWorkArea = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        startupFrameWidth = Math.Max(32, System.Windows.Forms.SystemInformation.FrameBorderSize.Width * 2);
        startupFrameHeight = Math.Max(96, System.Windows.Forms.SystemInformation.CaptionHeight + System.Windows.Forms.SystemInformation.FrameBorderSize.Height * 2);
        var scale = Math.Min(1, Math.Min(
            Math.Max(1, startupWorkArea.Width * .9 - startupFrameWidth) / 1600.0,
            Math.Max(1, startupWorkArea.Height * .9 - startupFrameHeight) / 900.0));
        manager = new(this)
        {
            PreferredBackBufferWidth = Math.Max(1, (int)(1600 * scale)),
            PreferredBackBufferHeight = Math.Max(1, (int)(900 * scale))
        };
        Window.Title = "StationeryUIEditor";
        Window.AllowUserResizing = true; IsMouseVisible = true;
        Exiting += (_, args) => { if (utilityDialog is not null || layoutDialog is not null || restoreDialog is not null || !FlushAutoSave()) args.Cancel = true; };
    }
    protected override void Initialize()
    {
        base.Initialize();
        Window.Position = new(
            startupWorkArea.Left + Math.Max(0, (startupWorkArea.Width - Window.ClientBounds.Width - startupFrameWidth) / 2),
            startupWorkArea.Top + Math.Max(0, (startupWorkArea.Height - Window.ClientBounds.Height - startupFrameHeight) / 2));
    }
    protected override void LoadContent()
    {
        Window.Title = $"文房具UIエディター v{AppVersion}";
        input = new(Window.Handle);
        BuildInspector();
        BuildApplicationBar();
        if (!string.IsNullOrEmpty(smokeOutput) && Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_DARK") == "1")
            theme = StationeryTheme.Dark with { FontSize = 16, Padding = 4 };
        BuildWelcome();
        if (startupFile is not null) Guard(() => OpenStyle(Path.GetFullPath(startupFile)));
    }
    private StationeryUiHost.Element Text(string id, ScreenRectangle bounds, string text) =>
        ui.AddTextBlock(ui.Root.AddChild(id, "textBlock"), bounds, text);
    private void SetText(StationeryUiHost.Element field, string text)
    {
        field.Editor!.SelectAll();
        if (text.Length == 0) field.Editor.Delete(backward: true);
        else field.Editor.Insert(text);
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        { message = ex.Message; }
    }
    private void Capture()
    {
        if (editingPage && panelFields.Count > 0)
            foreach (var (field, key) in panelFields) blueprint.PanelEdges[key].Number = field.Editor!.Text;
        if (!editingPage || !gridEditorVisible) return;
        foreach (var (field, _, track) in tracks) track.Number = field.Editor!.Text;
    }
    private void BuildUi()
    {
        ui?.Dispose(); tracks.Clear(); panelFields.Clear(); ResetLivePreview();
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
        gridEditorVisible = false;
        BuildSidebar();
        if (!HasLayoutTarget) return;
        if (DirectLayoutTargetId is null) { BuildReadOnly(); return; }
        if (blueprint.CanEditPanel)
        {
            BuildGridInsetsEditor(new(12, 136, 524, 226));
            BuildLivePreviewHeader();
            BuildSidebar();
            return;
        }
        if (!blueprint.CanEditGrid) { BuildReadOnly(); return; }
        gridEditorVisible = true;
        var propertyRowHeight = Math.Max(32, (int)Math.Ceiling(theme.FontSize * 1.5 + 4 + theme.Padding * 2));
        var propertyOffset = propertyRowHeight * 4 - 128;
        propNodeName = Text("propNodeName", new(12, 8, 524, propertyRowHeight), "文房具Ｉｄ: -");
        propKind = Text("propKind", new(12, 8 + propertyRowHeight, 524, propertyRowHeight), "種類: -");
        propPosition = Text("propPosition", new(12, 8 + propertyRowHeight * 2, 524, propertyRowHeight), "コンテナー内の位置: -");
        propLayout = Text("propLayout", new(12, 8 + propertyRowHeight * 3, 524, propertyRowHeight), "コンテナーとしてのレイアウト: -");
        UpdatePropertyPanel();

        Text("columnTracksTitle", new(12, 144 + propertyOffset, 252, 36), "列の幅");
        Text("rowTracksTitle", new(280, 144 + propertyOffset, 256, 36), "行の高さ");
        Text("columnsLabel", new(12, 184 + propertyOffset, 88, 44), "列数");
        columns = ui.AddTextBox("columns", new(104, 184 + propertyOffset, 160, 44), "列数", blueprint.Columns.Count.ToString());
        Text("rowsLabel", new(280, 184 + propertyOffset, 88, 44), "行数");
        rows = ui.AddTextBox("rows", new(372, 184 + propertyOffset, 164, 44), "行数", blueprint.Rows.Count.ToString());
        var trackBaseY = 236 + propertyOffset;
        for (var c = 0; c < blueprint.Columns.Count; c++)
        {
            Text($"columnIndex{c}", new(12, trackBaseY + c * 44, 36, 40), (c + 1).ToString());
            AddTrack(blueprint.Columns[c], $"column{c}", new(104, trackBaseY + c * 44, 160, 40), $"列 {c + 1} の幅");
        }
        for (var r = 0; r < blueprint.Rows.Count; r++)
        {
            Text($"rowIndex{r}", new(280, trackBaseY + r * 44, 36, 40), (r + 1).ToString());
            AddTrack(blueprint.Rows[r], $"row{r}", new(372, trackBaseY + r * 44, 164, 40), $"行 {r + 1} の高さ");
        }
        BuildGridInsetsEditor(new(12, 480 + 128 + propertyOffset, 524, 226));
        BuildLivePreviewHeader();
        BuildSidebar();
    }
    private void BuildGridInsetsEditor(ScreenRectangle bounds)
    {
        var values = blueprint.PanelEdges.ToDictionary(pair => pair.Key, pair => pair.Value.Number, StringComparer.Ordinal);
        compoundInput = ui.AddCompoundInsetsEditor("compound", bounds, values);
        panelFields.AddRange(compoundInput.Fields.Select(pair => (pair.Value, pair.Key)));
    }

    private void AddTrack(StyleBlueprint.Track track, string id, ScreenRectangle bounds, string name)
    {
        var field = ui.AddTextBox(id, bounds with { Width = bounds.Width - 60 }, name, track.Number, 24);
        StationeryUiHost.Element? unit = null;
        unit = ui.AddButton(id + "Unit", new(bounds.X + bounds.Width - 58, bounds.Y, 58, bounds.Height), track.IsRate ? "rate" : "px", () =>
        { track.IsRate = !track.IsRate; unit!.Label = track.IsRate ? "rate" : "px"; });
        tracks.Add((field, unit, track));
    }
    protected override void Update(GameTime gameTime)
    {
        var pageBefore = CurrentPageName;
        var mouse = Mouse.GetState();
        string? clickTarget = null;
        var clicked = false;
        if (IsActive && string.IsNullOrEmpty(smokeOutput) && mouse.LeftButton == ButtonState.Pressed
            && previousLoggedLeftButton == ButtonState.Released)
        {
            clicked = true;
            clickTarget = FindClickedPath(mouse.X, mouse.Y);
            operationLog.Record("click", new
            {
                Page = pageBefore,
                X = mouse.X,
                Y = mouse.Y,
                TargetPath = clickTarget,
                Document = string.IsNullOrEmpty(sourceFile) ? null : Path.GetFileName(sourceFile)
            });
        }
        previousLoggedLeftButton = mouse.LeftButton;
        try { UpdateFrame(gameTime); }
        finally
        {
            var pageAfter = CurrentPageName;
            if (pageBefore != pageAfter)
                operationLog.Record("page-transition", new
                {
                    From = pageBefore,
                    To = pageAfter,
                    Trigger = clicked ? new { X = mouse.X, Y = mouse.Y, TargetPath = clickTarget } : null,
                    Document = string.IsNullOrEmpty(sourceFile) ? null : Path.GetFileName(sourceFile)
                });
        }
    }

    private string CurrentPageName => layoutDialog is not null ? "layout-dialog"
        : restoreDialog is not null ? "restore-dialog"
        : utilityDialog is not null ? exportDialog ? "export-dialog" : "resize-confirm-dialog"
        : editingPage ? "editor" : "welcome";

    private string? FindClickedPath(int x, int y)
    {
        bool Contains(StationeryUI.Canvas.ScreenRectangle? bounds) => bounds is { } rect
            && x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height;
        var hosts = new List<StationeryUiHost>();
        if (layoutDialog is not null) hosts.Add(layoutDialog);
        else if (restoreDialog is not null) hosts.Add(restoreDialog);
        else if (utilityDialog is not null) hosts.Add(utilityDialog);
        else if (editingPage)
        {
            if (y < ApplicationBarHeight) hosts.Add(applicationBar);
            hosts.Add(sidebar!);
            hosts.Add(ui);
        }
        else hosts.Add(ui);
        hosts.Add(inspector);
        foreach (var host in hosts)
        {
            var hit = host.Inspect().LastOrDefault(entry => Contains(entry.WindowBounds));
            if (hit is not null) return hit.Path;
        }
        if (editingPage && Contains(previewWindow)) return "preview";
        return null;
    }

    private void UpdateFrame(GameTime gameTime)
    {
        var scale = BodyScale;
        if (utilityDialog is not null || restoreDialog is not null || layoutDialog is not null || pageDialog is not null)
            previewWasActive = false;
        if (utilityDialog is not null) { UpdateUtilityDialog(gameTime); base.Update(gameTime); return; }
        if (restoreDialog is not null) { UpdateRestoreDialog(gameTime); base.Update(gameTime); return; }
        if (layoutDialog is not null) { UpdateLayoutDialog(gameTime, scale); base.Update(gameTime); return; }
        if (pageDialog is not null) { UpdatePageDialog(gameTime, scale); base.Update(gameTime); return; }
        ui.Viewport.Scale = scale;
        ui.Viewport.Offset = editingPage ? new(320 * scale, BodyTop)
            : new((GraphicsDevice.Viewport.Width - 1600 * scale) / 2,
                (GraphicsDevice.Viewport.Height - InspectorHeight - 850 * scale) / 2);
        if (sidebar is not null) { sidebar.Viewport.Scale = scale; sidebar.Viewport.Offset = new(0, BodyTop); sidebar.Theme = theme; }
        var mouse = Mouse.GetState(); var keyboard = Keyboard.GetState();
        PrepareEditorSmoke(ref mouse, ref keyboard);
        inspectorMouse = mouse;
        var active = IsActive || !string.IsNullOrEmpty(smokeOutput);
        inspector.Update(gameTime, active, new(), mouse);
        if (active && editingPage && mouse.LeftButton == ButtonState.Pressed) sidebarActive = mouse.X < 320 * scale;
        if (editingPage)
        {
            ArrangeApplicationBar();
            if (active && mouse.LeftButton == ButtonState.Pressed) applicationBarActive = mouse.Y < ApplicationBarHeight;
            applicationBar.Update(gameTime, active, applicationBarActive ? keyboard : new(), mouse);
        }
        if (editingPage && sidebar is not null)
        {
            sidebar.Update(gameTime, active, sidebarActive && !applicationBarActive ? keyboard : new(), mouse);
            HandleTreeSelection();
        }
        ui.Update(gameTime, active, !editingPage || (!sidebarActive && !applicationBarActive) ? keyboard : new(), mouse);
        if (pendingPage is not null) { var action = pendingPage; pendingPage = null; action(); return; }
        if (!editingPage) { status.Label = message; base.Update(gameTime); return; }
        if (rebuild) { rebuild = false; BuildUi(); ui.Viewport.Scale = scale; ui.Viewport.Offset = new(320 * scale, BodyTop); }
        if (!gridEditorVisible)
        {
            Capture();
            try { var json = blueprint.BuildJson(); AutoSave(json, gameTime); RefreshTree(json); UpdateLivePreview(json, mouse, active); status.Label = message; }
            catch (JsonException ex) { invalidDraft = true; saveSession?.RestartTimer(); status.Label = "入力を確認してください：" + ex.Message; }
            base.Update(gameTime); return;
        }
        Capture();
        try
        {
            if (!ApplyGridCounts()) return;
            var json = blueprint.BuildJson();
            AutoSave(json, gameTime);
            RefreshTree(json);
            UpdateLivePreview(json, mouse, active);
            status.Label = message;
        }
        catch (JsonException ex)
        {
            invalidDraft = true;
            saveSession?.RestartTimer();
            ResetLivePreview();
            status.Label = "入力を確認してください：" + ex.Message;
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background)); ui.Draw();
        if (editingPage) { sidebar?.Draw(); DrawLivePreview(); }
        if (editingPage) { ArrangeApplicationBar(); applicationBar.Draw(); }
        DrawInspector();
        DrawModalBackdrop();
        layoutDialog?.Draw();
        pageDialog?.Draw();
        restoreDialog?.Draw();
        utilityDialog?.Draw();
        CaptureWelcomeSmoke();
        SavePickerScreenshot();
        CaptureUtilitySmoke();
        if (!string.IsNullOrEmpty(smokeOutput) && ++frames == (SaveSmoke || PageSmoke ? 150 : 32))
        {
            var data = new Microsoft.Xna.Framework.Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];
            GraphicsDevice.GetBackBufferData(data);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            texture.SetData(data);
            using var file = File.Create(Path.Combine(smokeOutput, "editor.png")); texture.SaveAsPng(file, texture.Width, texture.Height);
            ValidateEditorSmoke();
            VerifyUntargetedSmoke();
            if (Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG") != "1") blueprint.Export(Path.Combine(smokeOutput, "plan.stationery-ui.json"));
            Exit();
        }
        base.Draw(gameTime);
    }
    protected override void Dispose(bool disposing)
    { if (disposing) { operationLog.Dispose(); modalSprites?.Dispose(); modalPixel?.Dispose(); utilityDialog?.Dispose(); applicationBar?.Dispose(); restoreDialog?.Dispose(); inspector?.Dispose(); livePreview?.Dispose(); layoutDialog?.Dispose(); pageDialog?.Dispose(); ui?.Dispose(); sidebar?.Dispose(); input?.Dispose(); } base.Dispose(disposing); }
}
