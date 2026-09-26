using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Controls;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Editor;
using StationeryUI.Styling;
using StationeryUI.Windows;
using System.Linq;
using System.Text.Json.Nodes;

internal sealed partial class EditorGame
{
    private bool editingPage, hasDraft, sidebarActive;
    private Action? pendingPage;
    private StationeryUiHost? sidebar;
    private StationeryUiHost.Element? styleTree;
    private StationeryUiHost.Element? propNodeName, propKind, propPosition, propLayout;
    private StationeryUiHost.Element? modelTreeModeButton, layoutTreeModeButton, jsonTreeModeButton;
    private readonly DeveloperInspectionModel semanticTree = new();
    private DeveloperViewState? selectionFromReadMode;
    private EditorTreeMode editorTreeMode = EditorTreeMode.Layout;
    private string sourceFile = "";
    private string? treeJson;
    private string? lastValidTreeJson;
    private readonly Dictionary<string, string> treeLayouts = [];
    private string? lastTreeSelection;
    private bool gridEditorVisible;
    private string? TargetLayoutId
    {
        get
        {
            for (var item = styleTree?.Tree?.TargetItem; item is not null; item = item.Parent)
                if (treeLayouts.TryGetValue(item.Id, out var id)) return id;
            return null;
        }
    }
    private bool HasLayoutTarget => TargetLayoutId is not null;
    private string? DirectLayoutTargetId
    {
        get
        {
            var item = styleTree?.Tree?.TargetItem;
            return item is not null && treeLayouts.TryGetValue(item.Id, out var id) ? id : null;
        }
    }
    private readonly string? smokeInput = Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_INPUT");

    private enum EditorTreeMode { Model, Layout, Json }

    private void BuildWelcome()
    {
        if (!FlushAutoSave()) return;
        editingPage = false; sidebarActive = false; applicationBarActive = false;
        ui?.Dispose(); sidebar?.Dispose(); sidebar = null;
        ui = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
        const string title = "スタイル設定ファイル選択";
        var titleWidth = new WindowsTextRasterizer(theme.FontFamily).MeasureTextWidth(title, theme.FontSize, false);
        Text("welcomeTitle", new(800 - titleWidth / 2 - theme.Padding, 261, titleWidth + 48, 64), title)
            .Theme = theme with { Surface = theme.Background };
        ui.AddButton("new", new(540, 349, 520, 64), "新規作成", () =>
        {
            pendingPage = () =>
            {
                if (!FlushAutoSave()) return;
                saveSession = null; saveError = null; invalidDraft = false;
                blueprint = new(); selectedRow = selectedColumn = 0;
                outputPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my-plan.stationery-ui.json");
                if (!string.IsNullOrEmpty(smokeOutput))
                {
                    blueprint.Resize(3, 2); blueprint.Columns[0].Number = "1.5";
                    blueprint.Columns[1].Number = "120"; blueprint.Columns[1].IsRate = false;
                }
                BeginEditing("新しいプランを作成しました。");
            };
        });
        var resume = ui.AddButton("resume", new(540, 437, 520, 64), "現在の編集を再開", () => pendingPage = () => BeginEditing("編集を再開しました。"));
        ui.Focus.SetEnabled(resume.Path, hasDraft);
        ui.AddButton("open", new(540, 525, 520, 64), "既存ファイルを読み取る", () => Guard(() =>
        {
            var path = ChooseStyleFile();
            if (path is null) { message = "ファイルの選択をキャンセルしました。"; return; }
            // Existing editing smoke scenarios exercise the legacy edit flow; normal file picks open read-only.
            if (!string.IsNullOrEmpty(smokeInput)) OpenStyle(path);
            else OpenReadStyle(path);
        }));
    }

