using StationeryUI.Editor;

internal static class EditorLaunchTests
{
    public static void Run()
    {
        var file = Path.GetFullPath("sample.stationery-ui.json");
        Check(EditorLaunchOptions.Parse([]).Mode == EditorLaunchMode.Welcome, "standalone welcome");
        var read = EditorLaunchOptions.Parse(["--mode", "read", "--file", file]);
        Check(read.Mode == EditorLaunchMode.Read && read.FilePath == file && read.LivePipe is null, "explicit file read");
        var live = EditorLaunchOptions.Parse(["--live", "test-pipe", "--mode", "read"]);
        Check(live.Mode == EditorLaunchMode.Read && live.LivePipe == "test-pipe", "fileless live read");
        Check(EditorLaunchOptions.Parse([file]).Mode == EditorLaunchMode.Edit, "legacy positional edit");
        Check(EditorLaunchOptions.Parse(["--mode", "edit", "--file", file, "--live", "test-pipe"]).Mode == EditorLaunchMode.Edit,
            "connected edit");
        foreach (var args in new[] { new[] { "--mode", "edit" }, ["--mode", "read"], ["--file", file],
            ["--mode", "write", "--file", file], ["--mode", "read", "--mode", "edit", "--file", file] })
        {
            try { _ = EditorLaunchOptions.Parse(args); throw new Exception("Invalid launch was accepted."); }
            catch (ArgumentException) { }
        }
    }

    private static void Check(bool ok, string description)
    { if (!ok) throw new Exception(description); }
}
