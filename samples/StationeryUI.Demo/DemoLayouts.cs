using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.Controls;

internal sealed partial class Demo
{
    private StationeryUiHost? layoutUi;
    private StationeryUiHost.Element layoutLink = null!, layoutBackLink = null!, layoutToolHint = null!;
    private readonly List<StationeryUiHost.Element> layoutElements = [];

    private void CreateLayoutPage()
    {
        layoutLink = ui!.AddLink(modelBinding.Main["layoutDemoLink"], new(), "リンク：レイアウトデモ", () => Navigate("layoutDemoPage"));
        styledElements.Add(layoutLink);
        layoutUi = new(GraphicsDevice, input!, family => new StationeryUI.Windows.WindowsTextRasterizer(family), modelBinding.LayoutPage);
        layoutBackLink = layoutUi.AddLink(modelBinding.LayoutControls["topDemoLink"], new(), "← トップページへ", () => Navigate("topDemoPage"));
        layoutBackLink.LayoutKey = "keyLayoutDemoPage";
        layoutBackLink.ToolHint = "トップページへ戻ります。入力内容は保持されます。";
        layoutElements.Add(layoutBackLink);
        var labels = new Dictionary<string, string>
        {
            ["title"] = "レイアウトデモ",
            ["boxTitle"] = "ボックスレイアウト",
            ["boxContent"] = "boxLayout\npadding: 24px\n\nboxContent を直接配置",
            ["gridTitle"] = "グリッドレイアウト：3 行 × 3 列",
            ["spanCell"] = "入れ子 A\nrow: 0\ncol: 0\nrowspan: 2\n\nセル直下",
            ["nestedA"] = "入れ子 B\n2 × 2 グリッド\nセル (0, 0)",
            ["nestedB"] = "セル (0, 1)",
            ["nestedC"] = "セル (1, 0)",
            ["nestedD"] = "セル (1, 1)",
            ["gridFooter"] = "3 列にまたがるセル：row: 2 / col: 0 / colspan: 3"
        };
        foreach (var (id, label) in labels)
        {
            var element = layoutUi.AddTextBlock(modelBinding.LayoutControls[id], new(), label);
            element.ToolHint = id switch
            {
                "boxContent" => "boxLayout の padding 24px の内側に、boxContent を直接配置しています。",
                "spanCell" => "親グリッドの row: 0 / col: 0 から 2 行にまたがるセルへ、直接配置しています。",
                "nestedA" or "nestedB" or "nestedC" or "nestedD" => "親の row: 0 / col: 1 / rowspan: 2 / colspan: 2 に、2 × 2 グリッドを入れています。",
                "gridFooter" => "colspan: 3 で、親グリッドの最下行を横いっぱいに使っています。",
                _ => "ウィンドウのサイズを変えると、各レイアウトが利用できる領域に合わせて伸縮します。"
            };
            layoutElements.Add(element);
        }
        layoutToolHint = layoutUi.AddTextBlock(modelBinding.LayoutControls["toolHint"], new(), "");
        layoutElements.Add(layoutToolHint);
    }

    private void ApplyLayoutStyles(StationeryLayoutResult arranged)
    {
        layoutUi!.Theme = ui!.Theme;
        layoutUi.Viewport.Scale = requestedScale;
        foreach (var element in layoutElements)
        {
            var bounds = BoundsFor(element, arranged);
            element.Bounds = new(bounds.X / requestedScale, bounds.Y / requestedScale, bounds.Width / requestedScale, bounds.Height / requestedScale);
            ButtonColor? surface = StationeryControlHandle.ModelRoleFromId(element.Id) switch
            {
                "boxContent" => new(38, 77, 68),
                "spanCell" => new(64, 64, 98),
                "nestedA" or "nestedD" => new(35, 74, 98),
                "nestedB" or "nestedC" => new(42, 90, 111),
                "gridFooter" => new(96, 65, 42),
                _ => null
            };
            element.Theme = surface is { } color ? ui.Theme with { Surface = color, Text = new(255, 255, 255) } : null;
        }
    }
}
