using StationeryUI.MonoGame;
using StationeryUI.Theming;
using StationeryUI.Windows;

internal sealed partial class EditorGame
{
    private const int ApplicationBarHeight = 40;
    private int BodyTop => editingPage ? ApplicationBarHeight : 0;
    private StationeryUiHost applicationBar = null!;
    private StationeryUiHost.Element applicationBackground = null!, restoreButton = null!;
    private StationeryUiHost.Element liveStatusLabel = null!, externalConflictButton = null!;
    private bool applicationBarActive;

    private void BuildApplicationBar()
    {
        applicationBar = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
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
        applicationBar.AddButton("export", new(552, 4, 144, 32), "エクスポート", () => pendingPage = () => Guard(OpenExportDialog));
        pageButton = applicationBar.AddButton("pages", new(704, 4, 144, 32), "ページ編集", () => pendingPage = () => Guard(OpenPageDialog));
        externalConflictButton = applicationBar.AddButton("externalConflict", new(), "外部変更に対処",
            () => pendingPage = OpenExternalConflictDialog);
        liveStatusLabel = applicationBar.AddTextBlock(applicationBar.Root.AddChild("liveStatus", "textBlock"), new(), "");
    }

    private void ArrangeApplicationBar()
    {
        applicationBar.Theme = theme with { FontSize = 14, Padding = 2 };
        applicationBackground.Bounds = new(0, 0, GraphicsDevice.Viewport.Width, ApplicationBarHeight);
        applicationBar.Focus.SetEnabled(restoreButton.Path, saveSession is not null);
        applicationBar.Focus.SetEnabled(pageButton.Path, blueprint.CanEditPages);
        externalConflictButton.Bounds = externalConflict ? new(856, 4, 170, 32) : new();
        applicationBar.Focus.SetEnabled(externalConflictButton.Path, externalConflict);
        var liveX = externalConflict ? 1034 : 856;
        liveStatusLabel.Bounds = new(liveX, 4, Math.Max(0, GraphicsDevice.Viewport.Width - liveX - 8), 32);
        liveStatusLabel.Label = liveStyleStatus;
    }
}
