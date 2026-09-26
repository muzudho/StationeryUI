using StationeryUI.StyleDesigner;
using StationeryUI.Styling;
using System.Text.Json.Nodes;

internal static class DesignerReferenceTests
{
    public static void Run()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "demo.stationery-ui.json"));
        var plan = StyleBlueprint.Parse(source);
        var original = JsonNode.Parse(source)!;
        plan.AddPage("testPage");
        var demoBefore = DemoModelBinding.Create(StationeryStyleSettings.Parse(source));
        var demoAfter = DemoModelBinding.Create(StationeryStyleSettings.Parse(plan.BuildJson()));
        Check(demoBefore.Signature == demoAfter.Signature, "an extra page does not rebind the code-defined demo controls");
        foreach (var controls in new[] { demoAfter.Main, demoAfter.DialogControls, demoAfter.SplitControls, demoAfter.LayoutControls })
            foreach (var node in controls.Values)
                Check(controls[StationeryControlHandle.ModelRoleFromId(node.Id)] == node,
                    "host rebinding resolves the model role: " + node.Id);
        Check(plan.CreatePreview(1000, 700, "/csTestPageLayout").Layout.Bounds.Count > 0,
            "new page layout is previewable");
        var added = StyleBlueprint.Parse(plan.BuildJson());
        Check(added.PageNames.Contains("vTestPage"), "new page survives reopening");
        added.DeletePage("vTestPage");
        Check(JsonNode.DeepEquals(original, JsonNode.Parse(added.BuildJson())), "deleting a new page restores the original document");
        foreach (var pageName in added.PageNames)
        {
            var withoutPage = StyleBlueprint.Parse(source);
            withoutPage.DeletePage(pageName);
            Check(withoutPage.PageNames.Count == added.PageNames.Count - 1, "existing page deletion survives validation: " + pageName);
            var snapshot = JsonNode.Parse(withoutPage.BuildJson())!["initialViewportSnapshot"]![0]!["children"]!.AsArray();
            Check(snapshot.Count > 0 && withoutPage.PageNames.Contains((string?)snapshot[0]?["name"] ?? ""),
                "deleting the visible page selects a remaining page: " + pageName);
        }
        plan = StyleBlueprint.Parse(source);
        Check(JsonNode.DeepEquals(original, JsonNode.Parse(plan.BuildJson())), "opening preserves references and definitions");
        var settings = StationeryStyleSettings.Parse(source);
        var paths = StyleBlueprint.LayoutNodes(original).Select(l => l.Path).ToArray();
        Check(paths.ToHashSet().SetEquals(settings.Layouts.Select(l => l.Path)), "designer enumerates the same definitions as the runtime");
        Check(paths.Length == paths.Distinct().Count(), "references do not duplicate definitions");
        foreach (var id in plan.EditableLayouts)
        {
            plan.SelectLayout(id);
            var preview = plan.CreatePreview(1000, 700);
            Check(preview.Cells.Count > 0, "grid cells are previewable: " + id);
            foreach (var (handle, binding) in preview.Settings.ControlTree)
            {
                var scoped = binding.ModelPaths.FirstOrDefault(p => p.Value.StartsWith(preview.ScopePath + "/", StringComparison.Ordinal));
                if (scoped.Key is not null)
                    Check(preview.Layout.ControlLayoutPaths[handle] == binding.LayoutPaths[scoped.Key], "shared control uses preview page: " + handle);
            }
        }
        const string nested = "/csLayoutShowcaseGridLayout/grid/nestedGrid";
        plan.SelectLayout(nested);
        plan.Columns[0].Number = "2.5";
        var edited = JsonNode.Parse(plan.BuildJson())!;
        var expected = original.DeepClone();
        StyleBlueprint.FindLayout(expected, nested)!["column-definitions"]![0] = "2.5rate";
        Check(JsonNode.DeepEquals(expected, edited), "editing the referenced definition preserves the rest of the document");
        var reopened = StyleBlueprint.Parse(edited.ToJsonString());
        reopened.SelectLayout(nested);
        Check(reopened.Columns[0].Number == "2.5", "referenced grid edit survives reopening");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
