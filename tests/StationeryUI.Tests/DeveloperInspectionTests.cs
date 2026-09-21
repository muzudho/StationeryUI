using StationeryUI.Inspection;

internal static class DeveloperInspectionTests
{
    public static void Run()
    {
        StationeryInspectionEntry[] source = [
            new("name", "/demo/right/name", "/demo/right", "textBox", "右の名前", false, new(5, 6, 30, 40)),
            new("demo", "/demo", null, "viewport", "画面", true, new(0, 0, 100, 200)),
            new("left", "/demo/left", "/demo", "page", "左", true, null),
            new("name", "/demo/left/name", "/demo/left", "textBox", "左の名前", true, new(1, 2, 3, 4)),
            new("right", "/demo/right", "/demo", "page", "右", false, null)
        ];
        var model = new DeveloperInspectionModel(); model.Refresh(source);
        Check(model.Select("/demo/right/name") && model.Details.Contains("右の名前") && model.Details.Contains("非表示"), "duplicate IDs resolved by path");
        var original = model.Tree;
        var updated = source.Select(e => e.Path == "/demo/right/name" ? e with { WindowBounds = new(10, 20, 300, 400) } : e).ToArray();
        model.Refresh(updated);
        Check(model.Tree == original && model.SelectedPath == "/demo/right/name" && model.Details.Contains("幅=300"), "live coordinates preserve selection and tree");
        model.Select("/demo/left"); model.Tree.Toggle(model.Tree.SelectedItem!);
        var state = model.Capture(.6);
        Check(state.CollapsedPaths.Contains("/demo/left"), "expansion captured by source path");
        model.Refresh(updated.Append(new("new", "/demo/new", "/demo", "button", "追加", true, null)).ToArray());
        Check(model.SelectedPath == "/demo/left" && model.Capture().CollapsedPaths.Contains("/demo/left"), "structure rebuild retains selection and collapse");
        model.Select("/demo/new"); model.Refresh(source);
        Check(model.SelectedPath == "/demo", "deleted selection falls back to root");
        model.Restore(state);
        Check(model.SelectedPath == "/demo/left", "reopened state restored");
        model.Refresh([]); Check(model.SelectedEntry is null && model.Details.Contains("選択"), "empty snapshot");
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
