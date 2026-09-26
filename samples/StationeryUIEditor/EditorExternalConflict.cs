using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using StationeryUI.Editor;
using StationeryUI.Windows;
using System.Text.Json;

internal sealed partial class EditorGame
{
    private StationeryUiHost? externalConflictDialog;
    private StationeryUiHost.Element? externalConflictMessage;
    private Action? externalConflictAction;

    private void OpenExternalConflictDialog()
    {
        if (!externalConflict || saveSession is null) return;
        SuspendBackgroundInput();
        externalConflictDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true, ToolHintProvider = EditorToolHint };
        externalConflictDialog.Viewport.Scale = BodyScale;
        externalConflictDialog.AddTextBlock(externalConflictDialog.Root.AddChild("conflictBackground", "textBlock"),
            new(300, 190, 1000, 430), "");
        externalConflictDialog.AddTextBlock(externalConflictDialog.Root.AddChild("conflictHeading", "textBlock"),
            new(324, 214, 952, 52), "元ファイルが外部で変更されました");
        externalConflictMessage = externalConflictDialog.AddTextBlock(
            externalConflictDialog.Root.AddChild("conflictMessage", "textBlock"), new(324, 278, 952, 200),
            "現在の編集内容はメモリー上に保持しています。元ファイルは上書きしません。\n"
            + "別名で下書きを保存するか、編集内容を破棄して外部版を読み直してください。");
        externalConflictDialog.AddButton("conflictExport", new(324, 530, 304, 60), "下書きを別名で保存",
            () => externalConflictAction = ExportConflictedDraft);
        externalConflictDialog.AddButton("conflictReload", new(648, 530, 304, 60), "破棄して外部版を開く",
            () => externalConflictAction = ReloadExternalStyle);
        externalConflictDialog.AddButton("conflictClose", new(972, 530, 304, 60), "編集を続ける",
            () => externalConflictAction = CloseExternalConflictDialog);
    }

    private void UpdateExternalConflictDialog(GameTime time)
    {
        externalConflictDialog!.Theme = theme;
        externalConflictDialog.Viewport.Scale = BodyScale;
        inspectorMouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        externalConflictDialog.Update(time, IsActive, keyboard, inspectorMouse);
        if (keyboard.IsKeyDown(Keys.Escape)) externalConflictAction = CloseExternalConflictDialog;
        var action = externalConflictAction; externalConflictAction = null;
        action?.Invoke();
        status.Label = SaveState;
    }

    private void ExportConflictedDraft()
    {
        var path = saveSession?.FilePath;
        CloseExternalConflictDialog();
        if (path is null) return;
        outputPath = SuggestedRecoveryPath(path);
        Guard(OpenExportDialog);
    }

    private static string SuggestedRecoveryPath(string path)
    {
        var name = Path.GetFileName(path);
        var stem = name.EndsWith(".stationery-ui.json", StringComparison.OrdinalIgnoreCase)
            ? name[..^".stationery-ui.json".Length] : Path.GetFileNameWithoutExtension(name);
        var directory = Path.GetDirectoryName(path)!;
        var candidate = Path.Combine(directory, stem + ".recovered.stationery-ui.json");
        for (var number = 2; File.Exists(candidate) || Directory.Exists(candidate); number++)
            candidate = Path.Combine(directory, stem + $".recovered-{number}.stationery-ui.json");
        return candidate;
    }

    private void ReloadExternalStyle()
    {
        if (saveSession is null) return;
        try
        {
            var opened = StyleSaveSession.Open(saveSession.FilePath);
            saveSession = opened.Session; blueprint = opened.Blueprint;
            sourceFile = outputPath = saveSession.FilePath;
            saveError = null; invalidDraft = false; externalConflict = false; externalCheckElapsed = 0;
            treeJson = lastValidTreeJson = null; lastTreeSelection = null;
            selectedRow = selectedColumn = 0; rebuild = false;
            revealLayout = null;
            BuildUi();
            message = "外部版を読み直しました。以前の下書きは破棄しました。";
            CloseExternalConflictDialog();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            externalConflictMessage!.Label = "外部版を開けません：" + ex.Message
                + "\nファイルを修正するか、下書きを別名で保存してください。";
        }
    }

    private void CloseExternalConflictDialog()
    {
        externalConflictDialog?.Dispose(); externalConflictDialog = null;
        externalConflictMessage = null; externalConflictAction = null;
    }
}
