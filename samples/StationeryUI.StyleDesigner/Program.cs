using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.StyleDesigner;
using StationeryUI.Theming;
using StationeryUI.Windows;
using System.Globalization;
using System.Text.Json;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try { using var game = new DesignerGame(); game.Run(); }
        catch (Exception ex)
        {
            var testOutput = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_OUTPUT");
            if (!string.IsNullOrEmpty(testOutput)) File.WriteAllText(Path.Combine(testOutput, "error.txt"), ex.ToString());
            throw;
        }
    }
}

internal sealed partial class DesignerGame : Game
{
    private static readonly string AppVersion = typeof(DesignerGame).Assembly.GetName().Version!.ToString(3);
    private readonly GraphicsDeviceManager manager;
    private readonly System.Drawing.Rectangle startupWorkArea;
    private readonly int startupFrameWidth, startupFrameHeight;
    private StyleBlueprint blueprint = new();
    private WindowsTextInputService input = null!;
    private StationeryUiHost ui = null!;
    private StationeryUiHost.Element columns = null!, rows = null!, label = null!, kind = null!, status = null!, output = null!;
    private readonly List<(StationeryUiHost.Element Field, StationeryUiHost.Element Unit, StyleBlueprint.Track Track)> tracks = [];
    private int selectedRow, selectedColumn;
    private bool rebuild;
    private (int Columns, int Rows)? pendingShrink;
    private string message = "セル数 → 表を作る → サイズと文房具を指定 → エクスポート";
    private string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my-plan.stationery-style.json");
    private StationeryTheme theme = StationeryTheme.Light with { FontSize = 16, Padding = 4 };
    private int frames;
    private readonly string? smokeOutput = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_OUTPUT");
    private bool smokeClicked;

