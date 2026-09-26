using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Windows;
using StationeryUI.Editor;

internal sealed partial class EditorGame
{
    private bool readMode;
    private string? readFile;
    private StationeryDeveloperView? readView;
    private StationeryUiHost? readActions;
    private StationeryUiHost.Element? readEditButton, readCloseButton;

    private void OpenReadStyle(string? path, bool discardChanges = false)
    {
        if (!discardChanges && !FlushAutoSave()) return;
        if (path is not null)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path)) throw new FileNotFoundException("文房具UIファイルが見つかりません。", path);
            // Parse before changing mode. Reading must not create a save session or backup.
            _ = StyleBlueprint.Open(path);
        }
        readView ??= new(GraphicsDevice, input, family => new WindowsTextRasterizer(family), StationeryDeveloperStyle.Load());
        readView.EmbeddedInEditor = true;
        readActions ??= CreateReadActions();
        if (path is not null && launch.LivePipe is null) RefreshFileInspection(path);
        // Commit the mode change only after the file and preview have passed validation.
        saveSession = null;
        saveError = null;
        invalidDraft = false;
        readFile = path;
        readMode = true;
        editingPage = false;
        Window.Title = "文房具UIエディター — 読取" + (path is null ? "（ライブ）" : " — " + Path.GetFileName(path));
    }

    private StationeryUiHost CreateReadActions()
    {
        var actions = new StationeryUiHost(GraphicsDevice, input, family => new WindowsTextRasterizer(family))
            { UseStationeryButtons = true };
        readEditButton = actions.AddButton("readEdit", new(), "編集を開始", () =>
        {
            if (readFile is null) return;
            Guard(() => OpenStyle(readFile));
        });
        readCloseButton = actions.AddButton("readClose", new(), "開始画面へ", () =>
        {
            readMode = false;
            BuildWelcome();
        });
        return actions;
    }

    private void RefreshFileInspection(string path)
    {
        var plan = StyleBlueprint.Open(path);
        var preview = plan.CreatePreview(Math.Max(1, GraphicsDevice.Viewport.Width), Math.Max(1, GraphicsDevice.Viewport.Height));
        var entries = new List<StationeryInspectionEntry>();
        void Visit(StationeryNode node)
        {
            preview.Layout.Bounds.TryGetValue(node.Path, out var bounds);
            entries.Add(new(node.Id, node.Path, node.Parent?.Path, node.Kind, node.Id, true, bounds));
            foreach (var child in node.Children) Visit(child);
        }
        Visit(preview.Settings.ModelTree.CreateTree());
        readView!.Refresh(DeveloperInspectionLayout.Apply(entries, preview.Settings, preview.Layout));
    }

    private void UpdateReadMode(GameTime time)
    {
        var keyboard = Keyboard.GetState(); var mouse = Mouse.GetState();
        var width = GraphicsDevice.Viewport.Width;
        var buttonWidth = Math.Min(160, Math.Max(0, width / 6));
        var actions = readActions!;
        actions.Theme = readView!.Theme;
        readEditButton!.Bounds = new(width - buttonWidth * 2, 4, buttonWidth, 40);
        readCloseButton!.Bounds = new(width - buttonWidth, 4, buttonWidth, 40);
        actions.Focus.SetEnabled(readEditButton.Path, readFile is not null);
        readView.OperationLog.HostIsActive = IsActive;
        readView.Update(time, IsActive, keyboard,
            mouse.Y < 48 && mouse.X >= width - buttonWidth * 2 ? new MouseState() : mouse,
            width, GraphicsDevice.Viewport.Height);
        actions.Update(time, IsActive, keyboard, mouse);
        if (pendingPage is { } action) { pendingPage = null; action(); }
        base.Update(time);
    }

    private void DrawReadMode(GameTime time)
    {
        GraphicsDevice.Clear(StationeryUiHost.Convert(readView!.Theme.Background));
        readView.Draw();
        readActions!.Draw();
        if (!string.IsNullOrEmpty(smokeOutput) && ++frames == 32)
        {
            Directory.CreateDirectory(smokeOutput);
            var width = GraphicsDevice.Viewport.Width; var height = GraphicsDevice.Viewport.Height;
            var pixels = new Color[width * height]; GraphicsDevice.GetBackBufferData(pixels);
            using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, width, height);
            texture.SetData(pixels);
            using var output = File.Create(Path.Combine(smokeOutput, "editor-read.png"));
            texture.SaveAsPng(output, width, height);
            File.WriteAllText(Path.Combine(smokeOutput, "read-report.json"),
                System.Text.Json.JsonSerializer.Serialize(new { File = readFile, ReadOnly = readMode, HasSaveSession = saveSession is not null,
                    HasInspection = readView.Model.Tree.Roots.Count > 0 }));
            Exit();
        }
        base.Draw(time);
    }
}
