using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StationeryUI.Canvas;

internal sealed partial class EditorGame
{
    private SpriteBatch? modalSprites;
    private Texture2D? modalPixel;

    private void SuspendBackgroundInput()
    {
        // Release pointer capture and IME input before handing input to the modal host.
        ui.Update(new GameTime(), false, new(), new());
        sidebar?.Update(new GameTime(), false, new(), new());
        applicationBar.Update(new GameTime(), false, new(), new());
        inspector.Update(new GameTime(), false, new(), new());
    }

    private void DrawModalBackdrop()
    {
        var dialog = utilityDialog ?? restoreDialog ?? layoutDialog ?? pageDialog ?? externalConflictDialog;
        if (dialog is null) return;
        modalSprites ??= new SpriteBatch(GraphicsDevice);
        if (modalPixel is null)
        {
            modalPixel = new Texture2D(GraphicsDevice, 1, 1);
            modalPixel.SetData(new[] { Color.White });
        }
        ScreenRectangle logical = utilityDialog is not null ? new(180, 200, 1240, 380)
            : restoreDialog is not null ? new(184, 64, 1232, 688)
            : externalConflictDialog is not null ? new(300, 190, 1000, 430)
            : pageDialog is not null ? new(430, 150, 740, 600) : new(430, 210, 740, placementFields.Count > 0 ? 480 : 370);
        var bounds = dialog.Viewport.ToWindow(logical);
        var rectangle = new Rectangle((int)Math.Floor(bounds.X) - 2, (int)Math.Floor(bounds.Y) - 2,
            (int)Math.Ceiling(bounds.Width) + 4, (int)Math.Ceiling(bounds.Height) + 4);
        modalSprites.Begin(blendState: BlendState.AlphaBlend);
        modalSprites.Draw(modalPixel, GraphicsDevice.Viewport.Bounds, Color.Black * .55f);
        // Draw the border behind the opaque dialog surface.
        modalSprites.Draw(modalPixel, rectangle, StationeryUI.MonoGame.StationeryUiHost.Convert(theme.Border));
        modalSprites.End();
    }
}
