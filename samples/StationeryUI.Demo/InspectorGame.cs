using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Windows;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

/// <summary>One MonoGame/SDL event loop per process, connected to the inspected game by a local pipe.</summary>
internal sealed class InspectorGame : Game
{
    private readonly GraphicsDeviceManager manager;
    private readonly StationeryDeveloperStyle style = StationeryDeveloperStyle.Load();
    private readonly string pipeName;
    private readonly CancellationTokenSource stopping = new();
    private readonly object gate = new();
    private Pending? pending;
    private volatile bool disconnected;
    private Task? connection;
    private WindowsTextInputService? input;
    private StationeryDeveloperView? view;
    private long showSequence = -1;
    private long captureSequence;
    private bool shown = true;
    private bool waitForCloseKeyRelease = true;
    private KeyboardState previous;
    private double reportElapsed;
    private readonly string? testOutput = Environment.GetEnvironmentVariable("STATIONERYUI_INSPECTOR_TEST_OUTPUT");
    private TestInput? testInput;
    private sealed record TestInput(int Sequence, int X, int Y, bool Down, int[] Keys);
    private sealed record Pending(DeveloperInspectionMessage Message, TaskCompletionSource<DeveloperViewState> Response);
    public InspectorGame(string pipeName)
    {
        this.pipeName = pipeName;
        manager = new(this) { PreferredBackBufferWidth = style.Width, PreferredBackBufferHeight = style.Height };
        Window.Title = "StationeryUI Inspector";
        Window.AllowUserResizing = true; IsMouseVisible = true;
    }
    protected override void LoadContent()
    {
        Window.Title = "F12 開発者ウィンドウ — 文房具UI";
        input = new(Window.Handle);
        view = new(GraphicsDevice, input, family => new WindowsTextRasterizer(family), style);
        connection = Task.Run(ConnectAsync);
    }
    private async Task ConnectAsync()
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(10000, stopping.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            while (!stopping.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(stopping.Token);
                if (line is null) break;
                var message = JsonSerializer.Deserialize<DeveloperInspectionMessage>(line) ?? throw new JsonException("Missing snapshot.");
                var response = new TaskCompletionSource<DeveloperViewState>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (gate) pending = new(message, response);
                var state = await response.Task.WaitAsync(stopping.Token);
                await writer.WriteLineAsync(JsonSerializer.Serialize(state).AsMemory(), stopping.Token);
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or TimeoutException or JsonException)
        { System.Diagnostics.Trace.WriteLine("Inspector connection: " + ex.Message); }
        finally { disconnected = true; }
    }
    protected override void Update(GameTime gameTime)
    {
        if (disconnected) { Exit(); return; }
        Pending? packet;
        lock (gate) { packet = pending; pending = null; }
        if (packet is not null)
        {
            view!.Refresh(packet.Message.Entries);
            if (showSequence < 0) view.Restore(packet.Message.RestoreState);
            if (packet.Message.CaptureSequence != captureSequence)
            {
                if (packet.Message.CapturePath is { } path) view.SelectCaptured(path);
                captureSequence = packet.Message.CaptureSequence;
            }
            if (packet.Message.ShowSequence != showSequence)
            {
                shown = true;
                waitForCloseKeyRelease = true;
                SDL_ShowWindow(Window.Handle); SDL_RaiseWindow(Window.Handle);
                showSequence = packet.Message.ShowSequence;
            }
        }
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        if (!string.IsNullOrEmpty(testOutput) && File.Exists(Path.Combine(testOutput, "input.json")))
        {
            try { testInput = JsonSerializer.Deserialize<TestInput>(File.ReadAllText(Path.Combine(testOutput, "input.json"))); }
            catch (Exception ex) when (ex is IOException or JsonException) { }
            if (testInput is not null)
            {
                keyboard = new(testInput.Keys.Select(key => (Keys)key).ToArray());
                mouse = new(testInput.X, testInput.Y, 0, testInput.Down ? ButtonState.Pressed : ButtonState.Released,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            }
        }
        if (keyboard.IsKeyUp(Keys.F12) && keyboard.IsKeyUp(Keys.Escape)) waitForCloseKeyRelease = false;
        if (shown && (IsActive || testInput is not null) && !waitForCloseKeyRelease && ((keyboard.IsKeyDown(Keys.F12) && previous.IsKeyUp(Keys.F12)) ||
            (keyboard.IsKeyDown(Keys.Escape) && previous.IsKeyUp(Keys.Escape))))
        {
            shown = false; SDL_HideWindow(Window.Handle);
        }
        previous = keyboard;
        view!.Update(gameTime, shown && (IsActive || testInput is not null), keyboard, mouse, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        packet?.Response.TrySetResult(view.Capture(shown));
        reportElapsed += gameTime.ElapsedGameTime.TotalSeconds;
        if (!string.IsNullOrEmpty(testOutput) && reportElapsed >= .2)
        {
            Directory.CreateDirectory(testOutput);
            var report = new { ProcessId = Environment.ProcessId, IsActive, InputSequence = testInput?.Sequence ?? 0, view.FocusedPath,
                CopiedPath = view.LastCopiedPath is null ? null : input!.ReadClipboard(), State = view.Capture(shown), view.Model.Details,
                view.TreeBounds, view.SplitBounds, view.CopyBounds, view.CaptureBounds, view.DetailsBounds, view.DetailsScroll,
                view.InspectorPanelBounds, view.ToolHintBounds, view.ToolHintText,
                Rows = view.Model.Tree.VisibleRows().Select(row => new { Path = view.Model.PathFor(row.Item), row.Depth, row.Item.IsExpanded }) };
            File.WriteAllText(Path.Combine(testOutput, "report.tmp"), JsonSerializer.Serialize(report));
            File.Move(Path.Combine(testOutput, "report.tmp"), Path.Combine(testOutput, "report.json"), true);
            reportElapsed = 0;
        }
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        if (shown)
        {
            GraphicsDevice.Clear(StationeryUiHost.Convert(view!.Theme.Background));
            view.Draw();
            if (!string.IsNullOrEmpty(testOutput) && File.Exists(Path.Combine(testOutput, "capture.request")))
            {
                var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
                var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
                var pixels = new Color[width * height]; GraphicsDevice.GetBackBufferData(pixels);
                using var texture = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, width, height);
                texture.SetData(pixels);
                using var output = File.Create(Path.Combine(testOutput, "inspector.png"));
                texture.SaveAsPng(output, width, height);
                File.Delete(Path.Combine(testOutput, "capture.request"));
            }
        }
        base.Draw(gameTime);
    }
    protected override void UnloadContent()
    {
        stopping.Cancel();
        view?.Dispose(); input?.Dispose();
        base.UnloadContent();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { stopping.Cancel(); try { connection?.Wait(1000); } catch (AggregateException) { } }
        base.Dispose(disposing);
    }
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_ShowWindow(nint window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_HideWindow(nint window);
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)] private static extern void SDL_RaiseWindow(nint window);
}
