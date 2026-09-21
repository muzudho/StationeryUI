using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Controls;
using StationeryUI.MonoGame;
using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using StationeryUI.Windows;
using System.Text.Json.Nodes;

internal sealed partial class DesignerGame
{
    private bool editingPage, hasDraft, sidebarActive;
    private Action? pendingPage;
    private DesktopUi? sidebar;
    private DesktopUi.Element? styleTree;
    private DesktopUi.Element sourcePath = null!;
    private string sourceFile = "";
    private string? treeJson;
    private string? lastValidTreeJson;
    private readonly Dictionary<string, string> treeLayouts = [];
    private string? lastTreeSelection;
    private readonly string? smokeInput = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_INPUT");

    private void BuildWelcome()
    {
        editingPage = false; sidebarActive = false;
        ui?.Dispose(); sidebar?.Dispose(); sidebar = null;
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme };
        Text("welcomeTitle", new(180, 100, 1240, 64), "1 / 2 — スタイル設計を始める");
        Text("welcomeHelp", new(180, 184, 1240, 96), "新しい設計を作るか、既存のスタイル設定ファイルを読み込むかを選んでください。\n編集結果は別の JSON ファイルへエクスポートします。元のファイルは変更しません。");
        ui.AddButton("new", new(180, 310, 520, 64), hasDraft ? "新規作成（現在のプランを置換）" : "新規作成", () =>
        {
            pendingPage = () =>
            {
                blueprint = new(); selectedRow = selectedColumn = 0;
                outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my-plan.stationery-style.json");
                if (!string.IsNullOrEmpty(smokeOutput))
                {
                    blueprint.Resize(3, 2); blueprint.Columns[0].Number = "1.5";
                    blueprint.Columns[1].Number = "120"; blueprint.Columns[1].IsRate = false;
                }
                BeginEditing("新しいプランを作成しました。");
            };
        });
        Text("sourceLabel", new(180, 414, 1240, 42), "既存の *.stationery-style.json のパス（貼り付け可）");
        sourcePath = ui.AddTextBox("source", new(180, 468, 1240, 52), "読み込むスタイルファイル", sourceFile, 4096);
        ui.AddButton("open", new(180, 548, 520, 64), "既存ファイルを開いて編集", () => Guard(() =>
        {
            var path = sourcePath.Editor!.Text;
            var loaded = StyleBlueprint.Open(path);
            sourceFile = path;
            pendingPage = () =>
            {
                blueprint = loaded; selectedRow = selectedColumn = 0;
                var full = System.IO.Path.GetFullPath(path);
                outputPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(full)!, System.IO.Path.GetFileNameWithoutExtension(full) + ".edited.stationery-style.json");
                BeginEditing("既存ファイルを読み込みました。左の layouts から編集する表を選べます。モデルと bindings は保持します。");
            };
        }));
        if (hasDraft) ui.AddButton("resume", new(740, 310, 520, 64), "現在の編集を再開", () => pendingPage = () => BeginEditing("編集を再開しました。"));
        status = Text("welcomeStatus", new(180, 670, 1240, 130), message);
    }

    private void BeginEditing(string text)
    {
        editingPage = hasDraft = true; pendingShrink = null; sidebarActive = false; rebuild = false;
        message = text; treeJson = null; lastTreeSelection = null;
        if (!string.IsNullOrEmpty(smokeOutput)) outputPath = System.IO.Path.Combine(smokeOutput, "plan.stationery-style.json");
        BuildUi();
        var scale = Math.Max(.1, Math.Min(GraphicsDevice.Viewport.Width / 1600.0, GraphicsDevice.Viewport.Height / 900.0));
        ui.Viewport.Scale = scale; ui.Viewport.Offset = new(320 * scale, 0);
        if (sidebar is not null) sidebar.Viewport.Scale = scale;
    }

    private void BuildReadOnly()
    {
        Text("readOnlyTitle", new(12, 12, 1256, 64), "2 / 2 — スタイルの確認");
        Text("readOnlyHelp", new(12, 100, 1256, 150), "このファイルには、表で編集できる 8×8 以下のフローティングレイアウトがありません。\n左側のツリーでスタイル全体を確認できます。内容を保ったまま別名で出力できます。");
        ui.AddButton("back", new(12, 280, 300, 52), "1 ページ目へ戻る", () => pendingPage = BuildWelcome);
        output = ui.AddTextBox("output", new(12, 380, 980, 52), "出力先", outputPath, 4096);
        ui.AddButton("export", new(1010, 380, 258, 52), "エクスポート", () => Guard(() =>
        { outputPath = output.Editor!.Text; blueprint.Export(outputPath); message = "出力しました。"; }));
        status = Text("status", new(12, 470, 1256, 100), message);
        BuildSidebar();
    }

    private void BuildSidebar()
    {
        if (sidebar is not null && styleTree is not null)
        {
            sidebar.Theme = theme;
            try { RefreshTree(blueprint.BuildJson()); } catch (System.Text.Json.JsonException) { }
            return;
        }
        sidebar?.Dispose();
        sidebar = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme };
        sidebar.AddTextBlock(sidebar.Root.AddChild("heading", "textBlock"), new(8, 8, 300, 76), "2 / 2 — スタイルツリー\nmodels / layouts / bindings");
        styleTree = sidebar.AddTree(sidebar.Root.AddChild("styleTree", "tree"), new(8, 92, 300, 680), "スタイルの構造", new());
        sidebar.AddTextBlock(sidebar.Root.AddChild("treeHelp", "textBlock"), new(8, 782, 300, 110), "＋／－で開閉。\nlayouts 内の表を選ぶと右側で編集できます。\n他の種類はツリーで確認できます。");
        treeJson = null;
        try { RefreshTree(blueprint.BuildJson()); }
        catch (System.Text.Json.JsonException) { if (lastValidTreeJson is not null) RefreshTree(lastValidTreeJson); }
    }

    private void RefreshTree(string json)
    {
        if (json == treeJson || styleTree is null) return;
        var old = new Dictionary<string, bool>();
        void Remember(TreeItem item) { old[item.Id] = item.IsExpanded; foreach (var child in item.Children) Remember(child); }
        foreach (var root in styleTree.Tree!.Roots) Remember(root);
        var selected = styleTree.Tree.SelectedItem?.Id;
        var next = new TreeView(); treeLayouts.Clear();
        TreeItem? restore = null;
        void Visit(JsonNode? value, string key, string identity, TreeItem? parent, int depth, string? layoutId = null)
        {
            var title = value is JsonObject obj && obj["id"] is JsonValue id
                ? $"{id.GetValue<string>()} ({(string?)obj["type"]})"
                : value is JsonValue ? $"{key}: {value}" : key;
            var item = next.AddNode(identity, title, parent, old.GetValueOrDefault(identity, depth < 1 || key == "layouts"));
            if (identity == selected) restore = item;
            if (layoutId is not null) treeLayouts[item.Id] = layoutId;
            if (value is JsonObject properties)
            {
                var index = 0;
                foreach (var pair in properties) Visit(pair.Value, pair.Key, identity + "_p" + index++, item, depth + 1);
            }
            else if (value is JsonArray array)
                for (var i = 0; i < array.Count; i++) Visit(array[i], $"[{i}]", identity + "_a" + i, item, depth + 1,
                    key == "layouts" ? (string?)array[i]?["id"] : null);
        }
        var rootJson = JsonNode.Parse(json)!.AsObject(); var section = 0;
        foreach (var pair in rootJson) Visit(pair.Value, pair.Key, "section" + section++, null, 0);
        if (restore is not null) next.Select(restore);
        sidebar!.ReplaceTree(styleTree, next); treeJson = lastValidTreeJson = json;
    }

    private void HandleTreeSelection()
    {
        var selected = styleTree?.Tree?.SelectedItem?.Id;
        if (selected == lastTreeSelection) return;
        lastTreeSelection = selected;
        if (selected is null || !treeLayouts.TryGetValue(selected, out var layoutId)) return;
        if (!blueprint.IsImported) return;
        Guard(() =>
        {
            Capture(); blueprint.SelectLayout(layoutId);
            selectedRow = selectedColumn = 0; rebuild = true;
            message = $"編集中：{layoutId}。行・列のサイズを変更できます。既存モデルの種類・配置は保持します。";
        });
    }

    private string ImportedCellDescription(int row, int column)
    {
        try { return blueprint.CellDescription(row, column); }
        catch (System.Text.Json.JsonException) { return "入力を確認"; }
    }

    private void PrepareDesignerSmoke(ref MouseState mouse, ref KeyboardState keyboard)
    {
        if (string.IsNullOrEmpty(smokeOutput)) return;
        keyboard = new();
        if (!editingPage)
        {
            if (!string.IsNullOrEmpty(smokeInput)) SetText(sourcePath, smokeInput);
            var area = ui.Viewport.ToWindow(new(180, string.IsNullOrEmpty(smokeInput) ? 310 : 548, 520, 64));
            mouse = new((int)area.X + 10, (int)area.Y + 10, 0, frames == 1 ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            return;
        }
        var editFrame = frames - 3;
        if (!string.IsNullOrEmpty(smokeInput))
        {
            if (editFrame == 1 && tracks.Count > 0) SetText(tracks[0].Field, "2.5");
            if (editFrame is >= 4 and <= 6 && blueprint.EditableLayouts.Count > 1)
            {
                var target = blueprint.EditableLayouts[1];
                var visible = styleTree!.Tree!.VisibleRows();
                var index = visible.ToList().FindIndex(r => treeLayouts.GetValueOrDefault(r.Item.Id) == target);
                if (index < 0) throw new InvalidOperationException("Layout missing from style tree.");
                var treePoint = sidebar!.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(60, 92 + index * 32 + 16, 1, 1));
                mouse = new((int)treePoint.X, (int)treePoint.Y, 0, editFrame == 5 ? ButtonState.Pressed : ButtonState.Released,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                return;
            }
            mouse = new(); return;
        }
        if (editFrame == 4) { SetText(columns, "4"); SetText(rows, "3"); }
        if (editFrame == 8) { SetText(columns, "2"); SetText(rows, "1"); }
        var point = ui.Viewport.ToWindow(editFrame >= 4 ? new(484, 54, 220, 44) : kind.Bounds);
        mouse = new((int)point.X + 10, (int)point.Y + 10, 0, editFrame is 1 or 5 or 9 or 11 ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        if (editFrame == 3 && !smokeClicked) { SetText(label, "開始ボタン"); smokeClicked = true; }
    }
    private void CaptureWelcomeSmoke()
    {
        if (string.IsNullOrEmpty(smokeOutput) || frames != 0) return;
        var data = new Microsoft.Xna.Framework.Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];
        GraphicsDevice.GetBackBufferData(data);
        using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        texture.SetData(data);
        using var file = System.IO.File.Create(System.IO.Path.Combine(smokeOutput, "welcome.png")); texture.SaveAsPng(file, texture.Width, texture.Height);
    }
    private void ValidateDesignerSmoke()
    {
        if (!editingPage || styleTree?.Tree?.Roots.Count < 3) throw new InvalidOperationException("Page navigation or style tree failed.");
        if (!string.IsNullOrEmpty(smokeInput))
        {
            if (!blueprint.IsImported) throw new InvalidOperationException("Imported style editing failed.");
            if (blueprint.EditableLayouts.Count > 1 && blueprint.SelectedLayoutId != blueprint.EditableLayouts[1]) throw new InvalidOperationException("Tree layout selection failed.");
            return;
        }
        if (blueprint.At(0, 0).Kind != "button" || blueprint.At(0, 0).Label != "開始ボタン") throw new InvalidOperationException("Designer cell editing failed.");
        if (blueprint.Columns.Count != 2 || blueprint.Rows.Count != 1) throw new InvalidOperationException("Designer resize/confirmation failed.");
    }
}
