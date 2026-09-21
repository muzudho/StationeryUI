using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;

internal static class StationeryStyleTests
{
    public static void Run()
    {
        Equal(StationeryStyleSettings.Default, StationeryStyleSettings.Parse("{}"));
        var settings = StationeryStyleSettings.Parse("""
            {"autoReload":false,"viewport":{"padding":{"top":"12.5px","right":"20px","bottom":"30px","left":"40px"},"contents":[]}}
            """);
        Equal(false, settings.AutoReload);
        Equal(new ScreenRectangle(40, 12.5, 940, 737.5), settings.Padding.GetContentBounds(1000, 780));
        Equal(new ScreenRectangle(40, 12.5, 0, 0), settings.Padding.GetContentBounds(50, 20));
        Equal(new ScreenRectangle(5, 5, 0, 0), settings.Padding.GetContentBounds(5, 5));
        Equal(new ViewportPadding(8, 8, 8, 0), StationeryStyleSettings.Parse("""{"viewport":{"padding":{"left":"0px"}}}""").Padding);
        foreach (var bad in new[] { "null", "[]", "{", """{"autoReload":"false"}""", """{"viewport":null}""", """{"viewport":{"padding":[]}}""" })
            Reject(bad);
        foreach (var badValue in new[] { "8", "null", "true", "\"-1px\"", "\"2em\"", "\"NaNpx\"", "\"Infinitypx\"", "\"8\"", "\"\"" })
            Reject("{\"viewport\":{\"padding\":{\"top\":" + badValue + "}}}");

        var directory = Path.Combine(Path.GetTempPath(), "stationery-style-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "style.json");
            var file = new StationeryStyleFile(path);
            Equal(StationeryStyleSettings.Default, file.Current);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, """{"viewport":{"padding":{"left":"24px"}}}""");
            Poll(file); // First read waits for a stable snapshot.
            Equal(8d, file.Current.Padding.Left);
            Poll(file);
            Equal(24d, file.Current.Padding.Left);
            Equal(null, file.LastError);
            File.WriteAllText(path, "{\"autoReload\":false,");
            Poll(file); Poll(file);
            Equal(true, file.Current.AutoReload);
            Equal(24d, file.Current.Padding.Left);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, """{"autoReload":false,"viewport":{"padding":{"bottom":"80px"}}}""");
            Poll(file); Poll(file);
            Equal(false, file.Current.AutoReload);
            Equal(80d, file.Current.Padding.Bottom);
            Equal(8d, file.Current.Padding.Left); // Omitted sides reset to defaults.
            File.WriteAllText(path, """{"autoReload":true,"viewport":{"padding":{"right":"16px"}}}""");
            Poll(file); Poll(file);
            Equal(false, file.Current.AutoReload);
            Equal(true, file.Reload());
            Equal(true, file.Current.AutoReload);
            Equal(16d, file.Current.Padding.Right);
            File.Delete(path);
            Poll(file);
            Equal(16d, file.Current.Padding.Right);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, "{}");
            Poll(file); Poll(file);
            Equal(StationeryStyleSettings.Default, file.Current);
            Equal(null, file.LastError);
            // Atomic-save replacement and same-length edits are detected by content, not timestamps.
            var replacement = Path.Combine(directory, "replacement.json");
            File.WriteAllText(replacement, """{"viewport":{"padding":{"right":"32px"}}}""");
            File.Move(replacement, path, true);
            Poll(file); Poll(file);
            Equal(32d, file.Current.Padding.Right);
            File.WriteAllText(path, """{"viewport":{"padding":{"right":"64px"}}}""");
            Poll(file); Poll(file);
            Equal(64d, file.Current.Padding.Right);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Poll(StationeryStyleFile file) => file.Update(TimeSpan.FromMilliseconds(500));
    private static void Reject(string json)
    {
        try { StationeryStyleSettings.Parse(json); }
        catch (JsonException) { return; }
        throw new Exception("Expected invalid style to be rejected: " + json);
    }
    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
    }
}
