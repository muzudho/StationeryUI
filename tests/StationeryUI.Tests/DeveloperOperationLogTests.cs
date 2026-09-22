using Microsoft.Xna.Framework.Input;
using StationeryUI.MonoGame;
using System.Text.Json;

static class DeveloperOperationLogTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "stationery-operation-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            string path;
            var state = new DeveloperOperationState(true, "/inspector/tree", "/inspector/tree", "/game/a", "/game/a",
                false, .4, 0, 0, "", 1000, 660);
            using (var log = new DeveloperOperationLog(directory))
            {
                path = log.FilePath;
                log.HostIsActive = true;
                log.Record(Mouse(false), new(), state);
                var count = File.ReadAllLines(path).Length;
                log.Record(Mouse(false), new(), state);
                Check(File.ReadAllLines(path).Length == count, "idle frames are not logged");
                log.Record(Mouse(true), new(), state);
                log.Record(Mouse(false), new(), state with { SelectedPath = "/game/b", TargetPath = "/game/b" });
                log.HostIsActive = false;
                log.Record(Mouse(true, 120), new(), state with { InputEnabled = false });
                log.Record(Mouse(true, 120), new(Keys.A), state with { InputEnabled = false });
                Check(File.ReadAllLines(path).Length == count + 3, "typed text is excluded");
                Check(log.LastError is null, "successful writes");
            }
            var rows = File.ReadAllLines(path).Select(line => JsonDocument.Parse(line)).ToArray();
            try
            {
                Check(rows.Length == 6, "start, four updates, end");
                Check(rows[0].RootElement.GetProperty("Kind").GetString() == "session-start", "session start");
                Check(rows[^1].RootElement.GetProperty("Kind").GetString() == "session-end", "session end");
                var click = rows[3].RootElement.GetProperty("Data");
                Check(click.GetProperty("Before").GetProperty("SelectedPath").GetString() == "/game/a"
                    && click.GetProperty("After").GetProperty("SelectedPath").GetString() == "/game/b", "selection transition");
                var background = rows[4].RootElement.GetProperty("Data");
                Check(!background.GetProperty("HostIsActive").GetBoolean()
                    && !background.GetProperty("After").GetProperty("InputEnabled").GetBoolean(), "background input retained for diagnosis");
                Check(background.GetProperty("Mouse").GetProperty("WheelDelta").GetInt32() == 120, "wheel delta");
                for (var i = 0; i < rows.Length; i++)
                {
                    Check(rows[i].RootElement.GetProperty("Sequence").GetInt64() == i + 1, "ordered sequence");
                    _ = rows[i].RootElement.GetProperty("TimestampUtc").GetDateTimeOffset();
                }
            }
            finally { foreach (var row in rows) row.Dispose(); }
            // An unwritable destination must not break inspector input.
            var file = Path.Combine(directory, "not-a-directory");
            File.WriteAllText(file, "fixture");
            using var failed = new DeveloperOperationLog(file);
            failed.Record(Mouse(true), new(), state);
            Check(failed.LastError is not null, "I/O failure reported without throwing");
            using var other = new DeveloperOperationLog(directory);
            Check(other.FilePath != path, "sessions have separate files");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static MouseState Mouse(bool down, int wheel = 0) => new(30, 80, wheel,
        down ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
