using System.Text;

namespace StationeryUI.Editor;

/// <summary>One source file, debounced writes, and numbered savepoints.</summary>
internal sealed class StyleSaveSession
{
    public const double DelaySeconds = 1.5;
    public const int GenerationLimit = 20;
    public string FilePath { get; }
    private byte[] expected;
    private string saved, pending;
    private double elapsed;
    public bool IsDirty => pending != saved;
    public void RestartTimer() => elapsed = 0;
    public double Progress => IsDirty ? Math.Clamp(elapsed / DelaySeconds, 0, 1) : 1;
    public sealed record SavePoint(long Generation, string Path, DateTime Modified);

    private StyleSaveSession(string path, byte[] bytes, string json)
    { FilePath = path; expected = bytes; saved = pending = json; }

    public static (StyleSaveSession Session, StyleBlueprint Blueprint) Open(string path)
    {
        path = Path.GetFullPath(path);
        var bytes = File.ReadAllBytes(path);
        var blueprint = Parse(bytes);
        var session = new StyleSaveSession(path, bytes, blueprint.BuildJson());
        session.Backup(bytes, File.GetLastWriteTimeUtc(path));
        return (session, blueprint);
    }

    private static StyleBlueprint Parse(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
        return StyleBlueprint.Parse(reader.ReadToEnd());
    }

    public void Observe(string json)
    {
        if (pending == json) return;
        pending = json; elapsed = 0;
    }

    public bool Tick(double seconds)
    {
        if (!IsDirty) return false;
        elapsed += seconds;
        if (elapsed < DelaySeconds) return false;
        elapsed = 0; // Failed writes are retried at the same interval.
        Flush();
        return true;
    }

    public void Flush()
    {
        if (!IsDirty) return;
        StyleBlueprint.Parse(pending);
        Write(Encoding.UTF8.GetBytes(pending));
        saved = pending;
    }

    public IReadOnlyList<SavePoint> ListSavePoints()
    {
        var prefix = Path.GetFileName(FilePath) + ".";
        return Directory.EnumerateFiles(Path.GetDirectoryName(FilePath)!, "*.bak")
            .Select(path => (Path: path, Name: Path.GetFileName(path)))
            .Where(x => x.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => new SavePoint(long.TryParse(x.Name[prefix.Length..^4], out var n) ? n : 0, x.Path, File.GetLastWriteTime(x.Path)))
            .Where(x => x.Generation > 0 && string.Equals(Path.GetFileName(x.Path), prefix + x.Generation + ".bak", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Generation).ToArray();
    }

    private void Backup(byte[] bytes, DateTime modifiedUtc)
    {
        var generation = checked((ListSavePoints().FirstOrDefault()?.Generation ?? 0) + 1);
        var path = FilePath + "." + generation + ".bak";
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(true); }
        File.SetLastWriteTimeUtc(path, modifiedUtc);
        foreach (var old in ListSavePoints().Skip(GenerationLimit)) File.Delete(old.Path);
    }

    public StyleBlueprint Restore(string path)
    {
        if (!ListSavePoints().Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("このファイルのセーブポイントを選択してください。");
        var bytes = File.ReadAllBytes(path);
        var blueprint = Parse(bytes);
        CheckUnchanged();
        // Keep the discarded draft too, so restoration itself can be undone.
        Backup(IsDirty ? Encoding.UTF8.GetBytes(pending) : expected, IsDirty ? DateTime.UtcNow : File.GetLastWriteTimeUtc(FilePath));
        Write(bytes);
        saved = pending = blueprint.BuildJson(); elapsed = 0;
        return blueprint;
    }

    private void CheckUnchanged()
    {
        if (!File.ReadAllBytes(FilePath).AsSpan().SequenceEqual(expected))
            throw new IOException("元ファイルが外部で変更されています。上書きを停止しました。別名でエクスポートしてから開き直してください。");
    }

    private void Write(byte[] bytes)
    {
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            CheckUnchanged();
            File.Replace(temporary, FilePath, null);
            expected = bytes;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
