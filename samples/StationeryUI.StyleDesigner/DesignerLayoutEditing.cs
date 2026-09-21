using StationeryUI.MonoGame;
using StationeryUI.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

internal sealed partial class DesignerGame
{
    private readonly Dictionary<string, string[]> treePaths = [];
    private readonly List<(StationeryUiHost.Element Field, string Key)> panelFields = [];
    private StationeryUiHost.Element? addChild, deleteNode;
    private StationeryUiHost? layoutDialog;
    private string? layoutChoice, revealLayout;

    private void BuildTreeActions()
    {
        addChild = sidebar!.AddButton("addChild", new(8, 708, 144, 44), "子要素追加", () => pendingPage = OpenLayoutDialog);
        deleteNode = sidebar.AddButton("deleteNode", new(164, 708, 144, 44), "削除", () => Guard(() =>
        {
            var target = styleTree?.Tree?.TargetItem;
            if (target is null || !treePaths.TryGetValue(target.Id, out var path) || path.Length < 2) return;
            Capture(); blueprint.DeleteNode(path);
            styleTree!.Tree!.ClearTarget();
            treeJson = null; lastTreeSelection = null;
            message = "要素を削除しました。"; rebuild = true;
        }));
        UpdateTreeActions();
    }

    private void UpdateTreeActions()
    {
        if (addChild is null || deleteNode is null || sidebar is null) return;
        var target = styleTree?.Tree?.TargetItem;
        var path = target is not null ? treePaths.GetValueOrDefault(target.Id) : null;
        sidebar.Focus.SetEnabled(addChild.Path, path is ["layouts"]);
        sidebar.Focus.SetEnabled(deleteNode.Path, path is { Length: > 1 });
    }

    private void OpenLayoutDialog()
    {
        ui.Update(new GameTime(), false, new(), new());
        sidebar?.Update(new GameTime(), false, new(), new());
        layoutChoice = null;
        layoutDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family)) { Theme = theme, UseStationeryButtons = true };
        layoutDialog.AddTextBlock(layoutDialog.Root.AddChild("dialogHelp", "textBlock"), new(430, 280, 740, 240), "子要素追加 — レイアウトの種類を選んでください\n追加した定義は bindings でモデルに割り当てられます。");
        layoutDialog.AddButton("panel", new(460, 380, 290, 48), "panel", () => layoutChoice = "panel");
        layoutDialog.AddButton("floating", new(770, 380, 370, 48), "floating-layout", () => layoutChoice = "floating-layout");
        layoutDialog.AddButton("cancel", new(460, 448, 680, 44), "キャンセル", () => layoutChoice = "cancel");
    }

    private void UpdateLayoutDialog(GameTime time, double scale)
    {
        layoutDialog!.Viewport.Scale = scale;
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        if (!string.IsNullOrEmpty(smokeOutput) && layoutSmoke is not null)
        {
            keyboard = new();
            mouse = SmokeMouse(layoutDialog, layoutSmoke == "panel" ? 470 : 780, 395, frames == 9);
        }
        layoutDialog.Update(time, IsActive || !string.IsNullOrEmpty(smokeOutput), keyboard, mouse);
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) layoutChoice = "cancel";
        if (layoutChoice is null) return;
        var choice = layoutChoice;
        layoutDialog.Dispose(); layoutDialog = null; layoutChoice = null;
        if (choice == "cancel") return;
        Guard(() =>
        {
            Capture(); revealLayout = blueprint.AddLayout(choice);
            selectedRow = selectedColumn = 0;
            message = $"{revealLayout} を追加しました。設定を入力してください。";
            BuildUi();
        });
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
            var index = rows.ToList().FindIndex(r => treePaths.GetValueOrDefault(r.Item.Id) is ["layouts"]);
            mouse = SmokeMouse(sidebar!, 80, 92 + index * 32 + 16, frames == 4);
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
        return true;
    }

    private bool ValidateLayoutEditingSmoke()
    {
        if (layoutSmoke is null) return false;
        if (layoutDialog is not null || styleTree!.Tree!.SelectedItem is not null)
            throw new InvalidOperationException($"Dialog open: {layoutDialog is not null}; selection: {styleTree!.Tree!.SelectedItem?.Id}; message: {message}");
        var settings = StationeryUI.Styling.StationeryStyleSettings.Parse(blueprint.BuildJson());
        if (layoutSmoke == "delete")
        {
            if (settings.Layouts.Count != 1) throw new InvalidOperationException("Delete button failed.");
        }
        else if (layoutSmoke == "panel")
        {
            if (!blueprint.CanEditPanel || settings.Layouts.Single(l => l.Type == "panel").Margin.Left != 12.5)
                throw new InvalidOperationException("Panel dialog/editor failed.");
        }
        else if (!blueprint.CanEditGrid || settings.Layouts.Count != 2 || blueprint.Columns[0].Number != "2.5")
            throw new InvalidOperationException("Floating layout dialog/editor failed.");
        return true;
    }

    private void BuildPanelEditor()
    {
        Text("panelTitle", new(12, 12, 1256, 50), $"2 / 2 — panel：{blueprint.SelectedLayoutId}");
        Text("panelHelp", new(12, 76, 1256, 88), "margin＝外側の余白、padding＝内側の余白（単位 px）。\nborder はサイズ計算に含めず、パネルの外側に描く枠です。");
        ui.AddButton("back", new(1000, 180, 268, 44), "1 ページ目へ戻る", () => { Capture(); pendingPage = BuildWelcome; });
        var sides = new[] { "top", "right", "bottom", "left" };
        for (var c = 0; c < sides.Length; c++) Text("side" + c, new(210 + c * 240, 230, 220, 40), sides[c] + " (px)");
        var groups = new[] { "margin", "padding", "border" };
        for (var r = 0; r < groups.Length; r++)
        {
            Text("group" + r, new(12, 284 + r * 72, 180, 44), groups[r]);
            for (var c = 0; c < sides.Length; c++)
            {
                var key = groups[r] + "." + sides[c];
                var field = ui.AddTextBox("edge" + r + c, new(210 + c * 240, 284 + r * 72, 220, 44), key, blueprint.PanelEdges[key].Number, 24);
                panelFields.Add((field, key));
            }
        }
        Text("panelScope", new(12, 530, 1256, 80), "0 以上の数値を指定してください。変更はツリーと出力 JSON に反映されます。\nモデルへの割り当ては bindings で指定します。");
        BuildOutputControls(762);
        status = Text("status", new(12, 850, 1256, 48), message);
        BuildSidebar();
    }
}
