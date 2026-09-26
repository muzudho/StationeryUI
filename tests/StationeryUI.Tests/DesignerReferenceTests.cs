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
