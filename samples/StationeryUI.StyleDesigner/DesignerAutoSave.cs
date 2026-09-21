using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Controls;
using StationeryUI.MonoGame;
using StationeryUI.StyleDesigner;
using StationeryUI.Windows;
using System.Text.Json;

internal sealed partial class DesignerGame
{
    private StyleSaveSession? saveSession;
    private string? saveError;
    private bool invalidDraft;
    private StationeryUiHost? restoreDialog;
    private string? restoreChoice;
    private string SaveState => saveError is not null ? "保存失敗：" + saveError
        : saveSession is null ? "未保存：新規作成かエクスポートで保存先を決めてください"
        : invalidDraft ? "保存待機：入力を確認してください"
        : saveSession.IsDirty ? "オートセーブ待ち…" : "保存済み：" + Path.GetFileName(saveSession.FilePath);

    private void OpenStyle(string path)
    {
        if (!FlushAutoSave()) return;
        var opened = StyleSaveSession.Open(path);
        pendingPage = () =>
        {
            saveSession = opened.Session; blueprint = opened.Blueprint;
            saveError = null; invalidDraft = false;
            sourceFile = outputPath = saveSession.FilePath;
            selectedRow = selectedColumn = 0;
            BeginEditing("読み込み時のセーブポイントを作成しました。変更は元ファイルへ自動保存します。");
        };
    }

    private void AutoSave(string json, GameTime time)
    {
        invalidDraft = false;
        if (saveSession is null) return;
        saveSession.Observe(json);
        if (ui.IsComposing) { saveSession.RestartTimer(); return; }
        try { if (saveSession.Tick(time.ElapsedGameTime.TotalSeconds)) saveError = null; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { saveError = ex.Message; }
    }

    private bool FlushAutoSave()
    {
        if (saveSession is null) return true;
        try
        {
            ValidateGridCounts();
            if (ui.IsComposing) throw new IOException("日本語入力を確定してから操作してください。");
            Capture(); saveSession.Observe(blueprint.BuildJson()); saveSession.Flush();
            saveError = null; invalidDraft = false;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        { saveError = ex.Message; message = "保存できないため操作を中止しました：" + ex.Message; return false; }
    }

    private void ExportAndTrack(string path)
    {
        ValidateGridCounts();
        Capture();
        if (saveSession is not null && string.Equals(Path.GetFullPath(path), saveSession.FilePath, StringComparison.OrdinalIgnoreCase))
        { FlushAutoSave(); return; }
        blueprint.Export(path);
        // A new output becomes the active autosave destination; keep the editable draft.
        saveSession = StyleSaveSession.Open(path).Session;
        saveError = null; invalidDraft = false;
        sourceFile = outputPath = saveSession.FilePath;
        if (utilityDialog is not null && exportDialog) SetText(output, outputPath);
        rebuild = true;
        message = "保存しました。以後はこのファイルへ自動保存します：" + outputPath;
    }

    private void OpenRestoreDialog()
    {
        if (saveSession is null) return;
        Capture();
        // Invalid input may still be discarded by restoring a valid savepoint.
        try { saveSession.Observe(blueprint.BuildJson()); } catch (JsonException) { }
        var points = saveSession.ListSavePoints();
        SuspendBackgroundInput();
        restoreChoice = null;
        restoreDialog = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { Theme = theme, UseStationeryButtons = true, ToolHintProvider = DesignerToolHint };
        restoreDialog.AddTextBlock(restoreDialog.Root.AddChild("restoreBackground", "textBlock"), new(184, 64, 1232, 688), "");
        restoreDialog.AddTextBlock(restoreDialog.Root.AddChild("restoreHeading", "textBlock"), new(200, 80, 1200, 80),
            "セーブポイントに戻す — ファイル名 ／ ファイル変更日時（ローカル時刻）");
        var tree = new TreeView();
        var paths = new Dictionary<string, string>();
        foreach (var point in points)
        {
            var id = "backup" + point.Generation;
            tree.AddNode(id, $"{Path.GetFileName(point.Path)}  ／  {point.Modified:yyyy-MM-dd HH:mm:ss}");
            paths[id] = point.Path;
        }
        var list = restoreDialog.AddTree(restoreDialog.Root.AddChild("savePoints", "tree"), new(200, 168, 1200, 480), "セーブポイント", tree);
        list.ToolHint = "世代を選んで「このセーブポイントに戻す」を押します。直前の有効な内容もバックアップします。";
        restoreDialog.AddButton("restoreConfirm", new(200, 670, 720, 60), "このセーブポイントに戻す", () =>
        {
            if (tree.TargetItem is { } target) restoreChoice = paths[target.Id];
        });
        restoreDialog.AddButton("cancel", new(940, 670, 460, 60), "キャンセル", () => restoreChoice = "cancel");
        restoreDialog.Viewport.Scale = BodyScale;
    }

    private void UpdateRestoreDialog(GameTime time)
    {
        restoreDialog!.Theme = theme; restoreDialog.Viewport.Scale = BodyScale;
        inspectorMouse = Mouse.GetState();
        if (SaveSmoke)
        {
            var point = restoreDialog.Viewport.ToWindow(frames < 132
                ? new StationeryUI.Canvas.ScreenRectangle(260, 182, 1, 1)
                : new StationeryUI.Canvas.ScreenRectangle(260, 690, 1, 1));
            inspectorMouse = SmokeMouse((int)point.X, (int)point.Y, frames is 128 or 132);
        }
        restoreDialog.Update(time, IsActive || SaveSmoke, SaveSmoke ? new() : Keyboard.GetState(), inspectorMouse);
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) restoreChoice = "cancel";
        if (restoreChoice is null) return;
        var choice = restoreChoice; restoreChoice = null;
        if (choice != "cancel")
        {
            try
            {
                blueprint = saveSession!.Restore(choice);
                revealLayout = blueprint.SelectedLayoutId;
                saveError = null; invalidDraft = false; selectedRow = selectedColumn = 0;
                outputPath = saveSession.FilePath;
                treeJson = lastValidTreeJson = null; lastTreeSelection = null;
                message = "セーブポイントを復元して元ファイルへ保存しました。";
                BuildUi();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            { saveError = ex.Message; status.Label = SaveState; return; }
        }
        restoreDialog.Dispose(); restoreDialog = null;
    }
}