    private void BeginEditing(string text)
    {
        editingPage = hasDraft = true; sidebarActive = false; applicationBarActive = false; rebuild = false;
        message = text; treeJson = null; lastTreeSelection = null;
        if (selectionFromReadMode is { } readSelection && blueprint.IsImported)
        {
            editorTreeMode = readSelection.TreeMode == DeveloperTreeMode.Model ? EditorTreeMode.Model : EditorTreeMode.Layout;
            revealLayout = null;
        }
        else revealLayout = blueprint.IsImported ? blueprint.SelectedLayoutId : blueprint.DefaultLayoutPath;
        if (!string.IsNullOrEmpty(smokeOutput) && saveSession is null) outputPath = System.IO.Path.Combine(smokeOutput, "plan.stationery-ui.json");
        BuildUi();
        var scale = BodyScale;
        ui.Viewport.Scale = scale; ui.Viewport.Offset = new(320 * scale, BodyTop);
        if (sidebar is not null) { sidebar.Viewport.Scale = scale; sidebar.Viewport.Offset = new(0, BodyTop); }
        if (PageSmoke) OpenPageDialog();
    }

    private void BuildReadOnly()
    {
        BuildLivePreviewHeader();
        BuildSidebar();
    }

    private void BuildSidebar()
    {
        if (sidebar is not null && styleTree is not null)
        {
            sidebar.Theme = theme;
            UpdateEditorTreeButtons();
            try { RefreshTree(blueprint.BuildJson()); } catch (System.Text.Json.JsonException) { }
            return;
        }
        sidebar?.Dispose();
        sidebar = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
        sidebar.AddTextBlock(sidebar.Root.AddChild("heading", "textBlock"), new(8, 8, 300, 36), "スタイルツリー");
        modelTreeModeButton = sidebar.AddButton("modelTreeMode", new(8, 46, 94, 32), "モデル", () => SetEditorTreeMode(EditorTreeMode.Model));
        layoutTreeModeButton = sidebar.AddButton("layoutTreeMode", new(106, 46, 94, 32), "レイアウト", () => SetEditorTreeMode(EditorTreeMode.Layout));
        jsonTreeModeButton = sidebar.AddButton("jsonTreeMode", new(204, 46, 94, 32), "JSON", () => SetEditorTreeMode(EditorTreeMode.Json));
        styleTree = sidebar.AddTree(sidebar.Root.AddChild("styleTree", "tree"), new(8, 84, 300, 608), "スタイルの構造", new());
        UpdateEditorTreeButtons();
        BuildTreeActions();
        treeJson = null;
        try { RefreshTree(blueprint.BuildJson()); }
        catch (System.Text.Json.JsonException) { if (lastValidTreeJson is not null) RefreshTree(lastValidTreeJson); }
    }

    private void RefreshTree(string json)
    {
        if (editorTreeMode == EditorTreeMode.Json) RefreshJsonTree(json);
        else RefreshSemanticTree(json);
    }

    private void SetEditorTreeMode(EditorTreeMode mode)
    {
        if (editorTreeMode == mode) return;
        Capture();
        editorTreeMode = mode;
        treeJson = null;
        lastTreeSelection = null;
        UpdateEditorTreeButtons();
        try { RefreshTree(blueprint.BuildJson()); }
        catch (System.Text.Json.JsonException) { }
    }

    private void UpdateEditorTreeButtons()
    {
        if (modelTreeModeButton is null) return;
        modelTreeModeButton.Label = (editorTreeMode == EditorTreeMode.Model ? "● " : "") + "モデル";
        layoutTreeModeButton!.Label = (editorTreeMode == EditorTreeMode.Layout ? "● " : "") + "レイアウト";
        jsonTreeModeButton!.Label = (editorTreeMode == EditorTreeMode.Json ? "● " : "") + "JSON";
    }

