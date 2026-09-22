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
    private readonly StationeryUiHost ui;
    private readonly ITextInputService input;
    public DeveloperOperationLog OperationLog { get; } = new();
    private readonly StationeryUiHost.Element header, split, tree, details, copy, capture, toolHint;
    public ScreenRectangle InspectorPanelBounds { get; private set; }
    public ScreenRectangle ToolHintBounds => toolHint.Bounds;
    public string ToolHintText => toolHint.Label;
    public bool CaptureEnabled { get; private set; }
    public ScreenRectangle CaptureBounds => capture.Bounds;
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
        var splitNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit")!;
        var treeNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit/stationeryTree")!;
        var detailsNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit/details")!;
        ui = new(graphics, input, rasterizerFactory, root);
        header = ui.AddTextBlock(root.Resolve("/developerViewport/developerWindow/instructions")!, new(), "開発者ウィンドウ");
        split = ui.AddSplitPane(splitNode, new(), "階層と詳細", Style.SplitOptions);
        tree = ui.AddTree(treeNode, new(), "文房具の階層", Model.Tree);
        details = ui.AddTextBlock(detailsNode, new(), Model.Details);
        copy = ui.AddButton(root.Resolve("/developerViewport/developerWindow/copyPath")!, new(), "パスをコピー", CopySelectedPath);
        var captureNode = root.Resolve("/developerViewport/developerWindow/capture")
            ?? root.Resolve("/developerViewport/developerWindow")!.AddChild("capture", "button");
        capture = ui.AddButton(captureNode, new(), "キャプチャー", () => CaptureEnabled = !CaptureEnabled);
        capture.ToolHint = "キャプチャー：画面上の文房具を選択。もう一度押すと解除。";
        toolHint = ui.AddTextBlock(root.Resolve("/developerViewport/developerWindow/inspectorPanel/toolHint")!, new(), "");
        tree.ToolHint = "文房具を選ぶと Id・パス・位置を表示します。矢印キーで移動、＋／－で枝を開閉。";
        split.ToolHint = "仕切りをドラッグ、または左右キーで階層と詳細の幅を調整します。";
        details.ToolHint = "詳細はスクロールできます。Ctrl+C で詳細全体をコピーします。";
        copy.ToolHint = "選択した文房具の完全パスをクリップボードへコピーします。";
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
    public DeveloperViewState Capture(bool visible = true) => Model.Capture(SplitRatio, visible) with { CaptureEnabled = CaptureEnabled };
    public void Restore(DeveloperViewState? state)
    {
        Model.Restore(state);
        CaptureEnabled = state?.CaptureEnabled ?? false;
        RevealSelection();
        if (state is not null && double.IsFinite(state.SplitRatio)) split.Split!.SetRatio(state.SplitRatio);
    }
    public void SelectCaptured(string path)
    {
        if (!Model.Select(path)) return;
        RevealSelection();
        ui.Focus.Focus(tree.Path);
        details.Scroll = 0;
        details.Label = Model.Details;
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
        latestLayout = layout;
        header.Bounds = layout.ContentBounds[header.Path];
        capture.Bounds = new(header.Bounds.X, header.Bounds.Y, 48, 48);
        header.Bounds = new(header.Bounds.X + 56, header.Bounds.Y, Math.Max(0, header.Bounds.Width - 56), header.Bounds.Height);
        split.Bounds = layout.ContentBounds[split.Path];
        copy.Bounds = layout.ContentBounds[copy.Path];
        InspectorPanelBounds = layout.Bounds["/developerViewport/developerWindow/inspectorPanel"];
        toolHint.Bounds = layout.ContentBounds[toolHint.Path];
        ui.Focus.SetEnabled(copy.Path, Model.SelectedPath is not null);
        var before = Model.SelectedPath;
        ui.Update(time, active, keyboard, mouse);
        toolHint.Label = ui.HoveredToolHint ?? (CaptureEnabled
            ? "キャプチャー中：画面上の文房具をクリック。手のボタンで解除。"
            : "手のボタンでキャプチャー。F12 / Esc で閉じる。");
        if (before != Model.SelectedPath) { details.Scroll = 0; copy.Label = "パスをコピー"; }
        details.Label = Model.Details;
        var hit = DeveloperCapture.HitTest(ui.Inspect(), mouse.X, mouse.Y);
        OperationLog.Record(mouse, keyboard, new(active, hit?.Path, ui.Focus.FocusedId,
            Model.SelectedPath, Model.Tree.TargetItem is { } target ? Model.PathFor(target) : null,
            CaptureEnabled, SplitRatio, tree.TreeScroll, details.Scroll,
            string.Join("\n", Model.Capture().CollapsedPaths.OrderBy(path => path, StringComparer.Ordinal)), width, height)
            { TreeHorizontalScroll = tree.TreeHorizontalScroll });
    }
    private StationeryLayoutResult? latestLayout;
    public void Draw()
    {
        ui.Draw();
        ui.DrawCaptureIcon(capture.Bounds, CaptureEnabled, ui.Focus.FocusedId == capture.Path);
        if (latestLayout is not null) ui.DrawPanelBorders(latestLayout);
    }
    public void Dispose() { OperationLog.Dispose(); ui.Dispose(); }
}