    public DesignerGame()
    {
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
        Window.Title = "StationeryUI Style Designer";
        Window.AllowUserResizing = true; IsMouseVisible = true;
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
        Window.Title = $"文房具 UI — スタイル設計ツール v{AppVersion}";
        input = new(Window.Handle);
        if (!string.IsNullOrEmpty(smokeOutput) && Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_DARK") == "1")
            theme = StationeryTheme.Dark with { FontSize = 16, Padding = 4 };
        BuildWelcome();
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
        if (editingPage) outputPath = output.Editor!.Text;
        if (editingPage && blueprint.CanEditPanel)
            foreach (var (field, key) in panelFields) blueprint.PanelEdges[key].Number = field.Editor!.Text;
        if (!editingPage || !blueprint.CanEditGrid) return;
        foreach (var (field, _, track) in tracks) track.Number = field.Editor!.Text;
        if (!blueprint.IsImported) blueprint.At(selectedRow, selectedColumn).Label = label.Editor!.Text;
        outputPath = output.Editor!.Text;
    }
    private void BuildUi()
    {
        ui?.Dispose(); tracks.Clear(); panelFields.Clear(); ResetLivePreview();
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true };
        if (blueprint.CanEditPanel) { BuildPanelEditor(); return; }
        if (!blueprint.CanEditGrid) { BuildReadOnly(); return; }
        Text("title", new(12, 8, 1256, 42), blueprint.IsImported ? $"2 / 2 — 編集中：{blueprint.SelectedLayoutId}（既存モデルと bindings は保持）" : "2 / 2 — 新規スタイル設計（横・縦とも 1～8 セル）");
        ui.AddButton("back", new(1070, 54, 198, 44), "1 ページ目へ戻る", () => { Capture(); pendingPage = BuildWelcome; });
        Text("columnsLabel", new(12, 54, 140, 44), "横のセル数");
        columns = ui.AddTextBox("columns", new(154, 54, 80, 44), "横のセル数", blueprint.Columns.Count.ToString());
        Text("rowsLabel", new(246, 54, 140, 44), "縦のセル数");
        rows = ui.AddTextBox("rows", new(388, 54, 80, 44), "縦のセル数", blueprint.Rows.Count.ToString());
        ui.AddButton("resize", new(484, 54, 220, 44), "表を作る／更新", () => Guard(() =>
        {
            Capture();
            if (!int.TryParse(columns.Editor!.Text, out var c) || !int.TryParse(rows.Editor!.Text, out var r))
                throw new ArgumentException("横・縦のセル数を整数で入力してください。");
            if ((c < blueprint.Columns.Count || r < blueprint.Rows.Count) && pendingShrink != (c, r))
            {
                pendingShrink = (c, r);
                message = "表を縮めると範囲外のセルが消えます。続ける場合は同じセル数で、もう一度「表を作る／更新」を押してください。";
                return;
            }
            blueprint.Resize(c, r); pendingShrink = null;
            selectedRow = Math.Min(selectedRow, r - 1); selectedColumn = Math.Min(selectedColumn, c - 1);
            rebuild = true; message = "表を更新しました。サイズの数値を入力し、単位ボタンで rate／px を選んでください。";
        }));
        ui.AddButton("theme", new(720, 54, 180, 44), "明るい／暗い", () =>
        { theme = theme.Background == StationeryTheme.Light.Background ? StationeryTheme.Dark with { FontSize = 16, Padding = 4 } : StationeryTheme.Light with { FontSize = 16, Padding = 4 }; ui.Theme = theme; });
        Text("help", new(12, 104, 1256, 64), "列幅・行高を変更すると右のプレビューに即時反映します。\nプレビューのセルをクリックして、種類や表示名を設定できます。");
        Text("columnTracksTitle", new(12, 178, 252, 36), "列の幅（数値 / 単位）");
        Text("rowTracksTitle", new(280, 178, 256, 36), "行の高さ（数値 / 単位）");
        for (var c = 0; c < blueprint.Columns.Count; c++)
        {
            Text($"columnIndex{c}", new(12, 220 + c * 44, 36, 40), (c + 1).ToString());
            AddTrack(blueprint.Columns[c], $"column{c}", new(52, 220 + c * 44, 212, 40), $"列 {c + 1} の幅");
        }
        for (var r = 0; r < blueprint.Rows.Count; r++)
        {
            Text($"rowIndex{r}", new(280, 220 + r * 44, 36, 40), (r + 1).ToString());
            AddTrack(blueprint.Rows[r], $"row{r}", new(320, 220 + r * 44, 216, 40), $"行 {r + 1} の高さ");
        }
        Text("selected", new(12, 586, 180, 44), "選択セルの文房具：");
        kind = ui.AddButton("kind", new(196, 586, 340, 44), blueprint.IsImported ? "既存の定義を保持" : KindLabel(blueprint.At(selectedRow, selectedColumn).Kind), () =>
        {
            if (blueprint.IsImported) return;
            var cell = blueprint.At(selectedRow, selectedColumn);
            var index = StyleBlueprint.Kinds.ToList().IndexOf(cell.Kind);
            cell.Kind = StyleBlueprint.Kinds[(index + 1) % StyleBlueprint.Kinds.Count]; kind.Label = KindLabel(cell.Kind);
        });
        Text("labelTitle", new(12, 646, 100, 36), blueprint.IsImported ? "モデル：" : "表示名：");
        label = ui.AddTextBox("label", new(12, 690, 524, 44), "選択セルの表示名", blueprint.IsImported ? ImportedCellDescription(selectedRow, selectedColumn) : blueprint.At(selectedRow, selectedColumn).Label);
        ui.Focus.SetEnabled(label.Path, !blueprint.IsImported);
        ui.Focus.SetEnabled(kind.Path, !blueprint.IsImported);
        BuildLivePreviewHeader();
        BuildOutputControls(762);
        status = Text("status", new(12, 850, 1256, 48), message);
        BuildSidebar();
    }
    private static string KindLabel(string kind) => kind switch
    { "container" => "未指定（空き領域）", "button" => "ボタン", "textBox" => "テキスト入力", "textBlock" => "説明テキスト", "link" => "ページリンク", "tree" => "ツリー", _ => kind };
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
        var scale = Math.Max(.1, Math.Min(GraphicsDevice.Viewport.Width / 1600.0, GraphicsDevice.Viewport.Height / 900.0));
        if (layoutDialog is not null) { UpdateLayoutDialog(gameTime, scale); base.Update(gameTime); return; }
        ui.Viewport.Scale = scale;
        ui.Viewport.Offset = new(editingPage ? 320 * scale : 0, 0);
        if (sidebar is not null) { sidebar.Viewport.Scale = scale; sidebar.Theme = theme; }
        var mouse = Mouse.GetState(); var keyboard = Keyboard.GetState();
        PrepareDesignerSmoke(ref mouse, ref keyboard);
        if (editingPage && mouse.LeftButton == ButtonState.Pressed) sidebarActive = mouse.X < 320 * scale;
        var active = IsActive || !string.IsNullOrEmpty(smokeOutput);
        if (editingPage && sidebar is not null)
        {
            sidebar.Update(gameTime, active, sidebarActive ? keyboard : new(), mouse);
            HandleTreeSelection();
        }
        ui.Update(gameTime, active, !editingPage || !sidebarActive ? keyboard : new(), mouse);
        if (pendingPage is not null) { var action = pendingPage; pendingPage = null; action(); return; }
        if (!editingPage) { status.Label = message; base.Update(gameTime); return; }
        if (rebuild) { rebuild = false; BuildUi(); ui.Viewport.Scale = scale; ui.Viewport.Offset = new(320 * scale, 0); }
        if (!blueprint.CanEditGrid)
        {
            Capture();
            try { var json = blueprint.BuildJson(); RefreshTree(json); UpdateLivePreview(json, mouse); status.Label = message; }
            catch (JsonException ex) { status.Label = "入力を確認してください：" + ex.Message; }
            base.Update(gameTime); return;
        }
        Capture();
        try
        {
            var json = blueprint.BuildJson();
            RefreshTree(json);
            UpdateLivePreview(json, mouse);
            status.Label = message;
        }
        catch (JsonException ex)
        {
            ResetLivePreview();
            status.Label = "入力を確認してください：" + ex.Message;
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background)); ui.Draw();
        if (editingPage) { sidebar?.Draw(); DrawLivePreview(); }
        layoutDialog?.Draw();
        CaptureWelcomeSmoke();
        if (!string.IsNullOrEmpty(smokeOutput) && ++frames == 22)
        {
            var data = new Microsoft.Xna.Framework.Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];
            GraphicsDevice.GetBackBufferData(data);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            texture.SetData(data);
            using var file = File.Create(Path.Combine(smokeOutput, "designer.png")); texture.SaveAsPng(file, texture.Width, texture.Height);
            ValidateDesignerSmoke();
            if (Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG") != "1") blueprint.Export(outputPath);
            Exit();
        }
        base.Draw(gameTime);
    }
    protected override void Dispose(bool disposing)
    { if (disposing) { livePreview?.Dispose(); layoutDialog?.Dispose(); ui?.Dispose(); sidebar?.Dispose(); input?.Dispose(); } base.Dispose(disposing); }
}
