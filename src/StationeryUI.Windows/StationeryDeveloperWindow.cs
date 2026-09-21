namespace StationeryUI.Windows;

using StationeryUI.Inspection;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;

/// <summary>Runs a StationeryUI inspector host in a separate process. The host handles --stationery-inspector PIPE.</summary>
public sealed class StationeryDeveloperWindow : IDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource stopping = new();
    private StationeryInspectionEntry[] snapshot = [];
    private DeveloperViewState? viewState;
    private Task? worker;
    private Process? process;
    private bool visible, requested, disposed;
    private long showSequence;
    public bool IsOpen { get { lock (gate) return visible || requested; } }
    public string? LastError { get; private set; }

    public void Show(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            snapshot = entries.ToArray();
            requested = true; showSequence++;
            if (worker is null || worker.IsCompleted) worker = Task.Run(RunAsync);
        }
    }
    public void Update(IReadOnlyList<StationeryInspectionEntry> entries)
    {
        lock (gate) { if (!disposed) snapshot = entries.ToArray(); }
    }

    private async Task RunAsync()
    {
        var pipeName = "StationeryUI.Inspector." + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("No inspector host executable.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Normal };
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
            start.ArgumentList.Add("--stationery-inspector"); start.ArgumentList.Add(pipeName);
            // A helper must not inherit the demo's screenshot/automatic-input switches.
            foreach (var key in start.Environment.Keys.Where(key => key.StartsWith("STATIONERYUI_SMOKE_", StringComparison.Ordinal)).ToArray())
                start.Environment.Remove(key);
            lock (gate)
            {
                if (disposed) return;
                process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start inspector.");
            }
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true) { AutoFlush = true };
            LastError = null;
            while (!stopping.IsCancellationRequested)
            {
                DeveloperInspectionMessage message;
                lock (gate) message = new(snapshot, showSequence, viewState);
                await writer.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), stopping.Token);
                var response = await reader.ReadLineAsync(stopping.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(10), stopping.Token);
                if (response is null) break;
                var state = JsonSerializer.Deserialize<DeveloperViewState>(response);
                lock (gate)
                {
                    if (state is not null) { viewState = state; visible = state.Visible; }
                    if (showSequence == message.ShowSequence) requested = false;
                }
                await Task.Delay(150, stopping.Token);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException or JsonException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            LastError = ex.Message;
            Trace.WriteLine("StationeryUI inspector: " + ex.Message);
        }
        finally
        {
            lock (gate)
            {
                if (process is not null)
                {
                    if (!process.HasExited) process.Kill();
                    process.Dispose(); process = null;
                }
                visible = requested = false;
            }
        }
    }

    public void Dispose()
    {
        Task? current;
        lock (gate)
        {
            if (disposed) return;
            disposed = true; visible = requested = false;
            stopping.Cancel(); current = worker;
        }
        try { current?.Wait(2000); } catch (AggregateException) { }
        lock (gate)
        {
            if (process is { HasExited: false }) process.Kill();
        }
    }
}
