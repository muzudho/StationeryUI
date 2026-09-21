namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Inspection;
using StationeryUI.Platform;
using StationeryUI.Text;
using StationeryUI.Theming;
using StationeryUI.Styling;

/// <summary>StationeryUI-only inspector surface. The host supplies a graphics device, snapshots and clipboard service.</summary>
public sealed class StationeryDeveloperView : IDisposable
{
    private readonly DesktopUi ui;
    private readonly ITextInputService input;
    private readonly DesktopUi.Element header, split, tree, details, copy;
    public StationeryDeveloperStyle Style { get; }
    public DeveloperInspectionModel Model { get; } = new();
    public StationeryTheme Theme { get => ui.Theme; set => ui.Theme = value; }
    public double SplitRatio => split.Split!.Ratio;
    public ScreenRectangle SplitBounds => split.Bounds;
    public ScreenRectangle TreeBounds => tree.Bounds;
    public ScreenRectangle CopyBounds => copy.Bounds;
    public ScreenRectangle DetailsBounds => details.Bounds;
    public double DetailsScroll => details.Scroll;
    public string? LastCopyError { get; private set; }
    public string? LastCopiedPath { get; private set; }
    public string? FocusedPath => ui.Focus.FocusedId;
    public StationeryDeveloperView(GraphicsDevice graphics, ITextInputService input, Func<string, ITextRasterizer> rasterizerFactory, StationeryDeveloperStyle? style = null)
    {
        this.input = input;
        Style = style ?? StationeryDeveloperStyle.Load();
        var root = Style.Settings.Models[0].CreateTree();
        var splitNode = root.Resolve("/developerWindow/inspectorSplit")!;
        var treeNode = root.Resolve("/developerWindow/inspectorSplit/stationeryTree")!;
        var detailsNode = root.Resolve("/developerWindow/inspectorSplit/details")!;
        ui = new(graphics, input, rasterizerFactory, root);
        header = ui.AddTextBlock(root.Resolve("/developerWindow/instructions")!, new(), "F12 開発者ウィンドウ\n文房具を選択して Id・パス・位置を確認。F12 / Esc で閉じる。");
        split = ui.AddSplitPane(splitNode, new(), "階層と詳細", Style.SplitOptions);
        tree = ui.AddTree(treeNode, new(), "文房具の階層", Model.Tree);
        details = ui.AddTextBlock(detailsNode, new(), Model.Details);
        copy = ui.AddButton(root.Resolve("/developerWindow/copyPath")!, new(), "パスをコピー", CopySelectedPath);
        ui.BindSplitContent(split, tree, details);
        ui.Focus.Focus(tree.Path);
    }
    public void Refresh(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        var previousTree = Model.Tree;
        Model.Refresh(entries); tree.Tree = Model.Tree;
        if (previousTree != Model.Tree) RevealSelection();
        details.Label = Model.Details;
    }
    public DeveloperViewState Capture(bool visible = true) => Model.Capture(SplitRatio, visible);
    public void Restore(DeveloperViewState? state)
    {
        Model.Restore(state);
        RevealSelection();
        if (state is not null && double.IsFinite(state.SplitRatio)) split.Split!.SetRatio(state.SplitRatio);
    }
    private void RevealSelection()
    {
        var index = Model.Tree.VisibleRows().ToList().FindIndex(row => row.Item == Model.Tree.SelectedItem);
        tree.TreeScroll = Math.Max(0, index) * Math.Max(32, Theme.FontSize * 1.5 + 8);
    }
    public void CopySelectedPath()
    {
        if (Model.SelectedPath is not { } path) return;
        try { input.WriteClipboard(path); LastCopiedPath = path; LastCopyError = null; copy.Label = "コピーしました"; }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        { LastCopyError = ex.Message; copy.Label = "コピー失敗：クリックで再試行"; }
    }
    public void Update(GameTime time, bool active, KeyboardState keyboard, MouseState mouse, int width, int height)
    {
        var layout = StationeryLayoutEngine.Arrange(Style.Settings, width, height);
        header.Bounds = layout.ContentBounds[header.Path];
        split.Bounds = layout.ContentBounds[split.Path];
        copy.Bounds = layout.ContentBounds[copy.Path];
        ui.Focus.SetEnabled(copy.Path, Model.SelectedPath is not null);
        var before = Model.SelectedPath;
        ui.Update(time, active, keyboard, mouse);
        if (before != Model.SelectedPath) { details.Scroll = 0; copy.Label = "パスをコピー"; }
        details.Label = Model.Details;
    }
    public void Draw() => ui.Draw();
    public void Dispose() => ui.Dispose();
}
