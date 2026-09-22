namespace StationeryUI.MonoGame;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;

/// <summary>Per-inspector JSON Lines operation history. Call on the game thread.</summary>
public sealed class DeveloperOperationLog : IDisposable
{
    private MouseState previousMouse;
    private string[] previousKeys = [];
    private DeveloperOperationState? previousState;
    private bool? previousHostIsActive;
    private string? previousInputSource;
    private long sequence;
    private bool disposed;
    public string FilePath { get; }
    public string? LastError { get; private set; }
    /// <summary>Optional actual host focus, separate from input enabled for synthetic tests.</summary>
    public bool? HostIsActive { get; set; }
    public string InputSource { get; set; } = "mouse-keyboard";

    public DeveloperOperationLog(string? directory = null)
    {
        directory ??= Environment.GetEnvironmentVariable("STATIONERYUI_OPERATION_LOG_DIR");
        if (string.IsNullOrWhiteSpace(directory))
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StationeryUI", "Logs");
        FilePath = Path.Combine(directory, $"inspector-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}-{Guid.NewGuid():N}.jsonl");
        Write("session-start", null);
    }

    public void Record(MouseState mouse, KeyboardState keyboard, DeveloperOperationState state)
    {
        if (disposed) return;
        // Record navigation/commands only, not arbitrary typed text or clipboard contents.
        var keys = keyboard.GetPressedKeys().Where(k => k is Keys.Tab or Keys.Enter or Keys.Space or Keys.Escape or Keys.F12
            or Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            or Keys.LeftControl or Keys.RightControl or Keys.LeftShift or Keys.RightShift
            || k == Keys.C && (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl)))
            .Select(k => k.ToString()).ToArray();
        var buttonsChanged = mouse.LeftButton != previousMouse.LeftButton || mouse.RightButton != previousMouse.RightButton
            || mouse.MiddleButton != previousMouse.MiddleButton || mouse.XButton1 != previousMouse.XButton1 || mouse.XButton2 != previousMouse.XButton2;
        var dragging = mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed || mouse.MiddleButton == ButtonState.Pressed;
        var wheel = previousState is null ? 0 : mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        var horizontalWheel = previousState is null ? 0 : mouse.HorizontalScrollWheelValue - previousMouse.HorizontalScrollWheelValue;
        if (previousState != state || previousHostIsActive != HostIsActive || previousInputSource != InputSource
            || buttonsChanged || wheel != 0 || horizontalWheel != 0 || !keys.SequenceEqual(previousKeys)
            || dragging && mouse.Position != previousMouse.Position)
            Write("update", new
            {
                HostIsActive, InputSource,
                Mouse = new { mouse.X, mouse.Y, Left = mouse.LeftButton.ToString(), Right = mouse.RightButton.ToString(),
                    Middle = mouse.MiddleButton.ToString(), X1 = mouse.XButton1.ToString(), X2 = mouse.XButton2.ToString(), WheelDelta = wheel, HorizontalWheelDelta = horizontalWheel },
                Keys = keys, Before = previousState, After = state
            });
        previousMouse = mouse; previousKeys = keys; previousState = state;
        previousHostIsActive = HostIsActive; previousInputSource = InputSource;
    }

    private void Write(string kind, object? data)
    {
        if (LastError is not null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length >= 8 * 1024 * 1024)
            {
                File.Move(FilePath, FilePath + ".1", true);
            }
            using var writer = new StreamWriter(new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
            writer.WriteLine(JsonSerializer.Serialize(new { TimestampUtc = DateTimeOffset.UtcNow, ProcessId = Environment.ProcessId,
                Sequence = ++sequence, Kind = kind, Data = data }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            LastError = ex.Message;
            Trace.WriteLine("StationeryUI operation log: " + ex.Message);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Write("session-end", null);
    }
}

/// <summary>Inspector state after processing a frame; paths identify controls, without copying document text.</summary>
public sealed record DeveloperOperationState(bool InputEnabled, string? HitPath, string? FocusedPath,
    string? SelectedPath, string? TargetPath, bool CaptureEnabled, double SplitRatio,
    double TreeScroll, double DetailsScroll, string CollapsedPaths, int Width, int Height)
{
    public double TreeHorizontalScroll { get; init; }
    public StationeryUI.Inspection.DeveloperTreeMode TreeMode { get; init; }
}
