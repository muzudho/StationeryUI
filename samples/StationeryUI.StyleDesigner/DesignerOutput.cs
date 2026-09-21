using StationeryUI.Windows;
using StationeryUI.StyleDesigner;

internal sealed partial class DesignerGame
{
    private string? selectedOutputFolder;

    private void BuildOutputControls(double y)
    {
        Text("outputTitle", new(12, y, 120, 38), "出力先：");
        output = ui.AddTextBox("output", new(136, y, 1132, 38), "新しい JSON の出力パス", outputPath, 4096);
        ui.AddButton("export", new(12, y + 44, 210, 38), "エクスポート", () => Guard(() =>
        {
            ExportAndTrack(outputPath);
        }));
        ui.AddButton("chooseFolder", new(234, y + 44, 210, 38), "フォルダーを選ぶ", () => Guard(() =>
        {
            Capture();
            var folder = WindowsStyleFileDialog.ChooseFolder(selectedOutputFolder);
            if (folder is null) return;
            selectedOutputFolder = folder;
            outputPath = Path.Combine(folder, "my-plan.stationery-style.json");
            SetText(output, outputPath);
            message = "出力フォルダーを選択しました。新規作成で現在の設計を新しい JSON に出力できます。";
            rebuild = true;
        }));
        ui.AddButton("chooseFile", new(456, y + 44, 210, 38), "ファイルを選ぶ", () => Guard(() =>
        {
            var path = ChooseStyleFile();
            if (path is null) return;
            OpenStyle(path);
        }));
        var create = ui.AddButton("createFile", new(678, y + 44, 174, 38), "新規作成", () => Guard(() =>
        {
            if (selectedOutputFolder is null) return;
            Capture();
            var path = Path.Combine(selectedOutputFolder, "my-plan.stationery-style.json");
            for (var suffix = 2; File.Exists(path) || Directory.Exists(path); suffix++)
                path = Path.Combine(selectedOutputFolder, $"my-plan-{suffix}.stationery-style.json");
            ExportAndTrack(path);
        }));
        ui.Focus.SetEnabled(create.Path, selectedOutputFolder is not null);
    }
}
