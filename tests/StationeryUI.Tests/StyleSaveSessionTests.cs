using StationeryUI.Editor;
using System.Text;
using System.Text.Json;

internal static class StyleSaveSessionTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "stationery-save-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "banana.stationery-ui.json");
            var draft = new StyleBlueprint();
            var original = draft.BuildJson();
            File.WriteAllText(path, original, new UTF8Encoding(true));
            var originalBytes = File.ReadAllBytes(path);
            var timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, timestamp);
            var (session, loaded) = StyleSaveSession.Open(path);
            Check(File.ReadAllBytes(path + ".1.bak").SequenceEqual(originalBytes), "exact opening backup including BOM");
            Check(File.GetLastWriteTimeUtc(path + ".1.bak") == timestamp, "source modification time retained");
            Check(!session.Tick(10) && File.GetLastWriteTimeUtc(path) == timestamp, "opening alone does not rewrite");
            loaded.Columns[0].Number = "2";
            session.Observe(loaded.BuildJson());
            Check(!session.Tick(1) && session.IsDirty && session.Progress > .6, "timer waits");
            loaded.Columns[0].Number = "3"; session.Observe(loaded.BuildJson());
            Check(!session.Tick(1), "new edit resets timer");
            Check(session.Tick(.5) && !session.IsDirty && File.ReadAllText(path).Contains("3rate"), "timer overwrites source");
            Check(session.ListSavePoints().Count == 1, "autosave does not create extra generations");
            loaded.Columns[0].Number = "4"; session.Observe(loaded.BuildJson());
            var restored = session.Restore(path + ".1.bak");
            Check(restored.BuildJson() == original && File.ReadAllBytes(path).SequenceEqual(originalBytes), "restore exact bytes");
            Check(File.ReadAllText(path + ".2.bak").Contains("4rate"), "restore preserves pending draft");
            session.Restore(path + ".2.bak");
            Check(File.ReadAllText(path).Contains("4rate"), "undo restoration");
            session.Observe("invalid");
            Expect<JsonException>(session.Flush);
            Check(File.ReadAllText(path).Contains("4rate"), "invalid content never overwrites");
            session.Observe(original);
            File.WriteAllText(path, "external update");
            Expect<IOException>(session.Flush);
            Expect<IOException>(() => session.Restore(path + ".1.bak"));
            Check(File.ReadAllText(path) == "external update", "external update protected");
            File.WriteAllText(path, original);
            for (var i = 0; i < 25; i++) session = StyleSaveSession.Open(path).Session;
            var points = session.ListSavePoints();
            Check(points.Count == 20 && points[0].Generation == 28 && points[^1].Generation == 9, "20 monotonic generations");
            Check(!File.Exists(path + ".1.bak"), "oldest generation removed");
            session.Restore(points[^1].Path); // Selected oldest can be pruned during restore backup.
            Check(session.ListSavePoints().Count == 20, "restore also limits retention");
            session.Observe(original); Check(!session.Tick(2), "same content remains clean");
            var bad = Path.Combine(directory, "invalid.stationery-ui.json");
            File.WriteAllText(bad, "invalid");
            Expect<JsonException>(() => StyleSaveSession.Open(bad));
            Check(!File.Exists(bad + ".1.bak"), "failed import does not start session");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}
