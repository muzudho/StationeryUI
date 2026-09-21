using Microsoft.Xna.Framework.Input;

internal sealed partial class DesignerGame
{
    private bool SaveSmoke => !string.IsNullOrEmpty(smokeOutput)
        && Environment.GetEnvironmentVariable("STATIONERYUI_DESIGNER_TEST_SAVEPOINTS") == "1";

    private bool PrepareSaveSmoke(ref MouseState mouse)
    {
        if (!SaveSmoke) return false;
        mouse = new();
        if (frames == 4) SetText(tracks[0].Field, "2.5");
        if (frames == 120 && (saveSession!.IsDirty || !File.ReadAllText(saveSession.FilePath).Contains("2.5rate")))
            throw new InvalidOperationException("Timed autosave did not write the edited source.");
        if (frames is 124 or 125)
        {
            var point = ui.Viewport.ToWindow(new StationeryUI.Canvas.ScreenRectangle(870, 812, 1, 1));
            mouse = SmokeMouse((int)point.X, (int)point.Y, frames == 124);
        }
        return true;
    }

    private static MouseState SmokeMouse(int x, int y, bool pressed) => new(x, y, 0,
        pressed ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

    private void SavePickerScreenshot()
    {
        if (!SaveSmoke || frames != 130) return;
        if (restoreDialog is null) throw new InvalidOperationException("Savepoint picker was not opened.");
        var viewport = GraphicsDevice.Viewport;
        var pixels = new Microsoft.Xna.Framework.Color[viewport.Width * viewport.Height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, viewport.Width, viewport.Height);
        texture.SetData(pixels);
        using var file = File.Create(Path.Combine(smokeOutput!, "savepoints.png"));
        texture.SaveAsPng(file, viewport.Width, viewport.Height);
    }
}
