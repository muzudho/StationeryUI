using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using StationeryUI.Windows;
using StationeryUI.Styling;
using StationeryUI.Canvas;

internal sealed partial class Demo
{
    private string activePage = "topDemoPage";
    private StationeryUiHost? splitUi;
    private StationeryUiHost.Element forwardLink = null!, backLink = null!, verticalSplit = null!, horizontalSplit = null!;
    private StationeryUiHost.Element topToolHint = null!, splitToolHint = null!;
    private readonly List<StationeryUiHost.Element> splitElements = [];

    private void Navigate(string page)
    {
        activePage = page;
        SyncSelectedTab();
        popupOpen = false;
        Window.Title = page switch
        {
            "layoutDemoPage" => "StationeryUI — レイアウトデモ",
            "splitPaneDemoPage" => "StationeryUI — スプリットペーンデモページ",
            _ => "StationeryUI — トップデモページ"
        };
    }
    private void SyncSelectedTab()
    {
        if (styles is null) return;
        var index = activePage switch
        {
            "topDemoPage" => 0,
            "splitPaneDemoPage" => 1,
            "layoutDemoPage" => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(activePage), activePage, "Unknown demo page.")
        };
        var layout = styles.Current.Layouts.SingleOrDefault(item => item.Path == "/tabbedPages" && item.Type == "tabbed-box-layout");
        if (layout is not null) layout.SelectedTabIndex = index;
    }
    private void CreatePages()
    {
        forwardLink = ui!.AddLink(modelBinding.Main["splitPaneDemoLink"], new(), "リンク：スプリットペーン", () => Navigate("splitPaneDemoPage"));
        styledElements.Add(forwardLink);
        splitUi = new(GraphicsDevice, input!, family => new WindowsTextRasterizer(family), modelBinding.SplitPage);
        backLink = splitUi.AddLink(modelBinding.SplitControls["topDemoLink"], new(), "← トップデモページに戻る", () => Navigate("topDemoPage"));
        backLink.LayoutKey = "lytSplitPaneDemoPage";
        verticalSplit = splitUi.AddSplitPane(modelBinding.SplitControls["verticalSplit"], new(), "垂直分割（左右）", new());
        horizontalSplit = splitUi.AddSplitPane(modelBinding.SplitControls["horizontalSplit"], new(), "水平分割（上下）", new(true));
        var left = splitUi.AddTextBox(modelBinding.SplitControls["leftPane"], new(), "左ペーン", "垂直分割：左ペーン");
        var right = splitUi.AddTextBox(modelBinding.SplitControls["rightPane"], new(), "右ペーン", "右ペーン：縦線をドラッグ");
        var top = splitUi.AddTextBox(modelBinding.SplitControls["topPane"], new(), "上ペーン", "水平分割：上ペーン");
        var bottom = splitUi.AddTextBox(modelBinding.SplitControls["bottomPane"], new(), "下ペーン", "下ペーン（中央の横線をドラッグ）");
        splitUi.BindSplitContent(verticalSplit, left, right);
        splitUi.BindSplitContent(horizontalSplit, top, bottom);
        splitElements.AddRange(new[] { backLink, verticalSplit, horizontalSplit, left, right, top, bottom });
        topToolHint = ui.AddTextBlock(modelBinding.Main["toolHint"], new(), "");
        splitToolHint = splitUi.AddTextBlock(modelBinding.SplitControls["toolHint"], new(), "");
        styledElements.Add(topToolHint);
        splitElements.Add(splitToolHint);
        foreach (var element in styledElements.Concat(splitElements))
            element.ToolHint = element.Node.Kind switch
            {
                "button" => element.Id switch
                {
                    "themeButton" => "明るいテーマと暗いテーマを切り替えます。",
                    "scaleButton" => "文字と UI の拡大率を切り替えます。",
                    "applyTitleButton" => "名前欄の内容をウィンドウのタイトルに反映します。",
                    "openDialogButton" => "編集用のダイアログを開きます。",
                    _ => element.Label
                },
                "link" => element == forwardLink ? "スプリットペーンデモページへ移動します。" : "トップデモページへ戻ります。",
                "textBox" => "クリックしてテキストを編集できます。",
                "splitPane" => "仕切りをドラッグしてペーンの大きさを変更できます。",
                "tree" => "＋／－で子ノードを開閉できます。",
                _ => null
            };
        CreateLayoutPage();
        topToolHint.ControlHandle = splitToolHint.ControlHandle = layoutToolHint.ControlHandle = "ctrlToolHint";
        topToolHint.LayoutKey = "lytTopDemoPage";
        splitToolHint.LayoutKey = "lytSplitPaneDemoPage";
        layoutToolHint.LayoutKey = "lytLayoutDemoPage";
        layoutLink.ToolHint = "ボックスとグリッドの入れ子を、レイアウトデモページで確認できます。";
        Navigate("topDemoPage");
    }
    private void UpdateToolHints()
    {
        layoutToolHint.Label = layoutUi!.HoveredToolHint ?? "各セルにマウスを合わせると説明を表示します。ウィンドウのサイズ変更で伸縮、F12 で文房具の Id を確認できます。";
        topToolHint.Label = popupOpen ? "" : ui!.HoveredToolHint ?? "ツールヒント：ボタンなどにマウスを合わせると説明を表示します。";
        splitToolHint.Label = splitUi!.HoveredToolHint ?? "ツールヒント：ボタンなどにマウスを合わせると説明を表示します。";
    }
    private void ApplySplitStyles(StationeryLayoutResult arranged)
    {
        splitUi!.Theme = ui!.Theme;
        splitUi.Viewport.Scale = requestedScale;
        foreach (var element in splitElements)
        {
            var bounds = element.Split is not null ? arranged.ContentBounds[element.Path] : BoundsFor(element, arranged);
            element.Bounds = new(bounds.X / requestedScale, bounds.Y / requestedScale, bounds.Width / requestedScale, bounds.Height / requestedScale);
            if (element.Split is not null)
            {
                var binding = styles.Current.Bindings.Single(b => b.ModelPath == element.Path && b.FirstModel is not null);
                element.Split.Configure(styles.Current.Layouts.Single(l => l.Path == binding.Layout).Split!);
            }
        }
    }
    private static bool IsPageSmoke(string? scenario) => scenario is "page-hint" or "page-hint-clear" or "page-open" or "page-back" or "page-keyboard" or "split-vertical" or "split-horizontal" or "split-keyboard" or "layout-open";
    private void PreparePageSmoke(string? scenario, bool smoke, ref KeyboardState keyboard, ref MouseState mouse)
    {
        if (!smoke || !IsPageSmoke(scenario)) return;
        if (scenario is "page-hint" or "page-hint-clear")
        {
            var target = styledElements.Single(e => e.Id == "themeButton");
            var box = ui!.Viewport.ToWindow(scenario == "page-hint-clear" && updateFrames >= 4 ? topToolHint.Bounds : target.Bounds);
            mouse = new((int)(box.X + 24), (int)(box.Y + 24), 0, ButtonState.Released,
                ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            keyboard = new();
            return;
        }
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
        var bounds = ui!.Viewport.ToWindow(scenario == "layout-open" ? layoutLink.Bounds : forwardLink.Bounds);
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
        if (scenario is "page-hint" or "page-hint-clear")
        {
            var box = ui!.Viewport.ToWindow(topToolHint.Bounds);
            if (box.X != 0 || box.Width != GraphicsDevice.Viewport.Width || box.Height != 80 || box.Y + box.Height != GraphicsDevice.Viewport.Height)
                throw new InvalidOperationException("Inspector must fill the bottom 80 window pixels at any UI scale.");
            if (scenario == "page-hint" && topToolHint.Label != "明るいテーマと暗いテーマを切り替えます。" ||
                scenario == "page-hint-clear" && !topToolHint.Label.StartsWith("ツールヒント："))
                throw new InvalidOperationException("Tool hint hover/leave failed.");
            return;
        }
        if (activePage == "splitPaneDemoPage" && splitToolHint.Bounds.Height != 0)
            throw new InvalidOperationException("Fullscreen inspector must be hidden.");
        var expectedPage = scenario == "layout-open" ? "layoutDemoPage" : scenario is "page-back" or "page-keyboard" ? "topDemoPage" : "splitPaneDemoPage";
        if (activePage != expectedPage) throw new InvalidOperationException("Page navigation failed.");
        if (scenario == "layout-open")
        {
            var hint = layoutUi!.Viewport.ToWindow(layoutToolHint.Bounds);
            if (hint.X != 0 || hint.Width != GraphicsDevice.Viewport.Width || hint.Height != 80 || hint.Y + hint.Height != GraphicsDevice.Viewport.Height)
                throw new InvalidOperationException("Layout demo dock must reserve the bottom 80 pixels.");
            if (layoutElements.Any(e => e != layoutToolHint && layoutUi.Viewport.ToWindow(e.Bounds).Y + layoutUi.Viewport.ToWindow(e.Bounds).Height > hint.Y))
                throw new InvalidOperationException("Layout demo content overlaps the docked inspector.");
        }
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
