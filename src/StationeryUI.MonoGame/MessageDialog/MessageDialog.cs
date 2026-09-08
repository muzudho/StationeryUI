namespace StationeryUI.MonoGame.MessageDialog;

using Microsoft.Xna.Framework;
using System;
using StationeryUI.MonoGame;

/// <summary>アプリ内で表示する、本文と CLOSE ボタンだけを持つメッセージダイアログ。</summary>
public sealed class MessageDialog
{
    public static readonly Rectangle Bounds = new(510, 336, 900, 408);
    public static readonly Rectangle CloseButtonBounds = new(1218, 366, 154, 48);

    public MessageDialog(string title, string message, string closeButtonLabel = "CLOSE")
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        CloseButtonLabel = closeButtonLabel ?? throw new ArgumentNullException(nameof(closeButtonLabel));
    }

    public string Title { get; }
    public string Message { get; }
    public string CloseButtonLabel { get; }
    public bool IsCloseHit(Point point) => CloseButtonBounds.Contains(point);

    public void Draw(StationeryDrawingTools drawingContext, Point mousePosition)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        Draw(mousePoint, new MessageDialogDrawingCallbacks(
            drawingContext.FillRectangle, drawingContext.DrawRectangle,
            drawingContext.DrawDynamicText, drawingContext.DrawLine,
            (bounds, text, focused, point, scale) =>
                drawingContext.DrawButton(bounds, text, focused, point, true, scale)));
        drawingContext.End();
    }

    public void Draw(Point mousePoint, MessageDialogDrawingCallbacks draw)
    {
        draw.FillRectangle(new Rectangle(0, 0, 1920, 1080), new Color(0, 0, 0, 160));
        draw.FillRectangle(new Rectangle(Bounds.X + 14, Bounds.Y + 16, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 120));
        draw.FillRectangle(Bounds, new Color(21, 25, 32, 250));
        draw.DrawRectangle(Bounds, 2, new Color(99, 223, 185));
        draw.DrawText(Title, new Rectangle(Bounds.X + 42, Bounds.Y + 38, 620, 48), new Color(244, 238, 218), 0.64f);
        draw.DrawLine(new Vector2(Bounds.X + 42, Bounds.Y + 106), new Vector2(Bounds.Right - 42, Bounds.Y + 106), 1, new Color(82, 111, 114));
        draw.DrawText(Message, new Rectangle(Bounds.X + 56, Bounds.Y + 164, Bounds.Width - 112, 150), Color.White, 0.42f);
        draw.DrawButton(CloseButtonBounds, CloseButtonLabel, false, mousePoint, 0.32f);
    }
}

public sealed record MessageDialogDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Rectangle, Color, float> DrawText,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<Rectangle, string, bool, Point, float> DrawButton);
