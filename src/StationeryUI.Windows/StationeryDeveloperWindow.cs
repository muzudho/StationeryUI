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
    private long showSequence, captureSequence;
    private string? capturePath;
    private string? editorExecutable, styleFilePath, styleError;
    private string requestedMode = "read";
    public bool CaptureEnabled { get { lock (gate) return visible && viewState?.CaptureEnabled == true; } }
    public string? SelectedPath { get { lock (gate) return capturePath ?? viewState?.SelectedPath; } }
    public void SelectCaptured(string path)
    {
        lock (gate) { capturePath = path; captureSequence++; }
    }
    public bool IsOpen { get { lock (gate) return visible || requested; } }
    public string? LastError { get; private set; }

    /// <summary>Use the external UI editor as this host's inspector while retaining the legacy fallback.</summary>
    public void UseEditor(string executable, string? filePath)
    {
        lock (gate)
        {
            if (worker is { IsCompleted: false }) throw new InvalidOperationException("Configure the editor before opening the inspector.");
            editorExecutable = Path.GetFullPath(executable);
            styleFilePath = filePath is null ? null : Path.GetFullPath(filePath);
        }
    }

    public void Show(IReadOnlyList<StationeryInspectionEntry> entries)
        => Show(entries, edit: false);

    public void Show(IReadOnlyList<StationeryInspectionEntry> entries, bool edit)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            snapshot = entries.ToArray();
            requestedMode = edit ? "edit" : "read";
            requested = true; showSequence++;
            if (worker is null || worker.IsCompleted) worker = Task.Run(RunAsync);
        }
    }
    public void Update(IReadOnlyList<StationeryInspectionEntry> entries, string? filePath = null, string? error = null)
    {
        lock (gate)
        {
            if (disposed) return;
            snapshot = entries.ToArray();
            if (filePath is not null) styleFilePath = Path.GetFullPath(filePath);
            styleError = error;
        }
    }

    private async Task RunAsync()
    {
        var pipeName = "StationeryUI.Inspector." + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        try
        {
            var executable = editorExecutable ?? Environment.ProcessPath ?? throw new InvalidOperationException("No inspector host executable.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Normal };
            if (editorExecutable is null && Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
            if (editorExecutable is null)
            { start.ArgumentList.Add("--stationery-inspector"); start.ArgumentList.Add(pipeName); }
            else
            {
                start.ArgumentList.Add("--live"); start.ArgumentList.Add(pipeName);
                start.ArgumentList.Add("--mode"); start.ArgumentList.Add(requestedMode == "edit" && styleFilePath is not null ? "edit" : "read");
                if (styleFilePath is not null) { start.ArgumentList.Add("--file"); start.ArgumentList.Add(styleFilePath); }
            }
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
                lock (gate) message = new(snapshot, showSequence, viewState, capturePath, captureSequence)
                { EditorMode = requestedMode, StyleFilePath = styleFilePath, StyleError = styleError };
                await writer.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), stopping.Token);
                // The editor may ask whether to save, discard, or cancel an in-progress draft.
                var response = await reader.ReadLineAsync(stopping.Token).AsTask().WaitAsync(TimeSpan.FromMinutes(5), stopping.Token);
                if (response is null) break;
                var state = JsonSerializer.Deserialize<DeveloperViewState>(response);
                lock (gate)
                {
                    if (state is not null) { viewState = state; visible = state.Visible; if (captureSequence == message.CaptureSequence) capturePath = null; }
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
                    if (editorExecutable is null && !process.HasExited) process.Kill();
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
            if (editorExecutable is null && process is { HasExited: false }) process.Kill();
        }
    }
}
