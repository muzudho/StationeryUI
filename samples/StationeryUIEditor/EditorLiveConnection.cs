namespace StationeryUI.Editor;

using StationeryUI.Inspection;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

/// <summary>Receives live inspection snapshots without touching the document being edited.</summary>
internal sealed class EditorLiveConnection : IDisposable
{
    internal sealed record Packet(DeveloperInspectionMessage Message, TaskCompletionSource<DeveloperViewState> Response);
    private readonly string pipeName;
    private readonly CancellationTokenSource stopping = new();
    private readonly object gate = new();
    private readonly Task worker;
    private Packet? pending;
    public bool Disconnected { get; private set; }
    public string? LastError { get; private set; }

    public EditorLiveConnection(string pipeName)
    {
        this.pipeName = pipeName;
        worker = Task.Run(ConnectAsync);
    }

    public Packet? Take()
    {
        lock (gate) { var packet = pending; pending = null; return packet; }
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
                var message = JsonSerializer.Deserialize<DeveloperInspectionMessage>(line)
                    ?? throw new JsonException("Missing inspection snapshot.");
                if (message.ProtocolVersion != 1) throw new JsonException("Unsupported inspector protocol version.");
                var response = new TaskCompletionSource<DeveloperViewState>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (gate) pending = new(message, response);
                await response.Task.WaitAsync(stopping.Token);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response.Task.Result).AsMemory(), stopping.Token);
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or TimeoutException or JsonException)
        { LastError = ex.Message; }
        finally { Disconnected = true; }
    }

    public void Dispose()
    {
        stopping.Cancel();
        try { worker.Wait(1000); } catch (AggregateException) { }
        stopping.Dispose();
    }
}
