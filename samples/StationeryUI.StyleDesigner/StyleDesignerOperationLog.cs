namespace StationeryUI.StyleDesigner;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

/// <summary>Append-only, privacy-conscious operation history for the layout designer.</summary>
internal sealed class StyleDesignerOperationLog : IDisposable
{
    private long sequence;
    private bool disposed;

    public string FilePath { get; }
    public string? LastError { get; private set; }

    public StyleDesignerOperationLog()
    {
        var directory = Environment.GetEnvironmentVariable("STATIONERYUI_OPERATION_LOG_DIR");
        if (string.IsNullOrWhiteSpace(directory))
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "StationeryUI", "Logs");
        FilePath = Path.Combine(directory,
            $"style-designer-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}-{Guid.NewGuid():N}.jsonl");
        Write("session-start", new { Application = "StationeryUI.StyleDesigner" });
    }

    public void Record(string kind, object? data)
    {
        if (!disposed) Write(kind, data);
    }

    private void Write(string kind, object? data)
    {
        if (LastError is not null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            using var writer = new StreamWriter(new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                new UTF8Encoding(false));
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                ProcessId = Environment.ProcessId,
                Sequence = ++sequence,
                Kind = kind,
                Data = data
            }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
            or NotSupportedException or System.Security.SecurityException)
        {
            LastError = ex.Message;
            Trace.WriteLine("StationeryUI Style Designer operation log: " + ex.Message);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        Write("session-end", null);
        disposed = true;
    }
}
