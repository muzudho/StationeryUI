using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;

internal static class StationeryStyleTests
{
    public static void Run()
    {
        Equal(StationeryStyleSettings.Default.Padding, StationeryStyleSettings.Parse(Style()).Padding);
        var settings = StationeryStyleSettings.Parse(Style("""{"top":"12.5px","right":"20px","bottom":"30px","left":"40px"}"""));
        Equal(new ScreenRectangle(40, 12.5, 940, 737.5), settings.Padding.GetContentBounds(1000, 780));
        Equal(new ScreenRectangle(40, 12.5, 0, 0), settings.Padding.GetContentBounds(50, 20));
        Equal(new ScreenRectangle(5, 5, 0, 0), settings.Padding.GetContentBounds(5, 5));
        Equal(new ViewportPadding(8, 8, 8, 0), StationeryStyleSettings.Parse(Style("""{"left":"0px"}""")).Padding);
        foreach (var bad in new[] { "null", "[]", "{", """{"viewport":null}""", """{"viewport":{"padding":[]}}""" })
            Reject(bad);
        foreach (var badValue in new[] { "8", "null", "true", "\"-1px\"", "\"2em\"", "\"NaNpx\"", "\"Infinitypx\"", "\"8\"", "\"\"" })
            Reject(Style("{\"top\":" + badValue + "}"));

        var directory = Path.Combine(Path.GetTempPath(), "stationery-style-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "style.json");
            var configPath = Path.Combine(directory, "config.json");
            void Configure(bool enabled, string stylePath = "style.json") => File.WriteAllText(configPath,
                JsonSerializer.Serialize(new { styleFile = stylePath, autoReload = enabled }));
            Configure(true);
            var file = new StationeryStyleFile(configPath);
            Equal(path, file.FilePath); // Relative to configuration, independent of the working directory.
            Equal(StationeryStyleSettings.Default.Padding, file.Current.Padding);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, Style("""{"left":"24px"}"""));
            Poll(file); // First read waits for a stable snapshot.
            Equal(8d, file.Current.Padding.Left);
            Poll(file);
            Equal(24d, file.Current.Padding.Left);
            Equal(null, file.LastError);
            File.WriteAllText(path, "{\"viewport\":");
            Poll(file); Poll(file);
            Equal(true, file.Configuration.AutoReload);
            Equal(24d, file.Current.Padding.Left);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, Style("""{"bottom":"80px"}"""));
            Poll(file); Poll(file);
            Equal(80d, file.Current.Padding.Bottom);
            Equal(8d, file.Current.Padding.Left); // Omitted sides reset to defaults.
            Configure(false);
            Poll(file); Poll(file);
            Equal(false, file.Configuration.AutoReload);
            File.WriteAllText(path, Style("""{"right":"16px"}"""));
            Poll(file); Poll(file);
            Equal(8d, file.Current.Padding.Right); // Style edits are frozen while disabled.
            Configure(true);
            Poll(file); Poll(file); Poll(file);
            Equal(true, file.Configuration.AutoReload); // No Reload/F5 or restart required.
            Equal(16d, file.Current.Padding.Right);
            File.Delete(path);
            Poll(file);
            Equal(16d, file.Current.Padding.Right);
            Equal(true, file.LastError is not null);
            File.WriteAllText(path, Style());
            Poll(file); Poll(file);
            Equal(StationeryStyleSettings.Default.Padding, file.Current.Padding);
            Equal(null, file.LastError);
            // Atomic-save replacement and same-length edits are detected by content, not timestamps.
            var replacement = Path.Combine(directory, "replacement.json");
            File.WriteAllText(replacement, Style("""{"right":"32px"}"""));
            File.Move(replacement, path, true);
            Poll(file); Poll(file);
            Equal(32d, file.Current.Padding.Right);
            File.WriteAllText(path, Style("""{"right":"64px"}"""));
            Poll(file); Poll(file);
            Equal(64d, file.Current.Padding.Right);

            // Invalid configuration never replaces the last good loading policy.
            foreach (var invalid in new[] { "{", "null", "[]", """{"autoReload":"false"}""",
                """{"styleFile":""}""", """{"styleFile":null}""", """{"styleFile":"\u0000"}""" })
            {
                File.WriteAllText(configPath, invalid);
                Poll(file); Poll(file);
                Equal(path, file.FilePath);
                Equal(true, file.Configuration.AutoReload);
                Equal(true, file.LastError is not null);
            }
            File.Delete(configPath);
            Poll(file);
            Equal(true, file.LastError is not null);
            Configure(false);
            Poll(file); Poll(file);
            Equal(null, file.LastError);

            // Manual reload still works with auto reload off.
            File.WriteAllText(path, Style());
            Equal(true, file.Reload());
            Equal(StationeryStyleSettings.Default.Padding, file.Current.Padding);
            Equal(false, file.Configuration.AutoReload);

            // Switching paths loads the new file once even while auto reload is off.
            var alternate = Path.Combine(directory, "alternate.json");
            File.WriteAllText(alternate, Style("""{"top":"99px"}"""));
            Configure(false, "alternate.json");
            Poll(file); Poll(file); Poll(file);
            Equal(alternate, file.FilePath);
            Equal(99d, file.Current.Padding.Top);
            File.WriteAllText(alternate, Style());
            Poll(file); Poll(file);
            Equal(99d, file.Current.Padding.Top);
            Configure(true, alternate); // Absolute paths also work.
            Poll(file); Poll(file); Poll(file);
            Equal(StationeryStyleSettings.Default.Padding, file.Current.Padding);

            Configure(false, "missing.json");
            Poll(file); Poll(file); Poll(file);
            Equal(true, file.LastError is not null);
            Equal(StationeryStyleSettings.Default.Padding, file.Current.Padding);
            File.WriteAllText(Path.Combine(directory, "missing.json"), Style("""{"left":"42px"}"""));
            Poll(file); Poll(file);
            Equal(42d, file.Current.Padding.Left);
            Equal(null, file.LastError);

            // Startup with auto reload disabled still loads the configured style once.
            var disabled = new StationeryStyleFile(configPath);
            Equal(false, disabled.Configuration.AutoReload);
            Equal(42d, disabled.Current.Padding.Left);
            // An unavailable config uses defaults and recovers automatically when created.
            var lateConfig = Path.Combine(directory, "late-config.json");
            var recovering = new StationeryStyleFile(lateConfig);
            Equal(true, recovering.LastError is not null);
            File.Copy(configPath, lateConfig);
            Poll(recovering); Poll(recovering); Poll(recovering);
            Equal(42d, recovering.Current.Padding.Left);
            Equal(null, recovering.LastError);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static string Style(string padding = "{}") =>
        "{\"models\":[{\"id\":\"demo\",\"type\":\"viewport\"}],\"layouts\":[{\"id\":\"rootPanel\",\"type\":\"box-layout\",\"padding\":" + padding + "}],\"bindings\":[{\"layout\":\"rootPanel\",\"model\":\"demo\"}]}";
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
