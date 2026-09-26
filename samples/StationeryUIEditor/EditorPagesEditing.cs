using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Controls;
using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.Editor;
using StationeryUI.Windows;
using System.Text.Json;

internal sealed partial class EditorGame
{
    private StationeryUiHost? pageDialog;
    private StationeryUiHost.Element? pageNameField, pageList, pageFeedback;
    private string? pageAction;
    private bool pageDeleteArmed;
    private string? armedPageName;

    private void OpenPageDialog()
    {
        Capture();
        _ = blueprint.PageNames;
        SuspendBackgroundInput();
        pageAction = null; pageDeleteArmed = false; armedPageName = null;
        pageDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
        pageDialog.AddTextBlock(pageDialog.Root.AddChild("pageBackground", "textBlock"), new(430, 150, 740, 600), "");
        pageDialog.AddTextBlock(pageDialog.Root.AddChild("pageHeading", "textBlock"), new(454, 174, 690, 48), "ページの追加・削除");
        pageNameField = pageDialog.AddTextBox("pageName", new(454, 232, 450, 48), "新しいページ名", "newPage", 100);
        pageDialog.AddButton("addPage", new(924, 232, 220, 48), "ページを追加", () => pageAction = "add");
        var tree = new TreeView();
        foreach (var name in blueprint.PageNames) tree.AddNode(name, name);
        pageList = pageDialog.AddTree("pageList", new(454, 298, 690, 270), "ページ一覧", tree);
        pageFeedback = pageDialog.AddTextBlock(pageDialog.Root.AddChild("pageFeedback", "textBlock"), new(454, 582, 690, 50),
            "削除するページを一覧から選んでください。");
        pageDialog.AddButton("deletePage", new(454, 654, 330, 48), "選択したページを削除", () => pageAction = "delete");
        pageDialog.AddButton("closePages", new(814, 654, 330, 48), "閉じる", () => pageAction = "close");
    }

    private void UpdatePageDialog(GameTime time, double scale)
    {
        pageDialog!.Viewport.Scale = scale;
        if (PageSmoke && frames == 4)
        {
            SetText(pageNameField!, "testPage");
            pageAction = "add";
        }
        if (DeletePageSmoke && frames is 14 or 15)
        {
            if (frames == 14)
            {
                var item = pageList!.Tree!.Roots.Single(item => item.Id == "vTestPage");
                pageList.Tree.SetTarget(item);
            }
            pageAction = "delete";
        }
        pageDialog.Update(time, IsActive || !string.IsNullOrEmpty(smokeOutput), Keyboard.GetState(), Mouse.GetState());
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) pageAction = "close";
        var action = pageAction; pageAction = null;
        if (action is null) return;
        if (action == "close") { pageDialog.Dispose(); pageDialog = null; return; }
        try
        {
            if (action == "delete")
            {
                var selected = pageList!.Tree?.TargetItem?.Id;
                if (selected is null) throw new ArgumentException("削除するページを選んでください。");
                if (!pageDeleteArmed || armedPageName != selected)
                {
                    pageDeleteArmed = true;
                    armedPageName = selected;
                    pageFeedback!.Label = selected + " を削除します。もう一度、削除ボタンを押してください。";
                    return;
                }
                var candidate = StyleBlueprint.Parse(blueprint.BuildJson());
                candidate.DeletePage(selected);
                blueprint = candidate;
                message = selected + " を削除しました。";
            }
            else
            {
                var candidate = StyleBlueprint.Parse(blueprint.BuildJson());
                candidate.AddPage(pageNameField!.Editor!.Text);
                blueprint = candidate;
                message = "ページを追加しました。";
            }
            pageDialog.Dispose(); pageDialog = null;
            treeJson = null; lastTreeSelection = null;
            BuildUi();
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException)
        {
            pageDeleteArmed = false;
            armedPageName = null;
            pageFeedback!.Label = ex.Message;
        }
    }
}
