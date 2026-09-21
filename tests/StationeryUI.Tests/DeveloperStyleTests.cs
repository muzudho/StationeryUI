using StationeryUI.MonoGame;
using StationeryUI.Styling;
using System.Text.Json;

static class DeveloperStyleTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "stationery-dev-style-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "style.json");
        try
        {
            // Exercise the actual library resource without a development checkout or an external file.
            var embedded = StationeryDeveloperStyle.Load(path);
            Check(embedded.Width == 1000 && embedded.Height == 660 && embedded.LastError is null);
            var bounds = StationeryLayoutEngine.Arrange(embedded.Settings, 1000, 660).Bounds;
            Check(bounds["/developerViewport/developerWindow/inspectorSplit"] == new StationeryUI.Canvas.ScreenRectangle(8, 64, 984, 460));
            foreach (var size in new[] { (1000, 660), (720, 480), (1200, 900), (40, 40) })
            {
                var layout = StationeryLayoutEngine.Arrange(embedded.Settings, size.Item1, size.Item2);
                var panel = layout.Bounds["/developerViewport/developerWindow/inspectorPanel"];
                var hint = layout.Bounds["/developerViewport/developerWindow/inspectorPanel/toolHint"];
                Check(panel.X == 0 && panel.Width == size.Item1 && panel.Height == Math.Min(80, size.Item2));
                Check(panel.Y + panel.Height == size.Item2);
                Check(hint.X >= panel.X && hint.Y >= panel.Y && hint.X + hint.Width <= panel.Width && hint.Y + hint.Height <= size.Item2);
                Check(layout.Bounds["/developerViewport/developerWindow/copyPath"].Y + layout.Bounds["/developerViewport/developerWindow/copyPath"].Height <= panel.Y);
            }
            using var stream = typeof(StationeryDeveloperStyle).Assembly.GetManifestResourceStream("StationeryUI.dev-window.stationery-style.json")!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd().Replace("1000", "1200").Replace("56px", "80px").Replace("0.4", "0.6");
            File.WriteAllText(path, json);
            var custom = StationeryDeveloperStyle.Load(path);
            Check(custom.Width == 1200 && custom.SplitOptions.Ratio == .6 && custom.LastError is null);
            Check(StationeryLayoutEngine.Arrange(custom.Settings, 1200, 660).Bounds["/developerViewport/developerWindow/inspectorSplit"].Y == 88);
            foreach (var invalid in new[] { "{", json.Replace("1200", "0"), json.Replace("copyPath", "missingButton"), json.Replace("\"width\": 1200", "\"width\": null") })
            {
                File.WriteAllText(path, invalid);
                var fallback = StationeryDeveloperStyle.Load(path);
                Check(fallback.Width == 1000 && fallback.LastError is not null);
            }
            foreach (var size in new[] { 0, 1, 100, 1000 })
                foreach (var rectangle in StationeryLayoutEngine.Arrange(custom.Settings, size, size).Bounds.Values)
                    Check(rectangle.Width >= 0 && rectangle.Height >= 0);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new Exception("Developer style assertion failed.");
    }
}
