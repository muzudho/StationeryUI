using StationeryUI.MonoGame;
using StationeryUI.Windows;
using StationeryUI.StyleDesigner;
using System.Text.Json.Nodes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

internal sealed partial class DesignerGame
{
    private readonly Dictionary<string, string[]> treePaths = [];
    private readonly List<(StationeryUiHost.Element Field, string Key)> panelFields = [];
    private StationeryUiHost.Element? addChild, deleteNode, renameId, idField, idFeedback;
    private readonly List<StationeryUiHost.Element> idConfirmButtons = [];
    private string[]? renamePath;
    private string? addParentLayout;
    private readonly List<StationeryUiHost.Element> placementFields = [];
    private StationeryUiHost? layoutDialog;
    private string? layoutChoice, revealLayout;

    private void BuildTreeActions()
    {
        addChild = sidebar!.AddButton("addChild", new(8, 708, 144, 44), "子要素追加", () => pendingPage = OpenLayoutDialog);
        deleteNode = sidebar.AddButton("deleteNode", new(164, 708, 144, 44), "削除", () => Guard(() =>
        {
            var target = styleTree?.Tree?.TargetItem;
            if (target is null || !treePaths.TryGetValue(target.Id, out var path) || !IsEditableLayoutPath(path)) return;
            Capture(); blueprint.DeleteNode(path);
            styleTree!.Tree!.ClearTarget();
            treeJson = null; lastTreeSelection = null;
            message = "要素を削除しました。"; rebuild = true;
        }));
        renameId = sidebar.AddButton("renameId", new(8, 758, 300, 44), "Ｉｄ変更", () => pendingPage = () => Guard(() =>
        {
            Capture();
            var target = styleTree?.Tree?.TargetItem;
            if (target is null || !treePaths.TryGetValue(target.Id, out var path) || !IsEditableLayoutPath(path)) return;
            var id = blueprint.NodeId(path);
            if (id is null) return;
            OpenIdDialog(path, id);
        }));
        UpdateTreeActions();
    }

    private void UpdateTreeActions()
    {
        if (addChild is null || deleteNode is null || sidebar is null) return;
        var target = styleTree?.Tree?.TargetItem;
        var path = target is not null ? treePaths.GetValueOrDefault(target.Id) : null;
        var canAdd = path is ["layouts"];
        if (designerTreeMode != DesignerTreeMode.Json && TargetLayoutId is { } semanticLayout)
        {
            try
            {
                var node = StyleBlueprint.FindLayout(JsonNode.Parse(blueprint.BuildJson())!, semanticLayout);
                canAdd = (string?)node?["type"] == "grid-layout" ||
                    (string?)node?["type"] == "box-layout" && (node?["children"] as JsonArray)?.Count is null or 0;
            }
            catch (System.Text.Json.JsonException) { canAdd = false; }
        }
        if (!canAdd && path is not null)
        {
            try
            {
                var layoutPath = blueprint.LayoutPathFor(path[^1] == "children" ? path[..^1] : path);
                var node = StyleBlueprint.FindLayout(JsonNode.Parse(blueprint.BuildJson())!, layoutPath);
                canAdd = (string?)node?["type"] == "grid-layout" ||
                    (string?)node?["type"] == "box-layout" && (node?["children"] as JsonArray)?.Count is null or 0;
            }
            catch (System.Text.Json.JsonException) { }
        }
        sidebar.Focus.SetEnabled(addChild.Path, canAdd);
        sidebar.Focus.SetEnabled(deleteNode.Path, designerTreeMode == DesignerTreeMode.Json && IsEditableLayoutPath(path));
        if (renameId is not null)
        {
            var enabled = false;
            try { enabled = IsEditableLayoutPath(path) && blueprint.NodeId(path!) is not null; }
            catch (System.Text.Json.JsonException) { }
            if (designerTreeMode != DesignerTreeMode.Json) enabled = false;
            sidebar.Focus.SetEnabled(renameId.Path, enabled);
        }
    }

