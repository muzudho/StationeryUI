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
        CheckNestedLayouts();
        CheckPartitions();
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
              "cells":[{"row":0,"col":1,"rowspan":2,"colspan":3,"slots":[{"id":"slot1"}]}]
            }
          ],
          "bindings":[{"layout":"/grid","parentModel":"/demo/demoPage","childrenModel":[{"model":"btn123","slot":"slot1"}]}]
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
        Check(model.Tree.SelectedItem!.Label == "(demoPage : Page)", "model tree hides placement");
        model.SetTreeMode(DeveloperTreeMode.Layout);
        Check(model.Tree.SelectedItem!.Label == "(demoPage : Page) (- : gridLayout)", "layout owner label and serialization");
        model.Select("/demo/demoPage/btn123");
        Check(model.Tree.SelectedItem!.Label == "(btn123 : Button) (1, 0, 3, 2 : -)", "column row column-span row-span order");
        Check(model.Tree.SelectedItem!.Parent!.Label == "(grid : Layout) (- : gridLayout)", "bound model is under actual layout");
        model.Select("/demo/demoPage:/grid");
        model.Tree.Toggle(model.Tree.SelectedItem!);
        model.SetTreeMode(DeveloperTreeMode.Model);
        Check(model.SelectedPath == "/demo/demoPage" && !model.Select("/demo/demoPage:/grid"), "model tree omits layout nodes");
        model.SetTreeMode(DeveloperTreeMode.Layout);
        Check(model.SelectedPath == "/demo/demoPage:/grid" && model.Capture().CollapsedPaths.Contains("/demo/demoPage:/grid"), "layout selection and collapse survive switching");
        var saved = System.Text.Json.JsonSerializer.Deserialize<DeveloperViewState>(System.Text.Json.JsonSerializer.Serialize(model.Capture()))!;
        var reopened = new DeveloperInspectionModel();
        reopened.Refresh(received.Entries); reopened.Restore(saved);
        Check(reopened.TreeMode == DeveloperTreeMode.Layout && reopened.SelectedPath == model.SelectedPath, "mode survives transport and reopen");
        reopened.SetTreeMode(DeveloperTreeMode.Model);
        Check(reopened.SelectedPath == "/demo/demoPage", "other tree selection survives reopen");
        model.Select("/demo/demoPage/btn123");
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
    private static void CheckNestedLayouts()
    {
        var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "App_Data", "demo.stationery-style.json")))
            directory = directory.Parent;
        var settings = StationeryUI.Styling.StationeryStyleSettings.Parse(System.IO.File.ReadAllText(
            System.IO.Path.Combine(directory!.FullName, "App_Data", "demo.stationery-style.json")));
        const string owner = "/demo/layoutDemoPage/body";
        StationeryInspectionEntry[] entries = [
            new("body", owner, null, "container", "", true, null),
            new("boxContent", owner + "/boxContent", owner, "textBlock", "", true, null)
        ];
        var model = new DeveloperInspectionModel();
        model.Refresh(DeveloperInspectionLayout.Apply(entries, settings));
        model.SetTreeMode(DeveloperTreeMode.Layout);
        Check(model.Select(owner + "/boxContent"), "nested model selectable by capture path");
        Check(model.PathFor(model.Tree.SelectedItem!.Parent!) == owner + ":/layoutShowcase/box/content", "model under nested content layout");
        Check(model.Select(owner + ":/layoutShowcase/box"), "hidden intermediate box is inspectable");
        Check(model.SelectedEntry!.BoxModel!.Padding.Left == 24 && model.SelectedEntry.BoxModel.Margin.Left == 6, "intermediate layout owns its own insets");
        var arranged = StationeryUI.Styling.StationeryLayoutEngine.Arrange(settings, 1000, 700);
        var snapshot = DeveloperInspectionLayout.Apply(entries, settings, arranged);
        var received = System.Text.Json.JsonSerializer.Deserialize<StationeryInspectionEntry[]>(
            System.Text.Json.JsonSerializer.Serialize(snapshot))!;
        Check(received[0].PartitionLines!.Count > 0 &&
            received[0].LayoutNodes!.Single(e => e.Id == "layoutShowcase").PartitionLines!.SequenceEqual(received[0].PartitionLines!),
            "root and layout partitions survive transport in window pixels");
        var key = owner + ":/layoutShowcase/box";
        var selection = DeveloperInspectionLayout.FindVisibleEntry(received, key);
        Check(selection?.WindowBounds == arranged.LayoutBounds[key], "nested layout window bounds survive transport");
        var contentKey = key + "/content";
        var content = DeveloperInspectionLayout.FindVisibleEntry(received, contentKey)!.WindowBounds!.Value;
        var outer = selection!.WindowBounds!.Value;
        Check(content.X == outer.X + 24 && content.Width == outer.Width - 48, "outline includes padding, child content is inset");
        Check(DeveloperCapture.HitTest(received, outer.X + 1, outer.Y + 1) is null, "layout metadata does not intercept model captures");
        Check(DeveloperInspectionLayout.FindVisibleEntry(DeveloperInspectionLayout.Apply(entries.Select(e => e with { Visible = false }).ToArray(), settings, arranged), key) is null, "hidden owner has no layout outline");
        Check(DeveloperInspectionLayout.FindVisibleEntry(DeveloperInspectionLayout.Apply(entries, settings), key) is null, "missing arrangement has no invented rectangle");
        model.Refresh(received);
        Check(model.Details.Contains("X="), "layout details show actual coordinates");
        var resized = StationeryUI.Styling.StationeryLayoutEngine.Arrange(settings, 1300, 900);
        model.Refresh(DeveloperInspectionLayout.Apply(entries, settings, resized));
        Check(model.SelectedPath == key && model.SelectedEntry!.WindowBounds == resized.LayoutBounds[key]
            && model.SelectedEntry.WindowBounds != outer, "resize updates selected layout rectangle");
        model.SetTreeMode(DeveloperTreeMode.Model);
        model.Select(owner + "/boxContent");
        Check(model.Tree.SelectedItem!.Parent!.Label == "(body : Container)", "model hierarchy unchanged");
    }
    private static void CheckPartitions()
    {
        var grid = new StationeryUI.Styling.StationeryLayoutNode("grid", "grid-layout", default,
            [new(1, true), new(1, true)], [new(1, true), new(1, true), new(1, true)])
        { Cells = [new(0, 0, 1, 2)] };
        var lines = DeveloperInspectionPartitions.Create(grid, new(10, 20, 300, 200));
        Check(lines.Count == 3 && lines.Contains(new(new(110, 120), new(110, 220)))
            && lines.Contains(new(new(210, 20), new(210, 220)))
            && lines.Contains(new(new(10, 120), new(310, 120))), "grid stops inside merged cells");
        var dock = grid with { Type = "dock-layout", Cells = [
            new(Dock: "center"), new(Dock: "top", Size: 40),
            new(Dock: "right", Size: 60), new(Dock: "top", Size: 20)] };
        var dockLines = DeveloperInspectionPartitions.Create(dock, new(10, 20, 300, 200));
        Check(dockLines.SequenceEqual(new StationeryInspectionLine[] {
            new(new(10, 60), new(310, 60)), new(new(250, 60), new(250, 220)),
            new(new(10, 80), new(250, 80)) }), "dock boundaries honor declaration order and final center");
        Check(DeveloperInspectionPartitions.Create(dock, new(0, 0, 0, 0)).Count == 0, "empty content has no partitions");
        var saturated = dock with { Cells = [new(Dock: "top", Size: 999), new(Dock: "right", Size: 30)] };
        Check(DeveloperInspectionPartitions.Create(saturated, new(0, 0, 100, 100)).Count == 0, "clipped docks do not invent partitions");
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
