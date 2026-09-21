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
    private StationeryUiHost? sidebar;
    private StationeryUiHost.Element? styleTree;
    private string sourceFile = "";
    private string? treeJson;
    private string? lastValidTreeJson;
    private readonly Dictionary<string, string> treeLayouts = [];
    private string? lastTreeSelection;
    private readonly string? smokeInput = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_INPUT");

    private void BuildWelcome()
    {
        if (!FlushAutoSave()) return;
        editingPage = false; sidebarActive = false; applicationBarActive = false;
        ui?.Dispose(); sidebar?.Dispose(); sidebar = null;
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        Text("welcomeTitle", new(180, 100, 1240, 64), "1 / 2 — スタイル設計を始める");
        ui.AddButton("new", new(180, 310, 520, 64), hasDraft ? "新規作成（現在のプランを置換）" : "新規作成", () =>
        {
            pendingPage = () =>
            {
                if (!FlushAutoSave()) return;
                saveSession = null; saveError = null; invalidDraft = false;
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
        ui.AddButton("open", new(180, 548, 520, 64), "既存のファイルを編集する", () => Guard(() =>
        {
            var path = ChooseStyleFile();
            if (path is null) { message = "ファイルの選択をキャンセルしました。"; return; }
            OpenStyle(path);
        }));
        if (hasDraft) ui.AddButton("resume", new(740, 310, 520, 64), "現在の編集を再開", () => pendingPage = () => BeginEditing("編集を再開しました。"));
    }

    private void BeginEditing(string text)
    {
        editingPage = hasDraft = true; pendingShrink = null; sidebarActive = false; applicationBarActive = false; rebuild = false;
        message = text; treeJson = null; lastTreeSelection = null;
        if (!string.IsNullOrEmpty(smokeOutput) && saveSession is null) outputPath = System.IO.Path.Combine(smokeOutput, "plan.stationery-style.json");
        BuildUi();
        var scale = BodyScale;
        ui.Viewport.Scale = scale; ui.Viewport.Offset = new(320 * scale, BodyTop);
        if (sidebar is not null) { sidebar.Viewport.Scale = scale; sidebar.Viewport.Offset = new(0, BodyTop); }
    }

    private void BuildReadOnly()
    {
        BuildLivePreviewHeader();
        BuildOutputControls(762);
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
        sidebar = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        sidebar.AddTextBlock(sidebar.Root.AddChild("heading", "textBlock"), new(8, 8, 300, 36), "スタイルツリー");
        styleTree = sidebar.AddTree(sidebar.Root.AddChild("styleTree", "tree"), new(8, 48, 300, 644), "スタイルの構造", new());
        BuildTreeActions();
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
        var selected = styleTree.Tree.TargetItem?.Id;
        var next = new TreeView { SelectOnInteraction = false }; treeLayouts.Clear(); treePaths.Clear();
        TreeItem? restore = null;
        void Visit(JsonNode? value, string key, string identity, TreeItem? parent, int depth, string? layoutId = null, string[]? jsonPath = null)
        {
            jsonPath ??= [key];
            var title = value is JsonObject obj && obj["id"] is JsonValue id
                ? $"{id.GetValue<string>()} ({(string?)obj["type"]})"
                : value is JsonValue ? $"{key}: {value}" : key;
            var item = next.AddNode(identity, title, parent, old.GetValueOrDefault(identity, depth < 1 || key == "layouts"));
            treePaths[item.Id] = jsonPath;
            if (identity == selected) restore = item;
            if (layoutId is not null) treeLayouts[item.Id] = layoutId;
            if (value is JsonObject properties)
            {
                var index = 0;
                foreach (var pair in properties) Visit(pair.Value, pair.Key, identity + "_p" + index++, item, depth + 1, jsonPath: [.. jsonPath, pair.Key]);
            }
            else if (value is JsonArray array)
                for (var i = 0; i < array.Count; i++) Visit(array[i], $"[{i}]", identity + "_a" + i, item, depth + 1,
                    key == "layouts" ? (string?)array[i]?["id"] : null, [.. jsonPath, i.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        }
        var rootJson = JsonNode.Parse(json)!.AsObject(); var section = 0;
        foreach (var pair in rootJson) Visit(pair.Value, pair.Key, "section" + section++, null, 0);
        if (revealLayout is not null)
        {
            var identity = treeLayouts.FirstOrDefault(p => p.Value == revealLayout).Key;
            TreeItem? Find(IEnumerable<TreeItem> items) => items.SelectMany(i => new[] { i }.Concat(Flatten(i.Children))).FirstOrDefault(i => i.Id == identity);
            IEnumerable<TreeItem> Flatten(IEnumerable<TreeItem> items) => items.SelectMany(i => new[] { i }.Concat(Flatten(i.Children)));
            restore = Find(next.Roots); revealLayout = null;
        }
        if (restore is not null) next.SetTarget(restore);
        sidebar!.ReplaceTree(styleTree, next); treeJson = lastValidTreeJson = json;
    }

    private void HandleTreeSelection()
    {
        var selected = styleTree?.Tree?.TargetItem?.Id;
        UpdateTreeActions();
        if (selected == lastTreeSelection) return;
        lastTreeSelection = selected;
        if (selected is null || !treeLayouts.TryGetValue(selected, out var layoutId)) return;
        if (!blueprint.IsImported) return;
        Guard(() =>
        {
            Capture();
            var layoutType = (string?)JsonNode.Parse(blueprint.BuildJson())!["layouts"]!.AsArray().FirstOrDefault(l => (string?)l!["id"] == layoutId)?["type"];
            if (layoutType is not ("panel" or "floating-layout")) return;
            blueprint.SelectLayout(layoutId);
            selectedRow = selectedColumn = 0; rebuild = true;
            message = blueprint.CanEditPanel ? $"編集中：{layoutId}。四辺の margin・padding・border を指定できます。"
                : $"編集中：{layoutId}。行・列のサイズを変更できます。既存モデルの種類・配置は保持します。";
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
            var area = ui.Viewport.ToWindow(new(180, string.IsNullOrEmpty(smokeInput) ? 310 : 548, 520, 64));
            mouse = new((int)area.X + 10, (int)area.Y + 10, 0, frames == 1 ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            return;
        }
        if (PrepareLayoutEditingSmoke(ref mouse)) return;
        if (PrepareSaveSmoke(ref mouse)) return;
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
                var treePoint = sidebar!.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(60, styleTree.Bounds.Y + index * 32 + 16, 1, 1));
                mouse = new((int)treePoint.X, (int)treePoint.Y, 0, editFrame == 5 ? ButtonState.Pressed : ButtonState.Released,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                return;
            }
            mouse = new(); return;
        }
        if (editFrame == 4) { SetText(columns, "4"); SetText(rows, "3"); }
        if (editFrame == 8) { SetText(columns, "2"); SetText(rows, "1"); }
        if (editFrame == 12)
        {
            if (ui.Focus.IsEnabled(ui.Root.Path + "/createFile"))
                throw new InvalidOperationException("New file must be disabled before folder selection.");
            selectedOutputFolder = smokeOutput;
            File.WriteAllText(System.IO.Path.Combine(smokeOutput, "my-plan.stationery-style.json"), "existing file");
            rebuild = true;
        }
        if (editFrame >= 13)
        {
            if (editFrame == 13)
            {
                var cell = previewSnapshot!.Cells.Single(c => c.Row == 0 && c.Column == 1).Bounds;
                mouse = new((int)(previewWindow.X + cell.X + cell.Width / 2), (int)(previewWindow.Y + cell.Y + cell.Height / 2), 0,
                    ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                return;
            }
            var createPoint = ui.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(678, 806, 174, 38));
            mouse = new((int)createPoint.X + 10, (int)createPoint.Y + 10, 0, editFrame == 15 ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            return;
        }
        var point = ui.Viewport.ToWindow(editFrame >= 4 ? new(560, 48, 220, 44) : kind.Bounds);
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
        if (SaveSmoke)
        {
            if (restoreDialog is not null || saveSession is null || saveSession.IsDirty || saveError is not null
                || !File.ReadAllBytes(saveSession.FilePath).SequenceEqual(File.ReadAllBytes(saveSession.FilePath + ".1.bak"))
                || !File.ReadAllText(saveSession.FilePath + ".2.bak").Contains("2.5rate"))
                throw new InvalidOperationException("Savepoint restore did not preserve the edited state and restore the source.");
            return;
        }
        if (toolHint.Bounds.Height + status.Bounds.Height != 80 || status.Bounds.Y + status.Bounds.Height != GraphicsDevice.Viewport.Height)
            throw new InvalidOperationException("Inspector must reserve exactly 80 viewport pixels.");
        if (ValidateLayoutEditingSmoke()) return;
        if (Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_NATIVE_DIALOG") == "1" && !testedNativeDialog)
            throw new InvalidOperationException("Native file dialog was not observed.");
        if (Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_CANCEL_DIALOG") == "1")
        {
            if (editingPage) throw new InvalidOperationException("Cancel should stay on the first page.");
            return;
        }
        if (!editingPage || styleTree?.Tree?.Roots.Count < 3) throw new InvalidOperationException("Page navigation or style tree failed.");
        if (!string.IsNullOrEmpty(smokeInput))
        {
            if (!blueprint.IsImported) throw new InvalidOperationException("Imported style editing failed.");
            if (blueprint.EditableLayouts.Count > 1 && blueprint.SelectedLayoutId != blueprint.EditableLayouts[1]) throw new InvalidOperationException("Tree layout selection failed.");
            return;
        }
        if (blueprint.At(0, 0).Kind != "button" || blueprint.At(0, 0).Label != "開始ボタン") throw new InvalidOperationException("Designer cell editing failed.");
        if (blueprint.Columns.Count != 2 || blueprint.Rows.Count != 1) throw new InvalidOperationException("Designer resize/confirmation failed.");
        if (selectedColumn != 1 || selectedRow != 0) throw new InvalidOperationException("Preview cell selection failed.");
        if (blueprint.At(0, 1).Label != "") throw new InvalidOperationException("Selecting an empty cell copied the previous label.");
        var created = System.IO.Path.Combine(smokeOutput!, "my-plan-2.stationery-style.json");
        if (!File.Exists(created) || File.ReadAllText(System.IO.Path.Combine(smokeOutput!, "my-plan.stationery-style.json")) != "existing file")
            throw new InvalidOperationException("New file creation or collision handling failed.");
        outputPath = System.IO.Path.Combine(smokeOutput!, "plan.stationery-style.json");
    }
}