    private static bool IsEditableLayoutPath(string[]? path) => path is { Length: > 1 } && path[0] == "layouts";

    private void OpenLayoutDialog()
        => Guard(() =>
        {
            Capture();
            var target = styleTree?.Tree?.TargetItem;
            var path = target is null ? null : treePaths.GetValueOrDefault(target.Id);
            addParentLayout = designerTreeMode != DesignerTreeMode.Json
                ? (TargetLayoutId is { } selectedLayout && selectedLayout.Count(c => c == '/') > 1 ? selectedLayout : null)
                : path is null or ["layouts"] ? null : blueprint.LayoutPathFor(path[^1] == "children" ? path[..^1] : path);
            var suffix = 1;
            while (blueprint.ValidateChildId("layout" + suffix, addParentLayout).Error is not null) suffix++;
            OpenIdDialog(null, "layout" + suffix);
        });

    private void OpenIdDialog(string[]? path, string id)
    {
        SuspendBackgroundInput();
        layoutChoice = null;
        renamePath = path;
        if (path is not null) addParentLayout = null;
        placementFields.Clear();
        idConfirmButtons.Clear();
        layoutDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        var help = layoutDialog.AddTextBlock(layoutDialog.Root.AddChild("dialogHelp", "textBlock"), new(430, 210, 740, 370),
            path is null ? "子要素追加 — 文房具Ｉｄを入力し、種類を選んでください\n英字・数字・アンダースコア。推奨：camelCase" : "Ｉｄ変更 — 新しい文房具Ｉｄを入力してください\n既存の bindings の参照も更新します。");
        idField = layoutDialog.AddTextBox("stationeryId", new(460, 312, 680, 48), "文房具Ｉｄ", id, 256);
        if (path is null)
        {
            idConfirmButtons.Add(layoutDialog.AddButton("panel", new(460, 380, 290, 48), "box-layout を追加", () => layoutChoice = "box-layout"));
            idConfirmButtons.Add(layoutDialog.AddButton("floating", new(770, 380, 370, 48), "grid-layout を追加", () => layoutChoice = "grid-layout"));
        }
        else idConfirmButtons.Add(layoutDialog.AddButton("confirmId", new(460, 380, 680, 48), "変更を確定", () => layoutChoice = "rename"));
        var json = JsonNode.Parse(blueprint.BuildJson())!;
        var editedPath = path is null ? null : blueprint.LayoutPathFor(path);
        var parentPath = path is null ? addParentLayout : editedPath is not null && editedPath.LastIndexOf('/') > 0 ? editedPath[..editedPath.LastIndexOf('/')] : null;
        var needsPlacement = (string?)StyleBlueprint.FindLayout(json, parentPath)?["type"] == "grid-layout";
        if (needsPlacement)
        {
            help.Bounds = help.Bounds with { Height = 480 };
            var current = StyleBlueprint.FindLayout(json, editedPath);
            var keys = new[] { "row", "col", "rowspan", "colspan" };
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                layoutDialog.AddTextBlock(layoutDialog.Root.AddChild("placementLabel" + i, "textBlock"), new(460 + i * 170, 436, 160, 34), key);
                var value = (int?)(current?[key] ?? (key == "col" ? current?["column"] : null)) ?? (i < 2 ? 0 : 1);
                placementFields.Add(layoutDialog.AddTextBox("placement" + i, new(460 + i * 170, 474, 150, 44), key, value.ToString(), 10));
            }
        }
        idFeedback = layoutDialog.AddTextBlock(layoutDialog.Root.AddChild("idFeedback", "textBlock"), new(460, needsPlacement ? 532 : 438, 680, 72), "");
        layoutDialog.AddButton("cancel", new(460, needsPlacement ? 616 : 522, 680, 44), "キャンセル", () => layoutChoice = "cancel");
        UpdateIdFeedback();
    }

    private void UpdateIdFeedback()
    {
        var result = renamePath is null ? blueprint.ValidateChildId(idField!.Editor!.Text, addParentLayout) : blueprint.ValidateId(idField!.Editor!.Text, renamePath);
        if (result.Error is null && placementFields.Count > 0)
        {
            try
            {
                var values = ReadPlacement();
                var candidate = StyleBlueprint.Parse(blueprint.BuildJson());
                if (renamePath is not null) candidate.SetPlacement(blueprint.LayoutPathFor(renamePath)!, values[0], values[1], values[2], values[3]);
                else candidate.AddLayout("grid-layout", idField!.Editor!.Text, addParentLayout, values[0], values[1], values[2], values[3]);
            }
            catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException) { result = (ex.Message, result.Warning); }
        }
        idFeedback!.Label = result.Error ?? result.Warning ?? "この Id は使用できます。";
        foreach (var button in idConfirmButtons) layoutDialog!.Focus.SetEnabled(button.Path, result.Error is null);
    }

    private int[] ReadPlacement()
    {
        if (placementFields.Count == 0) return [0, 0, 1, 1];
        return placementFields.Select((field, index) => int.TryParse(field.Editor!.Text, out var value) && value >= (index < 2 ? 0 : 1)
            ? value : throw new ArgumentException("row / col は0以上、rowspan / colspan は1以上の整数を指定してください。")).ToArray();
    }
    private void UpdateLayoutDialog(GameTime time, double scale)
    {
        layoutDialog!.Viewport.Scale = scale;
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        if (!string.IsNullOrEmpty(smokeOutput) && layoutSmoke is not null)
        {
            keyboard = new();
            if (renamePath is not null) SetText(idField!, "renamedLayout");
            else if (frames == 8) SetText(idField!, blueprint.DefaultLayoutId);
            else if (frames == 9) SetText(idField!, "123_layout");
            mouse = SmokeMouse(layoutDialog, renamePath is not null || layoutSmoke == "panel" ? 470 : 780, 395, frames == (renamePath is null ? 9 : 17));
        }
        inspectorMouse = mouse;
        UpdateIdFeedback();
        layoutDialog.Update(time, IsActive || !string.IsNullOrEmpty(smokeOutput), keyboard, mouse);
        UpdateIdFeedback();
        if (!string.IsNullOrEmpty(smokeOutput) && layoutSmoke is not null && renamePath is null)
        {
            if (frames == 8 && idConfirmButtons.Any(b => layoutDialog.Focus.IsEnabled(b.Path))) throw new InvalidOperationException("Duplicate Id was enabled.");
            if (frames == 9 && (idFeedback!.Label.Length == 0 || idConfirmButtons.Any(b => !layoutDialog.Focus.IsEnabled(b.Path)))) throw new InvalidOperationException("Warning Id was blocked.");
        }
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) layoutChoice = "cancel";
        if (layoutChoice is null) return;
        var choice = layoutChoice;
        layoutChoice = null;
        if (choice == "cancel") { layoutDialog.Dispose(); layoutDialog = null; return; }
        try
        {
            var id = idField!.Editor!.Text;
            var candidate = StyleBlueprint.Parse(blueprint.BuildJson());
            if (blueprint.SelectedLayoutId is { } selected) candidate.SelectLayout(selected);
            var placement = ReadPlacement();
            if (choice == "rename")
            {
                if (placementFields.Count > 0) candidate.SetPlacement(blueprint.LayoutPathFor(renamePath!)!, placement[0], placement[1], placement[2], placement[3]);
                candidate.RenameId(renamePath!, id);
                if (renamePath![0] == "layouts") revealLayout = candidate.LayoutPathFor(renamePath);
            }
            else revealLayout = candidate.AddLayout(choice, id, addParentLayout, placement[0], placement[1], placement[2], placement[3]);
            blueprint = candidate;
            layoutDialog.Dispose(); layoutDialog = null;
            selectedRow = selectedColumn = 0;
            message = choice == "rename" ? $"Id を {id} に変更しました。" : $"{id} を追加しました。設定を入力してください。";
            BuildUi();
        }
        catch (Exception ex) when (ex is ArgumentException or System.Text.Json.JsonException)
        { idFeedback!.Label = ex.Message; }
    }

    private readonly string? layoutSmoke = Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_LAYOUT");
    private static MouseState SmokeMouse(StationeryUiHost host, double x, double y, bool pressed)
    {
        var point = host.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(x, y, 1, 1));
        return new((int)point.X, (int)point.Y, 0, pressed ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }

    private bool PrepareLayoutEditingSmoke(ref MouseState mouse)
    {
        if (layoutSmoke is null) return false;
        mouse = new();
        if (frames <= 5)
        {
            var rows = styleTree!.Tree!.VisibleRows();
            var index = rows.ToList().FindIndex(r => treeLayouts.TryGetValue(r.Item.Id, out var layout)
                && layout.Count(c => c == '/') == 1
                && (string?)StyleBlueprint.FindLayout(JsonNode.Parse(blueprint.BuildJson())!, layout)?["type"] is "grid-layout" or "box-layout");
            mouse = SmokeMouse(sidebar!, 80, styleTree.Bounds.Y + index * 32 + 16, frames == 4);
        }
        else if (frames <= 7)
        {
            if (!sidebar!.Focus.IsEnabled(addChild!.Path) || sidebar.Focus.IsEnabled(deleteNode!.Path))
                throw new InvalidOperationException("Layout-root action states are incorrect.");
            mouse = SmokeMouse(sidebar, 50, 730, frames == 6);
        }
        else if (frames == 12)
        {
            if (layoutSmoke == "panel") SetText(panelFields.Single(f => f.Key == "margin.left").Field, "12.5");
            else SetText(tracks[0].Field, "2.5");
        }
        if (layoutSmoke == "delete" && frames is >= 14 and <= 15)
            mouse = SmokeMouse(sidebar!, 200, 730, frames == 14);
        if (layoutSmoke == "rename" && frames is >= 14 and <= 15)
            mouse = SmokeMouse(sidebar!, 100, 780, frames == 14);
        // Let the background hosts resume for a frame after closing the rename modal.
        if (frames is >= 19 and <= 21) mouse = SmokeMouse(applicationBar, 570, 20, frames == 20);
        return true;
    }

    private bool ValidateLayoutEditingSmoke()
    {
        if (layoutSmoke is null) return false;
        if (!disabledExportHintVerified)
            throw new InvalidOperationException("Disabled control tooltip is missing.");
        if (layoutDialog is not null || styleTree!.Tree!.SelectedItem is not null)
            throw new InvalidOperationException($"Dialog open: {layoutDialog is not null}; selection: {styleTree!.Tree!.SelectedItem?.Id}; message: {message}");
        var settings = StationeryUI.Styling.StationeryStyleSettings.Parse(blueprint.BuildJson());
        if (layoutSmoke == "rename" && blueprint.SelectedLayoutId != "/renamedLayout") throw new InvalidOperationException("Rename Id button failed.");
        if (layoutSmoke == "delete")
        {
            if (settings.Layouts.Count != 1) throw new InvalidOperationException("Delete button failed.");
        }
        else if (layoutSmoke == "panel")
        {
            if (!blueprint.CanEditPanel || settings.Layouts.Single(l => l.Type == "box-layout").Margin.Left != 12.5)
                throw new InvalidOperationException("Panel dialog/editor failed.");
        }
        else if (!blueprint.CanEditGrid || settings.Layouts.Count != 2 || blueprint.Columns[0].Number != "2.5")
            throw new InvalidOperationException("Grid layout dialog/editor failed.");
        return true;
    }

}
