namespace StationeryUI.MonoGame;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Canvas;
using StationeryUI.Controls;
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
    private readonly StationeryUiHost.Element header, split, tree, details, copy, capture, toolHint, modelTreeButton, layoutTreeButton, documentTreeButton;
    private TreeView? documentTree;
    private Func<TreeItem?, string>? documentDetails;
    private Func<TreeItem?, string?>? documentInspectionPath, documentCopyPath;
    private Func<TreeItem?, IReadOnlyList<string>>? documentInspectionPaths;
    private Func<string, bool>? documentContainsInspectionPath;
    /// <summary>Set only when document nodes are being compared with a host snapshot.</summary>
    public string? DocumentComparisonLabel { get; set; }
    public bool DocumentTreeMode { get; private set; }
    public ScreenRectangle InspectorPanelBounds { get; private set; }
    public ScreenRectangle ToolHintBounds => toolHint.Bounds;
    public string ToolHintText => toolHint.Label;
    public bool CaptureEnabled { get; private set; }
    public bool EmbeddedInEditor { get; set; }
    public ScreenRectangle CaptureBounds => capture.Bounds;
    public StationeryDeveloperStyle Style { get; }
    public DeveloperInspectionModel Model { get; } = new();
    public StationeryTheme Theme { get => ui.Theme; set => ui.Theme = value; }
    public double SplitRatio => split.Split!.Ratio;
    public ScreenRectangle SplitBounds => split.Bounds;
    public ScreenRectangle ModelTreeButtonBounds => modelTreeButton.Bounds;
    public ScreenRectangle LayoutTreeButtonBounds => layoutTreeButton.Bounds;
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
        Model.SetTreeMode(DeveloperTreeMode.Layout);
        var root = Style.Settings.ModelTree.CreateTree();
        var splitNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit")!;
        var treeNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit/stationeryTree")!;
        var detailsNode = root.Resolve("/developerViewport/developerWindow/inspectorSplit/details")!;
        ui = new(graphics, input, rasterizerFactory, root);
        var theme = StationeryTheme.Light with { FontSize = 16, Padding = 4 };
        ui.Theme = theme with { Selected = theme.Surface };
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
        modelTreeButton = ui.AddButton(splitNode.AddChild("modelTreeMode", "button"), new(), "モデルツリー",
            () => SetTreeMode(DeveloperTreeMode.Model));
        layoutTreeButton = ui.AddButton(splitNode.AddChild("layoutTreeMode", "button"), new(), "レイアウトツリー",
            () => SetTreeMode(DeveloperTreeMode.Layout));
        documentTreeButton = ui.AddButton(splitNode.AddChild("documentTreeMode", "button"), new(), "JSON ツリー", SetDocumentTreeMode);
        ui.BindSplitContent(split, tree, details);
        ui.Focus.Focus(tree.Path);
    }
    public void SetTreeMode(DeveloperTreeMode mode)
    {
        DocumentTreeMode = false;
        Model.SetTreeMode(mode);
        tree.Tree = Model.Tree;
        tree.TreeHorizontalScroll = 0;
        details.Scroll = 0;
        RevealSelection();
        details.Label = Model.Details;
        details.BoxModel = Model.SelectedEntry?.BoxModel;
    }
    public void SetDocumentTree(TreeView? source, Func<TreeItem?, string>? describe = null,
        Func<TreeItem?, string?>? inspectPath = null, Func<TreeItem?, string?>? copyPath = null,
        Func<TreeItem?, IReadOnlyList<string>>? inspectPaths = null,
        Func<string, bool>? containsInspectionPath = null)
    {
        documentTree = source;
        documentDetails = describe;
        documentInspectionPath = inspectPath;
        documentCopyPath = copyPath;
        documentInspectionPaths = inspectPaths;
        documentContainsInspectionPath = containsInspectionPath;
        if (source is null && DocumentTreeMode) SetTreeMode(Model.TreeMode);
        else if (DocumentTreeMode && source is not null)
        {
            tree.Tree = source;
            SelectDocumentInspection(source.TargetItem);
            UpdateDetails();
        }
    }
    public void SetDocumentTreeMode()
    {
        if (documentTree is null) return;
        DocumentTreeMode = true;
        tree.Tree = documentTree;
        tree.TreeHorizontalScroll = 0;
        tree.TreeScroll = 0;
        details.Scroll = 0;
        SelectDocumentInspection(documentTree.TargetItem);
        UpdateDetails();
    }
    private void UpdateDetails()
    {
        if (DocumentTreeMode)
        {
            var selected = documentTree?.TargetItem;
            var description = documentDetails?.Invoke(selected) ?? "JSON の項目を選択してください。";
            if (DocumentComparisonLabel is not null)
            {
                var primary = documentInspectionPath?.Invoke(selected);
                IEnumerable<string> paths = primary is null ? documentInspectionPaths?.Invoke(selected) ?? []
                    : new[] { primary }.Concat(documentInspectionPaths?.Invoke(selected) ?? []);
                var candidates = paths.Distinct(StringComparer.Ordinal).ToArray();
                if (candidates.Length > 0)
                {
                    var found = candidates.Count(Model.Contains);
                    description += $"\n\n{DocumentComparisonLabel}の対応: "
                        + (found == 0 ? "未接続" : $"{found} / {candidates.Length} 件");
                }
            }
            details.Label = description;
        }
        else
        {
            details.Label = Model.Details;
            if (DocumentComparisonLabel is not null && Model.SelectedPath is { } path
                && documentContainsInspectionPath is not null && !documentContainsInspectionPath(path))
                details.Label += "\n\nファイル上の対応: なし（実行時のみ）";
        }
        details.BoxModel = DocumentTreeMode ? null : Model.SelectedEntry?.BoxModel;
    }
    private void SelectDocumentInspection(TreeItem? item)
    {
        var primary = documentInspectionPath?.Invoke(item);
        IEnumerable<string> candidates = primary is null ? documentInspectionPaths?.Invoke(item) ?? []
            : new[] { primary }.Concat(documentInspectionPaths?.Invoke(item) ?? []);
        foreach (var path in candidates.Distinct(StringComparer.Ordinal))
        {
            if (path.Contains(':') && Model.TreeMode != DeveloperTreeMode.Layout)
                Model.SetTreeMode(DeveloperTreeMode.Layout);
            if (Model.Select(path)) return;
        }
    }
    public void Refresh(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        var previousTree = Model.Tree;
        Model.Refresh(entries);
        if (!DocumentTreeMode) tree.Tree = Model.Tree;
        if (DocumentTreeMode)
        {
            SelectDocumentInspection(documentTree?.TargetItem);
        }
        else if (previousTree != Model.Tree) RevealSelection();
        UpdateDetails();
    }
    public DeveloperViewState Capture(bool visible = true) => Model.Capture(SplitRatio, visible)
        with { CaptureEnabled = CaptureEnabled, DocumentTreeMode = DocumentTreeMode };
    public void Restore(DeveloperViewState? state)
    {
        Model.Restore(state);
        DocumentTreeMode = state?.DocumentTreeMode == true && documentTree is not null;
        tree.Tree = DocumentTreeMode ? documentTree : Model.Tree;
        CaptureEnabled = state?.CaptureEnabled ?? false;
        if (!DocumentTreeMode) RevealSelection();
        UpdateDetails();
        if (state is not null && double.IsFinite(state.SplitRatio)) split.Split!.SetRatio(state.SplitRatio);
    }
    public void SelectCaptured(string path)
    {
        if (DocumentTreeMode) SetTreeMode(Model.TreeMode);
        if (!Model.Select(path)) return;
        RevealSelection();
        ui.Focus.Focus(tree.Path);
        details.Scroll = 0;
        details.Label = Model.Details;
        details.BoxModel = Model.SelectedEntry?.BoxModel;
    }
    private void RevealSelection()
    {
        var index = Model.Tree.VisibleRows().ToList().FindIndex(row => row.Item == Model.Tree.SelectedItem);
        tree.TreeScroll = Math.Max(0, index) * Math.Max(32, Theme.FontSize * 1.5 + 8);
    }
    public void CopySelectedPath()
    {
        var path = DocumentTreeMode ? documentCopyPath?.Invoke(documentTree?.TargetItem) : Model.SelectedPath;
        if (path is null) return;
        try { input.WriteClipboard(path); LastCopiedPath = path; LastCopyError = null; copy.Label = "コピーしました"; }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        { LastCopyError = ex.Message; copy.Label = "コピー失敗：クリックで再試行"; }
    }
    public void Update(GameTime time, bool active, KeyboardState keyboard, MouseState mouse, int width, int height)
    {
        header.Label = EmbeddedInEditor ? "文房具UIの検査" : "開発者ウィンドウ";
        var layout = StationeryLayoutEngine.Arrange(Style.Settings, width, height);
        latestLayout = layout;
        header.Bounds = layout.ContentBounds[header.Path];
        capture.Bounds = new(header.Bounds.X, header.Bounds.Y, 48, 48);
        header.Bounds = new(header.Bounds.X + 56, header.Bounds.Y, Math.Max(0, header.Bounds.Width - 56), header.Bounds.Height);
        var splitArea = layout.ContentBounds[split.Path];
        var toolbarHeight = Math.Min(48, splitArea.Height);
        var buttonWidth = Math.Min(260, splitArea.Width / (documentTree is null ? 2 : 3));
        modelTreeButton.Bounds = new(splitArea.X, splitArea.Y, buttonWidth, toolbarHeight);
        layoutTreeButton.Bounds = new(splitArea.X + buttonWidth, splitArea.Y, buttonWidth, toolbarHeight);
        documentTreeButton.Bounds = documentTree is null ? new() : new(splitArea.X + buttonWidth * 2, splitArea.Y, buttonWidth, toolbarHeight);
        modelTreeButton.Label = (!DocumentTreeMode && Model.TreeMode == DeveloperTreeMode.Model ? "● " : "") + "モデルツリー";
        layoutTreeButton.Label = (!DocumentTreeMode && Model.TreeMode == DeveloperTreeMode.Layout ? "● " : "") + "レイアウトツリー";
        documentTreeButton.Label = (DocumentTreeMode ? "● " : "") + "JSON ツリー";
        split.Bounds = new(splitArea.X, splitArea.Y + toolbarHeight, splitArea.Width, Math.Max(0, splitArea.Height - toolbarHeight));
        copy.Bounds = layout.ContentBounds[copy.Path];
        InspectorPanelBounds = layout.Bounds["/developerViewport/developerWindow/inspectorPanel"];
        toolHint.Bounds = layout.ContentBounds[toolHint.Path];
        ui.Focus.SetEnabled(copy.Path, DocumentTreeMode ? documentCopyPath?.Invoke(documentTree?.TargetItem) is not null
            : Model.SelectedPath is not null);
        ui.Focus.SetEnabled(documentTreeButton.Path, documentTree is not null);
        var before = Model.SelectedPath;
        var beforeDocument = documentTree?.TargetItem;
        ui.Update(time, active, keyboard, mouse);
        toolHint.Label = ui.HoveredToolHint ?? (CaptureEnabled
            ? "キャプチャー中：画面上の文房具をクリック。手のボタンで解除。"
            : DocumentTreeMode ? "JSON の項目を選ぶとパスと値を表示します。対応するモデルはプレビューにも反映します。"
            : EmbeddedInEditor ? "手のボタンでキャプチャー。編集を始めるには右上のボタンを押します。"
                : "手のボタンでキャプチャー。F12 / Esc で閉じる。");
        if (DocumentTreeMode && beforeDocument != documentTree?.TargetItem)
        {
            SelectDocumentInspection(documentTree?.TargetItem);
            details.Scroll = 0; copy.Label = "パスをコピー";
        }
        else if (!DocumentTreeMode && before != Model.SelectedPath) { details.Scroll = 0; copy.Label = "パスをコピー"; }
        UpdateDetails();
        var hit = DeveloperCapture.HitTest(ui.Inspect(), mouse.X, mouse.Y);
        OperationLog.Record(mouse, keyboard, new(active, hit?.Path, ui.Focus.FocusedId,
            Model.SelectedPath, Model.Tree.TargetItem is { } target ? Model.PathFor(target) : null,
            CaptureEnabled, SplitRatio, tree.TreeScroll, details.Scroll,
            string.Join("\n", Model.Capture().CollapsedPaths.OrderBy(path => path, StringComparer.Ordinal)), width, height)
            { TreeHorizontalScroll = tree.TreeHorizontalScroll, TreeMode = Model.TreeMode });
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
