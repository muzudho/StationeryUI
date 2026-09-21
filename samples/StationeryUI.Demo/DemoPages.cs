using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using StationeryUI.Windows;
using StationeryUI.Styling;
using StationeryUI.Canvas;

internal sealed partial class Demo
{
    private string activePage = "topDemoPage";
    private DesktopUi? splitUi;
    private DesktopUi.Element forwardLink = null!, backLink = null!, verticalSplit = null!, horizontalSplit = null!;
    private readonly List<DesktopUi.Element> splitElements = [];

    private void Navigate(string page)
    {
        activePage = page;
        popupOpen = false;
        Window.Title = page == "topDemoPage" ? "StationeryUI — トップデモページ" : "StationeryUI — スプリットペーンデモページ";
    }
    private void CreatePages()
    {
        forwardLink = ui!.AddLink(modelBinding.Main["splitPaneDemoLink"], new(), "リンク：スプリットペーン", () => Navigate("splitPaneDemoPage"));
        styledElements.Add(forwardLink);
        splitUi = new(GraphicsDevice, input!, family => new WindowsTextRasterizer(family), modelBinding.SplitPage);
        backLink = splitUi.AddLink(modelBinding.SplitControls["topDemoLink"], new(), "← トップデモページに戻る", () => Navigate("topDemoPage"));
        verticalSplit = splitUi.AddSplitPane(modelBinding.SplitControls["verticalSplit"], new(), "垂直分割（左右）", new());
        horizontalSplit = splitUi.AddSplitPane(modelBinding.SplitControls["horizontalSplit"], new(), "水平分割（上下）", new(true));
        var left = splitUi.AddTextBox(modelBinding.SplitControls["leftPane"], new(), "左ペーン", "垂直分割：左ペーン");
        var right = splitUi.AddTextBox(modelBinding.SplitControls["rightPane"], new(), "右ペーン", "右ペーン：縦線をドラッグ");
        var top = splitUi.AddTextBox(modelBinding.SplitControls["topPane"], new(), "上ペーン", "水平分割：上ペーン");
        var bottom = splitUi.AddTextBox(modelBinding.SplitControls["bottomPane"], new(), "下ペーン", "下ペーン（中央の横線をドラッグ）");
        splitUi.BindSplitContent(verticalSplit, left, right);
        splitUi.BindSplitContent(horizontalSplit, top, bottom);
        splitElements.AddRange(new[] { backLink, verticalSplit, horizontalSplit, left, right, top, bottom });
        Navigate("topDemoPage");
    }
    private void ApplySplitStyles(StationeryLayoutResult arranged)
    {
        splitUi!.Theme = ui!.Theme;
        splitUi.Viewport.Scale = requestedScale;
        foreach (var element in splitElements)
        {
            var bounds = element.Split is not null ? arranged.ContentBounds[element.Path] : arranged.Bounds[element.Path];
            element.Bounds = new(bounds.X / requestedScale, bounds.Y / requestedScale, bounds.Width / requestedScale, bounds.Height / requestedScale);
            if (element.Split is not null)
            {
                var binding = styles.Current.Bindings.Single(b => b.ModelPath == element.Path && b.FirstModel is not null);
                element.Split.Configure(styles.Current.Layouts.Single(l => l.Id == binding.Layout).Split!);
            }
        }
    }
    private static bool IsPageSmoke(string? scenario) => scenario is "page-open" or "page-back" or "page-keyboard" or "split-vertical" or "split-horizontal" or "split-keyboard";
    private void PreparePageSmoke(string? scenario, bool smoke, ref KeyboardState keyboard, ref MouseState mouse)
    {
        if (!smoke || !IsPageSmoke(scenario)) return;
        if (scenario == "page-back" && updateFrames == 1)
        {
            name!.Editor!.SelectAll(); name.Editor.Insert("ページを戻っても保持");
            sampleTree!.Tree!.SetExpanded(sampleTree.Tree.Roots[0], false);
        }
        keyboard = new();
        if (scenario == "page-keyboard")
        {
            mouse = new();
            if (updateFrames == 1) ui!.Focus.Focus(forwardLink.Path);
            if (updateFrames == 4) splitUi!.Focus.Focus(backLink.Path);
            if (updateFrames is 2 or 5) keyboard = new(Keys.Enter);
            return;
        }
        if (scenario == "split-keyboard" && updateFrames >= 4)
        {
            mouse = new();
            splitUi!.Focus.Focus(verticalSplit.Path);
            if (updateFrames == 5) keyboard = new(Keys.Right);
            return;
        }
        var bounds = ui!.Viewport.ToWindow(forwardLink.Bounds);
        var down = updateFrames == 2;
        if (updateFrames >= 4 && scenario == "page-back")
        {
            bounds = splitUi!.Viewport.ToWindow(backLink.Bounds);
            down = updateFrames == 5;
        }
        if (updateFrames >= 5 && scenario is "split-vertical" or "split-horizontal")
        {
            var element = scenario == "split-vertical" ? verticalSplit : horizontalSplit;
            var area = splitUi!.Viewport.ToWindow(element.Bounds);
            bounds = element.Split!.Arrange(area).Divider;
            var x = bounds.X + bounds.Width / 2;
            var y = bounds.Y + bounds.Height / 2;
            if (updateFrames >= 6)
            {
                x = element.Split.Options.Horizontal ? x : area.X + area.Width * .7;
                y = element.Split.Options.Horizontal ? area.Y + area.Height * .7 : y;
            }
            mouse = new((int)x, (int)y, 0, updateFrames is 5 or 6 ? ButtonState.Pressed : ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            return;
        }
        mouse = new((int)(bounds.X + 24), (int)(bounds.Y + 24), 0, down ? ButtonState.Pressed : ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    }
    private void ValidatePageSmoke(string? scenario)
    {
        if (!IsPageSmoke(scenario)) return;
        if (activePage != (scenario is "page-back" or "page-keyboard" ? "topDemoPage" : "splitPaneDemoPage")) throw new InvalidOperationException("Page navigation failed.");
        if (scenario == "split-keyboard" && verticalSplit.Split!.Ratio <= .5) throw new InvalidOperationException("Split keyboard adjustment failed.");
        if (scenario == "split-vertical" && verticalSplit.Split!.Ratio < .65 || scenario == "split-horizontal" && horizontalSplit.Split!.Ratio < .65)
            throw new InvalidOperationException("Split divider drag failed.");
        if (name!.Editor!.Text != (scenario == "page-back" ? "ページを戻っても保持" : "文房具UIへようこそ") ||
            sampleTree!.Tree!.Roots[0].IsExpanded == (scenario == "page-back"))
            throw new InvalidOperationException("Hidden page state was changed.");
        var entries = InspectStationery();
        if (entries.Single(e => e.Path == modelBinding.TopPage.Path).Visible != (activePage == "topDemoPage"))
            throw new InvalidOperationException("Inactive page visibility failed.");
    }
}
