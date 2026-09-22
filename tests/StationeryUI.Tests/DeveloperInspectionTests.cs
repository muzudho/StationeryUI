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
        model.Select("/demo/left/name");
        Check(model.Tree.TargetItem == model.Tree.SelectedItem && model.Tree.VisibleRows().Any(r => r.Item == model.Tree.SelectedItem),
            "captured node becomes selected and targeted and expands its ancestors");
        Check(model.IdPath == "demo.left.name" && model.Details.Contains("demo.left.name"), "dot separated ID path");
        Check(DeveloperCapture.HitTest(source, 2, 3)?.Path == "/demo/left/name", "deepest visible component wins over root");
        Check(DeveloperCapture.HitTest(source, 6, 7)?.Path == "/demo", "hidden component is excluded");
        Check(DeveloperCapture.HitTest(source, 4, 3)?.Path == "/demo", "right edge excluded");
        Check(DeveloperCapture.HitTest(source, -1, 3) is null, "outside has no hit");
        Check(DeveloperCapture.HitTest(source, 2, 3, "/demo/right") is null, "modal scope excludes background");
        var packet = new DeveloperInspectionMessage(source, 1, model.Capture() with { CaptureEnabled = true }, "/demo/left/name", 2);
        var roundtrip = System.Text.Json.JsonSerializer.Deserialize<DeveloperInspectionMessage>(System.Text.Json.JsonSerializer.Serialize(packet))!;
        Check(roundtrip.CaptureSequence == 2 && roundtrip.CapturePath == model.SelectedPath && roundtrip.RestoreState!.CaptureEnabled,
            "capture command and toggle survive pipe serialization");
        model.Refresh([]); Check(model.SelectedEntry is null && model.Details.Contains("選択"), "empty snapshot");
        CheckLayoutLabels();
    }

    private static void CheckLayoutLabels()
    {
        var settings = StationeryUI.Styling.StationeryStyleSettings.Parse("""
        {
          "models":[{"id":"demo","type":"viewport","children":[{"id":"demoPage","type":"page","children":[{"id":"btn123","type":"button"}]}]}],
          "layouts":[
            {
              "id":"grid",
              "type":"grid-layout",
              "row-definitions":["1rate","1rate","1rate"],
              "column-definitions":["1rate","1rate","1rate","1rate"],
              "slots":[{"id":"slot1","row":0,"col":1,"rowspan":2,"colspan":3}]
            }
          ],
          "bindings":[{"layout":"grid","parentModel":"/demo/demoPage","childrenModel":[{"model":"btn123","slot":"slot1"}]}]
        }
        """);
        StationeryInspectionEntry[] entries = [
            new("demo", "/demo", null, "viewport", "", true, null),
            new("demoPage", "/demo/demoPage", "/demo", "page", "", true, null),
            new("btn123", "/demo/demoPage/btn123", "/demo/demoPage", "button", "", true, null)
        ];
        var enriched = DeveloperInspectionLayout.Apply(entries, settings);
        var packet = new DeveloperInspectionMessage(enriched.ToArray(), 1, null);
        var received = System.Text.Json.JsonSerializer.Deserialize<DeveloperInspectionMessage>(System.Text.Json.JsonSerializer.Serialize(packet))!;
        var model = new DeveloperInspectionModel(); model.Refresh(received.Entries);
        model.Select("/demo/demoPage");
        Check(model.Tree.SelectedItem!.Label == "(demoPage : Page) (- : gridLayout)", "layout owner label and serialization");
        model.Select("/demo/demoPage/btn123");
        Check(model.Tree.SelectedItem!.Label == "(btn123 : Button) (1, 0, 3, 2 : -)", "column row column-span row-span order");
        var tree = model.Tree;
        var changed = enriched.Select(e => e.Id == "btn123" ? e with { Cell = new(1, 1, 1, 1), Visible = false } : e).ToArray();
        model.Refresh(changed);
        Check(model.Tree == tree && model.SelectedPath == "/demo/demoPage/btn123"
            && model.Tree.SelectedItem!.Label == "(btn123 : Button) (1, 1, 1, 1 : -)  （非表示）", "live placement label preserves selection");
        Check(DeveloperInspectionLayout.FormatLabel(entries[0]) == "(demo : Viewport) (- : -)", "legacy snapshots remain displayable");
        var cleared = DeveloperInspectionLayout.Apply(enriched, settings with { Bindings = [] });
        Check(cleared.All(e => e.LayoutTypes is null && e.Cell is null), "removed bindings clear old metadata");
        Check(DeveloperInspectionLayout.FormatLabel(enriched[2] with { LayoutTypes = ["grid-layout"] })
            == "(btn123 : Button) (1, 0, 3, 2 : gridLayout)", "child placement and parent layout appear together");
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
