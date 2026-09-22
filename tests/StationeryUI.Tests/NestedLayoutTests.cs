using StationeryUI.Canvas;
using StationeryUI.Styling;
using StationeryUI.StyleDesigner;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class NestedLayoutTests
{
    public const string Source = """
        {
          "models":[
            {
              "id":"demo",
              "type":"viewport",
              "children":[{"id":"a","type":"button"},{"id":"b","type":"button"},{"id":"c","type":"button"},{"id":"d","type":"button"}]
            }
          ],
          "layouts":[
            {
              "id":"frame",
              "type":"box-layout",
              "padding":{"top":"10px","right":"20px","bottom":"30px","left":"40px"},
              "children":[
                {
                  "id":"grid",
                  "type":"grid-layout",
                  "row-definitions":["50px","1rate","2rate"],
                  "column-definitions":["60px","1rate","3rate"],
                  "children":[
                    {
                      "id":"box",
                      "type":"box-layout",
                      "row":0,
                      "col":0,
                      "rowspan":2,
                      "colspan":2,
                      "padding":{"top":"5px","right":"5px","bottom":"5px","left":"5px"},
                      "children":[
                        {
                          "id":"inner",
                          "type":"grid-layout",
                          "row-definitions":["1rate"],
                          "column-definitions":["1rate","3rate"],
                          "slots":[{"id":"slot1","row":0,"col":0},{"id":"slot2","row":0,"col":1}]
                        }
                      ]
                    },
                    {
                      "id":"inner",
                      "type":"grid-layout",
                      "row":2,
                      "col":1,
                      "colspan":2,
                      "row-definitions":["1rate"],
                      "column-definitions":["1rate","1rate"],
                      "slots":[{"id":"slot1","row":0,"col":0,"colspan":2}]
                    }
                  ],
                  "slots":[{"id":"slot1","row":0,"col":2,"rowspan":2}]
                }
              ]
            }
          ],
          "bindings":[
            {"layout":"/frame","model":"demo"},
            {"layout":"/frame/grid","parentModel":"demo","childrenModel":[{"model":"d","slot":"slot1"}]},
            {"layout":"/frame/grid/box/inner","parentModel":"demo","childrenModel":[{"model":"a","slot":"slot1"},{"model":"b","slot":"slot2"}]},
            {"layout":"/frame/grid/inner","parentModel":"demo","childrenModel":[{"model":"c","slot":"slot1"}]}
          ]
        }
        """;
    public static void Run()
    {
        var settings = StationeryStyleSettings.Parse(Source);
        var layout = StationeryLayoutEngine.Arrange(settings, 400, 300);
        Equal(new(45, 15, 30, 110), layout.Bounds["/demo/a"]);
        Equal(new(75, 15, 90, 110), layout.Bounds["/demo/b"]);
        Equal(new(100, 130, 280, 140), layout.Bounds["/demo/c"]);
        Equal(new(170, 10, 210, 120), layout.Bounds["/demo/d"]);
        Check(settings.Layouts.Count(l => l.Id == "inner") == 2, "local duplicate IDs have distinct paths");
        var reversed = JsonNode.Parse(Source)!;
        reversed["bindings"] = new JsonArray(reversed["bindings"]!.AsArray().Reverse().Select(b => b!.DeepClone()).ToArray());
        Equal(layout.Bounds["/demo/a"], StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(reversed.ToJsonString()), 400, 300).Bounds["/demo/a"]);
        foreach (var size in new[] { 0, 1, 20, 800 })
        {
            var resized = StationeryLayoutEngine.Arrange(settings, size, size);
            Check(resized.Bounds.Values.Concat(resized.LayoutContentBounds.Values).All(r => r.Width >= 0 && r.Height >= 0), "small window remains nonnegative");
        }
        var old = StationeryStyleSettings.Parse(Source.Replace("box-layout", "panel").Replace("grid-layout", "floating-layout").Replace("\"col\":", "\"column\":"));
        Equal(layout.Bounds["/demo/c"], StationeryLayoutEngine.Arrange(old, 400, 300).Bounds["/demo/c"]);
        var leafOnly = JsonNode.Parse(Source)!;
        leafOnly["bindings"]!.AsArray().RemoveAt(0);
        var inferred = StationeryStyleSettings.Parse(leafOnly.ToJsonString());
        Equal(layout.Bounds["/demo/a"], StationeryLayoutEngine.Arrange(inferred, 400, 300).Bounds["/demo/a"]);
        Check(inferred.Padding.Left == 40, "ancestor padding is inferred from descendant binding");
        var reused = StationeryStyleSettings.Parse("""
        {
          "models":[
            {
              "id":"app",
              "type":"viewport",
              "children":[
                {"id":"left","type":"container","children":[{"id":"name","type":"textBox"}]},
                {"id":"right","type":"container","children":[{"id":"name","type":"textBox"}]}
              ]
            }
          ],
          "layouts":[
            {
              "id":"outer",
              "type":"grid-layout",
              "row-definitions":["1rate"],
              "column-definitions":["1rate","3rate"],
              "slots":[{"id":"slot1","row":0,"col":0},{"id":"slot2","row":0,"col":1}]
            },
            {
              "id":"frame",
              "type":"box-layout",
              "padding":{"top":"2px","right":"2px","bottom":"2px","left":"2px"},
              "children":[{"id":"grid","type":"grid-layout","row-definitions":["1rate"],"column-definitions":["1rate"],"slots":[{"id":"slot1","row":0,"col":0}]}]
            }
          ],
          "bindings":[
            {"layout":"/outer","parentModel":"app","childrenModel":[{"model":"left","slot":"slot1"},{"model":"right","slot":"slot2"}]},
            {"layout":"/frame/grid","parentModel":"app/left","childrenModel":[{"model":"name","slot":"slot1"}]},
            {"layout":"/frame/grid","parentModel":"app/right","childrenModel":[{"model":"name","slot":"slot1"}]}
          ]
        }
        """);
        var reusedLayout = StationeryLayoutEngine.Arrange(reused, 400, 100);
        Equal(new(2, 2, 96, 96), reusedLayout.Bounds["/app/left/name"]);
        Equal(new(102, 2, 296, 96), reusedLayout.Bounds["/app/right/name"]);
        Reject(s => s["layouts"]![0]!["children"]!.AsArray().Add(s["layouts"]![0]!["children"]![0]!.DeepClone()));
        Reject(s => { var child = Grid(s).DeepClone(); child["id"] = "different"; s["layouts"]![0]!["children"]!.AsArray().Add(child); });
        Reject(s => Grid(s)["children"]![0]!["rowspan"] = 0);
        Reject(s => Grid(s)["children"]![0]!["colspan"] = int.MaxValue);
        Reject(s => Grid(s)["children"]![0]!["col"] = -1);
        Reject(s => Grid(s)["children"]![0]!["row"] = .5);
        Reject(s => Grid(s)["children"]![0]!["rowspan"] = null);
        Reject(s => Grid(s)["children"]![0]!["column"] = 0);
        Reject(s => Grid(s)["children"]![1]!["row"] = 1);
        Reject(s => Grid(s)["children"]![1]!["id"] = "box");
        Reject(s => s["bindings"]![2]!["layout"] = "/inner");
        Reject(s => s["bindings"]![1]!["childrenModel"]![0]!["col"] = 1);
        Reject(s => s["layouts"]![0]!["row"] = 0);
        Reject(s => s["layouts"]![0]!["children"] = null);
        Reject(s => Grid(s)["row"] = 0); // box has no cells
        Reject(s => s["bindings"]![3]!["childrenModel"]![0]!["rowspan"] = 2);

        var plan = StyleBlueprint.Parse(Source);
        plan.SelectLayout("/frame/grid/box/inner");
        Check(plan.SelectedLayoutType == "grid-layout" && plan.EditableLayouts.Count == 3, "nested grids are editable");
        Equal(new(45, 15, 30, 110), plan.CreatePreview(400, 300).Cells[0].Bounds);
        plan.Columns[0].Number = "3";
        var saved = plan.BuildJson();
        Check(StyleBlueprint.FindLayout(JsonNode.Parse(saved)!, "/frame/grid/box/inner")!["colspan"] is null, "box child has no placement invented");
        Equal(new(45, 15, 60, 110), plan.CreatePreview(400, 300).Cells[0].Bounds);
        var before = plan.BuildJson();
        try { plan.SelectLayout("/frame/grid"); plan.Resize(1, 1); throw new Exception("resize accepted"); } catch (ArgumentException) { }
        plan.SelectLayout("/frame/grid/box/inner");
        plan.RenameId(["layouts","0","children","0","children","0"], "renamed");
        Check(plan.SelectedLayoutId == "/frame/grid/renamed/inner", "renaming parent updates selected descendant");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings.Any(b => b.Layout == "/frame/grid/renamed/inner"), "rename updates descendant bindings");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings.Any(b => b.Layout == "/frame/grid/inner"), "rename does not affect same local ID elsewhere");
        before = plan.BuildJson();
        try { plan.DeleteNode(["layouts","0","children","0","children","0"]); throw new Exception("bound subtree deleted"); } catch (JsonException) { }
        Check(plan.BuildJson() == before, "invalid delete is atomic");

        var empty = StyleBlueprint.Parse("""
        {"models":[{"id":"app","type":"viewport"}],"layouts":[],"bindings":[]}
        """);
        empty.AddLayout("box-layout", "root");
        empty.AddLayout("grid-layout", "cells", "/root");
        empty.AddLayout("box-layout", "item", "/root/cells", 0, 0, 2, 1);
        empty.AddLayout("grid-layout", "inside", "/root/cells/item");
        empty.AddLayout("box-layout", "other", "/root/cells", 0, 1, 2, 1);
        var roundtrip = StyleBlueprint.Parse(empty.BuildJson());
        roundtrip.SelectLayout("/root/cells/item/inside");
        Check(roundtrip.CreatePreview(300, 200).Standalone, "unbound nested subtree preview");
        before = empty.BuildJson();
        try { empty.AddLayout("grid-layout", "extra", "/root"); throw new Exception("second box child accepted"); } catch (JsonException) { }
        Check(empty.BuildJson() == before, "box child cap enforced atomically");
        try { empty.SetPlacement("/root/cells/other", 1, 0); throw new Exception("overlap accepted"); } catch (JsonException) { }
        Check(empty.BuildJson() == before, "placement failure is atomic");
        empty.DeleteNode(["layouts","0","children","0","children","1"]);
        empty.SetPlacement("/root/cells/item", 0, 0, 2, 2);
        roundtrip = StyleBlueprint.Parse(empty.BuildJson());
        Check(StationeryStyleSettings.Parse(roundtrip.BuildJson()).Layouts.Single(l => l.Path == "/root/cells/item").ColumnSpan == 2, "span changes survive save and reopen");
    }
    private static JsonNode Grid(JsonNode root) => root["layouts"]![0]!["children"]![0]!;
    private static void Reject(Action<JsonNode> edit)
    {
        var json = JsonNode.Parse(Source)!; edit(json);
        try { StationeryStyleSettings.Parse(json.ToJsonString()); } catch (JsonException) { return; }
        throw new Exception("Invalid nested layout accepted: " + json);
    }
    private static void Equal(ScreenRectangle expected, ScreenRectangle actual) => Check(
        Math.Abs(expected.X - actual.X) < 1e-8 && Math.Abs(expected.Y - actual.Y) < 1e-8 &&
        Math.Abs(expected.Width - actual.Width) < 1e-8 && Math.Abs(expected.Height - actual.Height) < 1e-8,
        $"expected {expected}, got {actual}");
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
