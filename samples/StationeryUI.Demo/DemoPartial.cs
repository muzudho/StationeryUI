using StationeryUI.MonoGame;
using StationeryUI.Styling;

internal sealed partial class Demo
{
    private const string PartialTarget = "/vMainViewport/vPartialDemoPage/vBody/vContentSwitcher";
    private StationeryUiHost? partialUi;
    private StationeryUiHost.Element partialLink = null!, partialBackLink = null!, partialToolHint = null!;
    private StationeryUiHost.Element summaryLink = null!, detailsLink = null!, summaryText = null!, detailsText = null!;
    private readonly List<StationeryUiHost.Element> partialElements = [];

    private void CreatePartialPage()
    {
        partialLink = ui!.AddLink(modelBinding.Main["partialDemoLink"], new(), "リンク：ページ内の部分切替", () => Navigate("partialDemoPage"));
        styledElements.Add(partialLink);
        partialUi = new(GraphicsDevice, input!, family => new StationeryUI.Windows.WindowsTextRasterizer(family), modelBinding.PartialPage);
        partialBackLink = partialUi.AddLink(modelBinding.PartialControls["topDemoLink"], new(), "← トップページへ", () => Navigate("topDemoPage"));
        partialBackLink.LayoutKey = "/ctrlViewPort/ctrlPartialDemoPage/ctrlBody";
        var title = partialUi.AddTextBlock(modelBinding.PartialControls["partialTitle"], new(), "ページ内の部分切替デモ — 左のリンクで右側だけが変わります");
        summaryLink = partialUi.AddLink(modelBinding.PartialControls["summaryLink"], new(), "概要を表示",
            () => viewNavigator!.SelectLink("/vMainViewport/vPartialDemoPage/vBody/vSummaryLink"));
        detailsLink = partialUi.AddLink(modelBinding.PartialControls["detailsLink"], new(), "詳細を表示",
            () => viewNavigator!.SelectLink("/vMainViewport/vPartialDemoPage/vBody/vDetailsLink"));
        summaryText = partialUi.AddTextBlock(modelBinding.PartialControls["summaryText"], new(),
            "概要\n\nTabbedBoxLayout がこの領域だけを切り替えます。\n見出し・リンク・下部の説明はそのままです。");
        detailsText = partialUi.AddTextBlock(modelBinding.PartialControls["detailsText"], new(),
            "詳細\n\nリンクの onClick は、切替先のパスと表示する子を指定します。\n選択状態は viewportsSnapshot に初期値を保存できます。");
        partialToolHint = partialUi.AddTextBlock(modelBinding.PartialControls["toolHint"], new(), "");
        partialToolHint.LayoutKey = "/ctrlViewPort/ctrlPartialDemoPage/ctrlInspectorPanel";
        partialElements.AddRange([partialBackLink, title, summaryLink, detailsLink, summaryText, detailsText, partialToolHint]);
        partialBackLink.ToolHint = "トップページへ戻ります。";
        summaryLink.ToolHint = "右側を概要に切り替えます。";
        detailsLink.ToolHint = "右側を詳細に切り替えます。";
    }

    private void ApplyPartialStyles(StationeryLayoutResult arranged)
    {
        partialUi!.Theme = ui!.Theme;
        partialUi.Viewport.Scale = requestedScale;
        foreach (var element in partialElements)
        {
            var bounds = BoundsFor(element, arranged);
            element.Bounds = new(bounds.X / requestedScale, bounds.Y / requestedScale,
                bounds.Width / requestedScale, bounds.Height / requestedScale);
        }
    }
}
