using StationeryUI.Editor;
using System.Text.Json;

internal static class ReadStyleSnapshotTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "stationery-read-snapshot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "style.stationery-ui.json");
            var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json");
            var original = File.ReadAllText(fixture);
            File.WriteAllText(path, original);
            var timestamp = File.GetLastWriteTimeUtc(path);
            var snapshot = ReadStyleSnapshot.Open(path);
            Check(!snapshot.Refresh(), "unchanged text does not rebuild the preview");
            Check(File.GetLastWriteTimeUtc(path) == timestamp && !Directory.EnumerateFiles(directory, "*.bak").Any(),
                "opening and polling do not write the source or backups");

            File.WriteAllText(path, original + " ");
            File.SetLastWriteTimeUtc(path, timestamp);
            Check(snapshot.Refresh() && snapshot.Text == original + " ",
                "changed content is noticed even with the original modification time");

            File.WriteAllText(path, "{");
            File.SetLastWriteTimeUtc(path, timestamp);
            try { snapshot.Refresh(); throw new Exception("Invalid style was accepted."); }
            catch (JsonException) { }
            Check(snapshot.Text == original + " " && snapshot.Blueprint.IsImported,
                "parse failure retains the last valid preview");

            File.WriteAllText(path, original + "  ");
            File.SetLastWriteTimeUtc(path, timestamp);
            Check(snapshot.Refresh() && snapshot.Text == original + "  ", "valid contents recover after a failed refresh");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