    private void RefreshSemanticTree(string json)
    {
        if (json == treeJson || styleTree is null) return;
        var previous = semanticTree.SelectedPath;
        var snapshot = blueprint.CreatePreview(Math.Max(1, previewWindow.Width), Math.Max(1, previewWindow.Height), TargetLayoutId);
        var root = snapshot.Settings.ModelTree.CreateTree();
        var entries = new List<StationeryInspectionEntry>();
        void Visit(StationeryNode node)
        {
            snapshot.Layout.Bounds.TryGetValue(node.Path, out var bounds);
            // The designer has no runtime page visibility state; every configured node is inspectable.
            entries.Add(new(node.Id, node.Path, node.Parent?.Path, node.Kind, node.Id, true, bounds));
            foreach (var child in node.Children) Visit(child);
        }
        Visit(root);
        var enriched = DeveloperInspectionLayout.Apply(entries, snapshot.Settings, snapshot.Layout);
        var targetMode = editorTreeMode == EditorTreeMode.Model ? DeveloperTreeMode.Model : DeveloperTreeMode.Layout;
        semanticTree.SetTreeMode(targetMode);
        semanticTree.Refresh(enriched);
        if (selectionFromReadMode is { } readSelection)
        {
            semanticTree.Restore(readSelection);
            selectionFromReadMode = null;
        }
        else if (revealLayout is not null)
        {
            var requested = revealLayout;
            var semanticLayoutPath = enriched.SelectMany(e => e.LayoutNodes ?? [])
                .FirstOrDefault(l => l.Path.EndsWith(":" + requested, StringComparison.Ordinal))?.Path ?? requested;
            if (semanticTree.Select(semanticLayoutPath)) revealLayout = null;
        }
        else if (previous is not null) semanticTree.Select(previous);
        foreach (var row in semanticTree.Tree.VisibleRows())
        {
            var path = semanticTree.PathFor(row.Item);
            if (path is not null)
                row.Item.Label = enriched.FirstOrDefault(entry => entry.Path == path)?.Id ?? row.Item.Label;
        }
        sidebar!.ReplaceTree(styleTree!, semanticTree.Tree);
        treePaths.Clear(); treeLayouts.Clear();
        var rootLayouts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var binding in snapshot.Settings.Bindings)
        {
            var rootLayout = "/" + binding.Layout.Split('/')[1];
            for (var path = binding.ModelPath; path is not null; )
            {
                rootLayouts.TryAdd(path, rootLayout);
                var slash = path.LastIndexOf('/');
                path = slash > 0 ? path[..slash] : null;
            }
        }
        foreach (var row in semanticTree.Tree.VisibleRows())
        {
            var path = semanticTree.PathFor(row.Item);
            if (path is null) continue;
            var separator = path.IndexOf(':');
            if (separator >= 0) treeLayouts[row.Item.Id] = path[(separator + 1)..];
            else if (rootLayouts.TryGetValue(path, out var rootLayout)) treeLayouts[row.Item.Id] = rootLayout;
        }
        treeJson = lastValidTreeJson = json;
        UpdateTreeActions();
        UpdatePropertyPanel();
    }

    private void RefreshJsonTree(string json)
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
            if (jsonPath[0] == "layouts" && value is JsonObject layoutObject && layoutObject["id"] is not null &&
                jsonPath.Length % 2 == 0 && jsonPath.Where((_, i) => i > 0 && i % 2 == 0).All(p => p == "children"))
            {
                var ids = new List<string>();
                for (JsonNode? ancestor = value; ancestor is not null; ancestor = ancestor.Parent)
                    if (ancestor is JsonObject objWithId && objWithId["id"] is JsonValue localId) ids.Insert(0, localId.GetValue<string>());
                treeLayouts[item.Id] = "/" + string.Join("/", ids);
            }
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
        UpdatePropertyPanel();
    }

    private void HandleTreeSelection()
    {
        var selected = styleTree?.Tree?.TargetItem?.Id;
        UpdateTreeActions();
        if (selected == lastTreeSelection) return;
        lastTreeSelection = selected;
        UpdatePropertyPanel();
        var layoutId = DirectLayoutTargetId;
        if (layoutId is null)
        {
            Capture(); rebuild = true;
            message = "layouts 内のレイアウトを操作対象にすると、設定とプレビューを表示します。";
            return;
        }
        if (!blueprint.IsImported) { Capture(); rebuild = true; return; }
        Guard(() =>
        {
            Capture();
            var layoutType = StyleBlueprint.NormalizeLayoutType((string?)StyleBlueprint.FindLayout(JsonNode.Parse(blueprint.BuildJson())!, layoutId)?["type"]);
            if (layoutType is not ("box-layout" or "grid-layout"))
            {
                rebuild = true;
                message = $"{layoutId} は閲覧専用レイアウトです。プレビューだけ表示します。";
                return;
            }
            blueprint.SelectLayout(layoutId);
            selectedRow = selectedColumn = 0; rebuild = true;
            message = blueprint.CanEditPanel ? $"編集中：{layoutId}。四辺の margin・padding を指定できます。border は padding 上に表示されます。"
                : $"編集中：{layoutId}。行・列のサイズを変更できます。既存モデルの種類・配置は保持します。";
        });
    }

    private void UpdatePropertyPanel()
    {
        if (propNodeName is null) return;
        var entry = editorTreeMode == EditorTreeMode.Json
            ? null
            : semanticTree.EntryFor(styleTree?.Tree?.TargetItem);
        if (entry is null)
        {
            propNodeName.Label = "文房具Ｉｄ: -";
            propKind!.Label = "種類: -";
            propPosition!.Label = "コンテナー内の位置: -";
            propLayout!.Label = "コンテナーとしてのレイアウト: -";
            return;
        }
        propNodeName.Label = $"文房具Ｉｄ: {entry.Id}";
        propKind!.Label = $"種類: {entry.Kind}";
        var position = entry.Cell is { } cell
            ? FormattableString.Invariant($"col={cell.Column}, row={cell.Row}, colspan={cell.ColumnSpan}, rowspan={cell.RowSpan}")
            : "-";
        propPosition!.Label = $"コンテナー内の位置: {position}";
        var layout = entry.LayoutTypes is { Count: > 0 } ?
            string.Join(", ", entry.LayoutTypes!.Select(t => t switch {
                "grid-layout" => "gridLayout",
                "box-layout" => "boxLayout",
                "dock-layout" => "dockLayout",
                _ => t
            })) : "-";
        propLayout!.Label = $"コンテナーとしてのレイアウト: {layout}";
    }

    private string ImportedCellDescription(int row, int column)
    {
        try { return blueprint.CellDescription(row, column); }
        catch (System.Text.Json.JsonException) { return "入力を確認"; }
    }

    private void PrepareEditorSmoke(ref MouseState mouse, ref KeyboardState keyboard)
    {
        if (string.IsNullOrEmpty(smokeOutput)) return;
        keyboard = new();
        if (startupFile is not null)
        {
            mouse = new();
            if (DeletePageSmoke && frames == 12) pendingPage = OpenPageDialog;
            return;
        }
        if (!editingPage)
        {
            var area = ui.Viewport.ToWindow(new(540, string.IsNullOrEmpty(smokeInput) ? 349 : 525, 520, 64));
            mouse = new((int)area.X + 10, (int)area.Y + 10, 0, frames == 1 ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            return;
        }
        if (PrepareLayoutEditingSmoke(ref mouse)) return;
        if (frames == 3 && blueprint.CanEditGrid && (!HasLayoutTarget || styleTree?.Tree?.TargetItem is null))
            throw new InvalidOperationException("Initial layout must have a visible tree target.");
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
                if (editorTreeMode != EditorTreeMode.Json)
                {
                    if (editFrame == 5)
                    {
                        var item = visible[index].Item;
                        styleTree.Tree.SetTarget(item);
                    }
                    mouse = new();
                    return;
                }
                var treePoint = sidebar!.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(60, styleTree.Bounds.Y + index * 32 + 16, 1, 1));
                mouse = new((int)treePoint.X, (int)treePoint.Y, 0, editFrame == 5 ? ButtonState.Pressed : ButtonState.Released,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
                return;
            }
            mouse = new(); return;
        }
        if (editFrame == 4) { SetText(columns, "4"); SetText(rows, "3"); }
        if (editFrame == 8) { SetText(columns, "2"); SetText(rows, "1"); }
        if (editFrame == 13)
        {
            var cell = previewSnapshot!.Cells.Single(c => c.Row == 0 && c.Column == 1).Bounds;
            mouse = SmokeMouse((int)(previewWindow.X + cell.X + cell.Width / 2), (int)(previewWindow.Y + cell.Y + cell.Height / 2), true);
            return;
        }
        if (frames is >= 17 and <= 19) { mouse = SmokeMouse(applicationBar, 570, 20, frames == 18); return; }
        mouse = new();
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
    private void ValidateEditorSmoke()
    {
        if (startupFile is not null && (!editingPage || !blueprint.IsImported ||
            sourceFile != Path.GetFullPath(startupFile) || saveSession?.FilePath != sourceFile))
            throw new InvalidOperationException("Command-line file did not open for editing.");
        if (PageSmoke && blueprint.PageNames.Contains("vTestPage") == DeletePageSmoke)
            throw new InvalidOperationException("Page dialog did not add/delete the page.");
        if (editingPage && styleTree?.Tree is { } tree)
        {
            var previous = tree.TargetItem;
            foreach (var root in tree.Roots.Where(n => treePaths.GetValueOrDefault(n.Id) is ["modelTree"] or ["controlTree"]))
            {
                tree.SetTarget(root.Children.FirstOrDefault() ?? root);
                UpdateTreeActions();
                if (sidebar!.Focus.IsEnabled(deleteNode!.Path) || sidebar.Focus.IsEnabled(renameId!.Path))
                    throw new InvalidOperationException("modelTree and controlTree must be read-only in this view.");
            }
            if (previous is not null) tree.SetTarget(previous); else tree.ClearTarget();
            UpdateTreeActions();
            if (ui.Inspect().Any(e => e.Id is "kind" or "label")) throw new InvalidOperationException("Model editor is still visible.");
            if (livePreview is not null && (Math.Abs(previewWindow.X + previewWindow.Width - (GraphicsDevice.Viewport.Width - 8)) > 1
                || Math.Abs(previewWindow.Y + previewWindow.Height - (GraphicsDevice.Viewport.Height - InspectorHeight - 8)) > 1))
                throw new InvalidOperationException("Preview must fill the available right-hand area.");
        }
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
        if (Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_NATIVE_DIALOG") == "1" && !testedNativeDialog)
            throw new InvalidOperationException("Native file dialog was not observed.");
        if (Environment.GetEnvironmentVariable("STATIONERYUI_EDITOR_TEST_CANCEL_DIALOG") == "1")
        {
            if (editingPage) throw new InvalidOperationException("Cancel should stay on the first page.");
            return;
        }
        if (!editingPage || styleTree?.Tree?.Roots.Count < 1) throw new InvalidOperationException("Page navigation or style tree failed.");
        if (startupFile is not null) return;
        if (!string.IsNullOrEmpty(smokeInput))
        {
            if (!blueprint.IsImported) throw new InvalidOperationException("Imported style editing failed.");
            if (blueprint.EditableLayouts.Count > 1 && blueprint.SelectedLayoutId != blueprint.EditableLayouts[1]) throw new InvalidOperationException("Tree layout selection failed.");
            return;
        }
        if (blueprint.Columns.Count != 2 || blueprint.Rows.Count != 1) throw new InvalidOperationException("Editor resize/confirmation failed.");
        if (selectedColumn != 1 || selectedRow != 0) throw new InvalidOperationException("Preview cell selection failed.");
        if (blueprint.At(0, 1).Label != "") throw new InvalidOperationException("Selecting an empty cell copied the previous label.");
        var created = System.IO.Path.Combine(smokeOutput!, "my-plan-2.stationery-ui.json");
        if (!File.Exists(created) || File.ReadAllText(System.IO.Path.Combine(smokeOutput!, "my-plan.stationery-ui.json")) != "existing file")
            throw new InvalidOperationException("New file creation or collision handling failed.");
        outputPath = System.IO.Path.Combine(smokeOutput!, "plan.stationery-ui.json");
    }

    private void VerifyUntargetedSmoke()
    {
        if (!editingPage) return;
        Capture();
        var before = blueprint.BuildJson();
        styleTree!.Tree!.ClearTarget();
        lastTreeSelection = "previousTarget";
        HandleTreeSelection();
        BuildUi();
        UpdateLivePreview(before, new());
        if (livePreview is not null || gridEditorVisible || tracks.Count != 0 || panelFields.Count != 0
            || ui.Inspect().Count != 1 || blueprint.BuildJson() != before)
            throw new InvalidOperationException("No target must leave an empty editor and preview without changing the document.");
    }
}
