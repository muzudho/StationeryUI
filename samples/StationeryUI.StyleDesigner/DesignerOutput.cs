using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using StationeryUI.Windows;

internal sealed partial class DesignerGame
{
    private string? selectedOutputFolder;
    private StationeryUiHost? utilityDialog;
    private bool exportDialog;
    private Action? utilityAction;
    private StationeryUiHost.Element? createFileButton;

    private void StartUtilityDialog(string title, bool exporting)
    {
        SuspendBackgroundInput();
        exportDialog = exporting; utilityAction = null;
        utilityDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        utilityDialog.Viewport.Scale = BodyScale;
        utilityDialog.AddTextBlock(utilityDialog.Root.AddChild("dialogBackground", "textBlock"), new(180, 200, 1240, 380), "");
        utilityDialog.AddTextBlock(utilityDialog.Root.AddChild("dialogTitle", "textBlock"), new(204, 220, 1192, 60), title);
        utilityDialog.AddButton("cancel", new(1136, 512, 260, 44), "閉じる", () => utilityAction = CloseUtilityDialog);
    }

    private void OpenExportDialog()
    {
        Capture(); ValidateGridCounts();
        StartUtilityDialog("エクスポート", true);
        var dialog = utilityDialog!;
        dialog.AddTextBlock(dialog.Root.AddChild("outputTitle", "textBlock"), new(204, 300, 120, 44), "出力先：");
        output = dialog.AddTextBox("output", new(328, 300, 1068, 44), "出力先のパス", outputPath, 4096);
        dialog.AddButton("writeOutput", new(204, 512, 240, 44), "書き出す", () => utilityAction = () => Guard(() =>
        {
            outputPath = output.Editor!.Text;
            ExportAndTrack(outputPath);
        }));
        dialog.AddButton("chooseFolder", new(204, 368, 280, 44), "フォルダーを選ぶ", () => utilityAction = () => Guard(() =>
        {
            var folder = WindowsStyleFileDialog.ChooseFolder(selectedOutputFolder);
            if (folder is null) return;
            selectedOutputFolder = folder;
            outputPath = Path.Combine(folder, "my-plan.stationery-style.json");
            SetText(output, outputPath);
            message = "出力フォルダーを選択しました。";
        }));
        dialog.AddButton("chooseFile", new(496, 368, 280, 44), "ファイルを選ぶ", () => utilityAction = () => Guard(() =>
        {
            var path = ChooseStyleFile();
            if (path is not null) OpenStyle(path);
        }));
        createFileButton = dialog.AddButton("createFile", new(788, 368, 240, 44), "新規作成", () => utilityAction = () => Guard(() =>
        {
            if (selectedOutputFolder is null) return;
            var path = Path.Combine(selectedOutputFolder, "my-plan.stationery-style.json");
            for (var suffix = 2; File.Exists(path) || Directory.Exists(path); suffix++)
                path = Path.Combine(selectedOutputFolder, $"my-plan-{suffix}.stationery-style.json");
            ExportAndTrack(path);
        }));
        dialog.Focus.SetEnabled(createFileButton.Path, selectedOutputFolder is not null);
    }

    private void CloseUtilityDialog()
    {
        if (exportDialog) outputPath = output.Editor!.Text;
        else ResetGridCounts();
        utilityDialog?.Dispose(); utilityDialog = null; utilityAction = null;
        exportDialog = false;
    }

    private void UpdateUtilityDialog(GameTime time)
    {
        utilityDialog!.Viewport.Scale = BodyScale;
        inspectorMouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        PrepareUtilitySmoke(ref inspectorMouse, ref keyboard);
        utilityDialog.Update(time, IsActive || !string.IsNullOrEmpty(smokeOutput), keyboard, inspectorMouse);
        if (keyboard.IsKeyDown(Keys.Escape)) utilityAction = CloseUtilityDialog;
        var action = utilityAction; utilityAction = null; action?.Invoke();
        if (pendingPage is not null)
        {
            CloseUtilityDialog();
            var page = pendingPage; pendingPage = null; page();
        }
        if (utilityDialog is not null && exportDialog)
            utilityDialog.Focus.SetEnabled(createFileButton!.Path, selectedOutputFolder is not null);
        status.Label = message;
    }
}
