using Microsoft.Xna.Framework.Input;

internal sealed partial class DesignerGame
{
    private bool disabledExportHintVerified;
    private bool exportSmokeFolderChosen;

    private void PrepareUtilitySmoke(ref MouseState mouse, ref KeyboardState keyboard)
    {
        if (string.IsNullOrEmpty(smokeOutput)) return;
        keyboard = new();
        if (!exportDialog)
        {
            mouse = SmokeMouse(utilityDialog!, 240, 390, frames == 13);
            return;
        }
        if (frames == 21 && !exportSmokeFolderChosen)
        {
            exportSmokeFolderChosen = true;
            if (utilityDialog!.Focus.IsEnabled(createFileButton!.Path)) throw new InvalidOperationException("Create must start disabled.");
            if (layoutSmoke is null)
            {
                selectedOutputFolder = smokeOutput;
                File.WriteAllText(Path.Combine(smokeOutput, "my-plan.stationery-ui.json"), "existing file");
            }
        }
        if (layoutSmoke is not null)
        {
            mouse = SmokeMouse(utilityDialog!, 800, 390, false);
            if (frames == 23)
            {
                disabledExportHintVerified = toolHint.Label.Contains("先にフォルダーを選択してください");
                keyboard = new(Keys.Escape);
            }
            return;
        }
        mouse = frames < 24 ? SmokeMouse(utilityDialog!, 800, 390, frames == 22)
            : SmokeMouse(utilityDialog!, 240, 534, frames == 24);
        if (frames is 20 or 21)
        {
            // Click the background's Back button and send keyboard input while modal.
            mouse = SmokeMouse(applicationBar, 200, 20, frames == 20);
            keyboard = frames == 20 ? new(Keys.Tab) : new(Keys.A);
        }
        if (frames == 22 && (!editingPage || !exportDialog || pendingPage is not null))
            throw new InvalidOperationException("Background controls received modal input.");
        if (frames == 27) keyboard = new(Keys.Escape);
    }

    private void CaptureUtilitySmoke()
    {
        if (string.IsNullOrEmpty(smokeOutput) || utilityDialog is null || frames != 26 || !exportDialog) return;
        var viewport = GraphicsDevice.Viewport;
        var pixels = new Microsoft.Xna.Framework.Color[viewport.Width * viewport.Height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, viewport.Width, viewport.Height);
        texture.SetData(pixels);
        using var file = File.Create(Path.Combine(smokeOutput, "export-dialog.png"));
        texture.SaveAsPng(file, viewport.Width, viewport.Height);
    }
}
