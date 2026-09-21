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
    private static void Main() { using var game = new DesignerGame(); game.Run(); }
}

internal sealed partial class DesignerGame : Game
{
    private readonly GraphicsDeviceManager manager;
    private StyleBlueprint blueprint = new();
    private WindowsTextInputService input = null!;
    private DesktopUi ui = null!;
    private DesktopUi.Element columns = null!, rows = null!, label = null!, kind = null!, status = null!, output = null!;
    private readonly List<(DesktopUi.Element Field, DesktopUi.Element Unit, StyleBlueprint.Track Track)> tracks = [];
    private readonly List<(DesktopUi.Element Element, int Row, int Column)> cells = [];
    private readonly List<(DesktopUi.Element Element, int Row, int Column)> preview = [];
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
        manager = new(this) { PreferredBackBufferWidth = 1600, PreferredBackBufferHeight = 900 };
        Window.Title = "StationeryUI Style Designer";
        Window.AllowUserResizing = true; IsMouseVisible = true;
    }
    protected override void LoadContent()
    {
        Window.Title = "文房具 UI — スタイル設計ツール";
        input = new(Window.Handle);
        if (!string.IsNullOrEmpty(smokeOutput) && Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_DARK") == "1")
            theme = StationeryTheme.Dark with { FontSize = 16, Padding = 4 };
        BuildWelcome();
    }
    private DesktopUi.Element Text(string id, ScreenRectangle bounds, string text) =>
        ui.AddTextBlock(ui.Root.AddChild(id, "textBlock"), bounds, text);
    private void SetText(DesktopUi.Element field, string text) { field.Editor!.SelectAll(); field.Editor.Insert(text); }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        { message = ex.Message; }
    }
    private void Capture()
    {
        if (!editingPage || !blueprint.CanEditGrid) return;
        foreach (var (field, _, track) in tracks) track.Number = field.Editor!.Text;
        if (!blueprint.IsImported) blueprint.At(selectedRow, selectedColumn).Label = label.Editor!.Text;
        outputPath = output.Editor!.Text;
    }
    private void BuildUi()
    {
        ui?.Dispose(); tracks.Clear(); cells.Clear(); preview.Clear();
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true };
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
        Text("help", new(12, 104, 1256, 72), "上の入力欄＝各列の幅、左の入力欄＝各行の高さ。数値と単位を分けて指定します。\n中央の表でセルを選び、下の欄で種類と表示名を指定します。文房具 Id は自動で付きます。");
        var cw = 1072.0 / blueprint.Columns.Count;
        var rh = 320.0 / blueprint.Rows.Count;
        for (var c = 0; c < blueprint.Columns.Count; c++) AddTrack(blueprint.Columns[c], $"column{c}", new(196 + c * cw, 178, cw - 4, 42), $"列 {c + 1} の幅");
        for (var r = 0; r < blueprint.Rows.Count; r++) AddTrack(blueprint.Rows[r], $"row{r}", new(12, 230 + r * rh, 172, Math.Min(42, rh - 2)), $"行 {r + 1} の高さ");
        for (var r = 0; r < blueprint.Rows.Count; r++)
            for (var c = 0; c < blueprint.Columns.Count; c++)
            {
                var rr = r; var cc = c;
                var element = ui.AddButton($"cell{r}_{c}", new(196 + c * cw, 230 + r * rh, cw - 4, rh - 4), "", () =>
                {
                    Capture(); selectedRow = rr; selectedColumn = cc;
                    SetText(label, blueprint.IsImported ? ImportedCellDescription(rr, cc) : blueprint.At(rr, cc).Label);
                    kind.Label = blueprint.IsImported ? "既存の定義を保持" : KindLabel(blueprint.At(rr, cc).Kind);
                });
                cells.Add((element, r, c));
            }
        Text("selected", new(12, 562, 180, 44), "選択セルの文房具：");
        kind = ui.AddButton("kind", new(196, 562, 190, 44), blueprint.IsImported ? "既存の定義を保持" : KindLabel(blueprint.At(selectedRow, selectedColumn).Kind), () =>
        {
            if (blueprint.IsImported) return;
            var cell = blueprint.At(selectedRow, selectedColumn);
            var index = StyleBlueprint.Kinds.ToList().IndexOf(cell.Kind);
            cell.Kind = StyleBlueprint.Kinds[(index + 1) % StyleBlueprint.Kinds.Count]; kind.Label = KindLabel(cell.Kind);
        });
        Text("labelTitle", new(400, 562, 100, 44), blueprint.IsImported ? "モデル：" : "表示名：");
        label = ui.AddTextBox("label", new(504, 562, 764, 44), "選択セルの表示名", blueprint.IsImported ? ImportedCellDescription(selectedRow, selectedColumn) : blueprint.At(selectedRow, selectedColumn).Label);
        ui.Focus.SetEnabled(label.Path, !blueprint.IsImported);
        ui.Focus.SetEnabled(kind.Path, !blueprint.IsImported);
        Text("previewTitle", new(12, 620, 180, 136), "サイズ配分\n1000×600px\n縦・横を縮めて表示");
        for (var r = 0; r < blueprint.Rows.Count; r++)
            for (var c = 0; c < blueprint.Columns.Count; c++)
                preview.Add((Text($"preview{r}_{c}", new(), $"{r + 1},{c + 1}"), r, c));
        Text("outputTitle", new(12, 770, 120, 42), "出力先：");
        output = ui.AddTextBox("output", new(136, 770, 904, 42), "新しい JSON の出力パス", outputPath, 4096);
        ui.AddButton("export", new(1052, 770, 216, 42), "エクスポート", () => Guard(() =>
        {
            Capture(); blueprint.Export(outputPath);
            message = "設計図を出力しました。この JSON を AI に渡して「こう作って」と指示できます。";
        }));
        status = Text("status", new(12, 824, 1256, 64), message);
        BuildSidebar();
    }
    private static string KindLabel(string kind) => kind switch
    { "container" => "未指定（空き領域）", "button" => "ボタン", "textBox" => "テキスト入力", "textBlock" => "説明テキスト", "link" => "ページリンク", "tree" => "ツリー", _ => kind };
    private void AddTrack(StyleBlueprint.Track track, string id, ScreenRectangle bounds, string name)
    {
        var field = ui.AddTextBox(id, bounds with { Width = bounds.Width - 60 }, name, track.Number, 24);
        DesktopUi.Element? unit = null;
        unit = ui.AddButton(id + "Unit", new(bounds.X + bounds.Width - 58, bounds.Y, 58, bounds.Height), track.IsRate ? "rate" : "px", () =>
        { track.IsRate = !track.IsRate; unit!.Label = track.IsRate ? "rate" : "px"; });
        tracks.Add((field, unit, track));
    }
    protected override void Update(GameTime gameTime)
    {
        var scale = Math.Max(.1, Math.Min(GraphicsDevice.Viewport.Width / 1600.0, GraphicsDevice.Viewport.Height / 900.0));
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
        if (!blueprint.CanEditGrid) { status.Label = message; base.Update(gameTime); return; }
        Capture();
        foreach (var (element, row, column) in cells)
        {
            var cell = blueprint.At(row, column);
            var description = blueprint.IsImported ? ImportedCellDescription(row, column) : (string.IsNullOrEmpty(cell.Label) ? KindLabel(cell.Kind) : cell.Label);
            element.Label = $"{(row == selectedRow && column == selectedColumn ? "● " : "")}{row + 1},{column + 1} {description}";
        }
        try
        {
            var json = blueprint.BuildJson();
            RefreshTree(json);
            var visual = new StyleBlueprint(); visual.Resize(blueprint.Columns.Count, blueprint.Rows.Count);
            for (var i = 0; i < visual.Columns.Count; i++) { visual.Columns[i].Number = blueprint.Columns[i].Number; visual.Columns[i].IsRate = blueprint.Columns[i].IsRate; }
            for (var i = 0; i < visual.Rows.Count; i++) { visual.Rows[i].Number = blueprint.Rows[i].Number; visual.Rows[i].IsRate = blueprint.Rows[i].IsRate; }
            var arranged = StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(visual.BuildJson()), 1000, 600);
            foreach (var (element, row, column) in preview)
            {
                var rect = arranged.Bounds[$"/design/mainPage/cellR{row + 1}C{column + 1}"];
                element.Bounds = new(196 + rect.X * 1.072, 620 + rect.Y * .23, Math.Max(0, rect.Width * 1.072 - 2), Math.Max(0, rect.Height * .23 - 2));
            }
            status.Label = message;
        }
        catch (JsonException ex)
        {
            foreach (var (element, _, _) in preview) element.Bounds = new();
            status.Label = "入力を確認してください：" + ex.Message;
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(DesktopUi.Convert(ui.Theme.Background)); ui.Draw();
        if (editingPage) sidebar?.Draw();
        CaptureWelcomeSmoke();
        if (!string.IsNullOrEmpty(smokeOutput) && ++frames == 19)
        {
            ValidateDesignerSmoke();
            if (Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG") != "1") blueprint.Export(outputPath);
            var data = new Microsoft.Xna.Framework.Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];
            GraphicsDevice.GetBackBufferData(data);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            texture.SetData(data);
            using var file = File.Create(Path.Combine(smokeOutput, "designer.png")); texture.SaveAsPng(file, texture.Width, texture.Height);
            Exit();
        }
        base.Draw(gameTime);
    }
    protected override void Dispose(bool disposing)
    { if (disposing) { ui?.Dispose(); sidebar?.Dispose(); input?.Dispose(); } base.Dispose(disposing); }
}
