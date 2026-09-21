using StationeryUI.MonoGame;
using StationeryUI.Theming;
using StationeryUI.Windows;

internal sealed partial class DesignerGame
{
    private const int ApplicationBarHeight = 40;
    private int BodyTop => editingPage ? ApplicationBarHeight : 0;
    private StationeryUiHost applicationBar = null!;
    private StationeryUiHost.Element applicationBackground = null!, restoreButton = null!;
    private bool applicationBarActive;

    private void BuildApplicationBar()
    {
        applicationBar = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        applicationBackground = applicationBar.AddTextBlock(applicationBar.Root.AddChild("applicationBar", "textBlock"), new(), "");
        applicationBar.AddButton("theme", new(8, 4, 144, 32), "明るい／暗い", () =>
        {
            theme = theme.Background == StationeryTheme.Light.Background
                ? StationeryTheme.Dark with { FontSize = 16, Padding = 4 }
                : StationeryTheme.Light with { FontSize = 16, Padding = 4 };
            ui.Theme = theme;
        });
        applicationBar.AddButton("back", new(160, 4, 168, 32), "1 ページ目へ戻る", () => { Capture(); pendingPage = BuildWelcome; });
        restoreButton = applicationBar.AddButton("restore", new(336, 4, 208, 32), "セーブポイントに戻す",
            () => pendingPage = () => Guard(OpenRestoreDialog));
    }

    private void ArrangeApplicationBar()
    {
        applicationBar.Theme = theme with { FontSize = 14, Padding = 2 };
        applicationBackground.Bounds = new(0, 0, GraphicsDevice.Viewport.Width, ApplicationBarHeight);
        applicationBar.Focus.SetEnabled(restoreButton.Path, saveSession is not null);
    }
}
