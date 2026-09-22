using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class GridLayoutTests
{
    private const string Source = """
        {
          "models":[{"id":"screen","type":"viewport","children":[{"id":"a","type":"button"},{"id":"b","type":"button"},{"id":"c","type":"button"}]}],
          "layouts":[
            {
              "id":"frame",
              "type":"box-layout",
              "padding":{"top":"0px","right":"0px","bottom":"0px","left":"0px"},
              "children":[
                {
                  "id":"grid",
                  "type":"grid-layout",
                  "row-definitions":["1rate","3rate"],
                  "column-definitions":["1rate","1.5rate"],
                  "cells":[{"row":0,"column":0,"slots":[{"id":"slot1"}]},{"row":0,"column":1,"slots":[{"id":"slot2"}]},{"row":1,"column":0,"slots":[{"id":"slot3"}]}]
                }
              ]
            }
          ],
          "bindings":[
            {"layout":"/frame","model":"screen"},
            {
              "layout":"/frame/grid",
              "parentModel":"screen",
              "childrenModel":[{"model":"a","slot":"slot1"},{"model":"b","slot":"slot2"},{"model":"c","slot":"slot3"}]
            }
          ]
        }
        """;

    public static void Run()
    {
        CellKeysWithoutSlots();
        var settings = StationeryStyleSettings.Parse(Source);
        if (settings.Layouts.Single(l => l.Id == "grid").Path != "/frame/grid" ||
            settings.Layouts.Single(l => l.Id == "grid").ParentPath != "/frame")
            throw new Exception("Layout paths and parent paths must use absolute slash paths.");
        foreach (var invalidPath in new[] { "frame.grid", "frame/grid", "/frame.grid", "/frame/grid/", "/frame//grid", "/frame/./grid", "/frame/../grid", "/", "" })
            Reject(node => node["bindings"]![1]!["layout"] = invalidPath);
        var result = StationeryLayoutEngine.Arrange(settings, 100, 200);
        var legacy = StationeryStyleSettings.Parse(Source.Replace("grid-layout", "floating-layout"));
        if (legacy.Layouts.Single(l => l.Id == "grid").Type != "grid-layout")
            throw new Exception("Legacy grid type must normalize to grid-layout.");
        var legacyBounds = StationeryLayoutEngine.Arrange(legacy, 100, 200).Bounds;
        foreach (var (path, bounds) in result.Bounds) Equal(bounds, legacyBounds[path]);
        Equal(new(0, 0, 40, 50), result.Bounds["/screen/a"]);
        Equal(new(40, 0, 60, 50), result.Bounds["/screen/b"]);
        Equal(new(0, 50, 40, 150), result.Bounds["/screen/c"]);
        Equal(new(80, 0, 120, 100), StationeryLayoutEngine.Arrange(settings, 200, 400).Bounds["/screen/b"]);
        Equal(new(0, 0, 0, 0), StationeryLayoutEngine.Arrange(settings, 0, 0).Bounds["/screen/a"]);

        var padded = Edit(node => node["layouts"]![0]!["padding"] = JsonNode.Parse("""{"top":"10px","right":"20px","bottom":"30px","left":"40px"}"""));
        Equal(new(40, 10, 96, 90), StationeryLayoutEngine.Arrange(padded, 300, 400).Bounds["/screen/a"]);
        Equal(new(20, 10, 0, 0), StationeryLayoutEngine.Arrange(padded, 20, 20).Bounds["/screen/a"]);

        var gridPadding = Edit(node => node["layouts"]![0]!["children"]![0]!["padding"] =
            JsonNode.Parse("""{"left":"10px","top":"20px"}"""));
        Equal(new(10, 20, 36, 45), StationeryLayoutEngine.Arrange(gridPadding, 100, 200).Bounds["/screen/a"]);
        // Independent root layouts on one model are invalid, even box + grid.
        Reject(node =>
        {
            var grid = node["layouts"]![0]!["children"]![0]!.DeepClone();
            node["layouts"]![0]!.AsObject().Remove("children");
            node["layouts"]!.AsArray().Add(grid);
            node["bindings"]![1]!["layout"] = "/grid";
        });

        Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![0]!["row"] = 2);
        Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![1]!["column"] = 0);
        Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![1]!["slots"]![0]!["id"] = "slot1");
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["slot"] = "missing");
        Reject(node => node["bindings"]![1]!["childrenModel"]![1]!["slot"] = "slot1");
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!.AsObject().Remove("slot"));
        foreach (var field in new[] { "row", "col", "rowspan", "colspan", "dock", "size", "margin", "padding", "model" })
            Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![0]!["slots"]![0]![field] = "forbidden");
        foreach (var field in new[] { "margin", "padding", "id" })
            Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![0]![field] = "forbidden");
        Reject(node => node["layouts"]![0]!["children"]![0]!["cells"]![0]!["slots"]!.AsArray().Add(new JsonObject { ["id"] = "alias" }));
        Reject(node => node["layouts"]![0]!["children"]![0]!["slots"] = new JsonArray());
        Reject(node => node["bindings"]![1]!["cells"] = new JsonArray());
        if (typeof(StationeryLayoutSlot).GetProperties().Any(p => p.Name != "Id")) throw new Exception("Slots must only hold an Id.");
        var reordered = Edit(node => node["bindings"]![1]!["childrenModel"] = new JsonArray(
            node["bindings"]![1]!["childrenModel"]!.AsArray().Reverse().Select(c => c!.DeepClone()).ToArray()));
        Equal(result.Bounds["/screen/b"], StationeryLayoutEngine.Arrange(reordered, 100, 200).Bounds["/screen/b"]);
        var unbound = JsonNode.Parse(Source)!;
        unbound["bindings"]![1]!["childrenModel"] = new JsonArray();
        var emptySlotPlan = StationeryUI.StyleDesigner.StyleBlueprint.Parse(unbound.ToJsonString());
        var beforeResize = emptySlotPlan.BuildJson();
        try { emptySlotPlan.Resize(1, 1); throw new Exception("Unbound slots outside resized grid accepted."); }
        catch (ArgumentException) { }
        if (emptySlotPlan.BuildJson() != beforeResize) throw new Exception("Rejected resize mutated the design.");
        var slotPlan = StationeryUI.StyleDesigner.StyleBlueprint.Parse(Source);
        slotPlan.RenameId(["layouts", "0", "children", "0", "cells", "0", "slots", "0"], "firstCell");
        var renamed = JsonNode.Parse(slotPlan.BuildJson())!;
        if ((string?)renamed["bindings"]![1]!["childrenModel"]![0]!["slot"] != "firstCell") throw new Exception("Slot rename must update binding references.");
        try { slotPlan.DeleteNode(["layouts", "0", "children", "0", "cells", "0", "slots", "0"]); throw new Exception("Referenced slot deleted."); }
        catch (JsonException) { }

        var marginSettings = Edit(node =>
        {
            var grid = node["layouts"]![0]!["children"]![0]!;
            grid["margin"] = JsonNode.Parse("""{"left":"10px","top":"20px","right":"10px","bottom":"20px"}""");
            grid["padding"] = JsonNode.Parse("""{"left":"5px","top":"5px","right":"5px","bottom":"5px"}""");
            AddElementMargin(node, """{"left":"2px","top":"3px","right":"4px","bottom":"5px"}""");
        });
        var marginResult = StationeryLayoutEngine.Arrange(marginSettings, 100, 200);
        Equal(new(17, 28, 22, 29.5), marginResult.Bounds["/screen/a"]);
        Equal(new(43, 25, 42, 37.5), marginResult.Bounds["/screen/b"]);
        foreach (var bad in new[] { "-1px", "1rate", "NaNpx" })
            Reject(node => node["layouts"]![0]!["children"]![0]!["margin"] = new JsonObject { ["left"] = bad });
        var collapsedMargin = Edit(node => AddElementMargin(node, """{"left":"999px"}"""));
        Equal(new(40, 0, 0, 50), StationeryLayoutEngine.Arrange(collapsedMargin, 100, 200).Bounds["/screen/a"]);

        var fixedRows = Edit(node => node["layouts"]![0]!["children"]![0]!["row-definitions"] = JsonNode.Parse("""["100px","1rate"]"""));
        Equal(new(0, 100, 40, 300), StationeryLayoutEngine.Arrange(fixedRows, 100, 400).Bounds["/screen/c"]);
        var overflow = Edit(node => node["layouts"]![0]!["children"]![0]!["column-definitions"] = JsonNode.Parse("""["200px","100px"]"""));
        Equal(new(0, 0, 100, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/a"]);
        Equal(new(100, 0, 50, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/b"]);
        var collapsed = Edit(node => node["layouts"]![0]!["children"]![0]!["column-definitions"] = JsonNode.Parse("""["0rate","1rate"]"""));
        Equal(new(0, 0, 0, 50), StationeryLayoutEngine.Arrange(collapsed, 100, 200).Bounds["/screen/a"]);
        Equal(new(0, 0, 100, 50), StationeryLayoutEngine.Arrange(collapsed, 100, 200).Bounds["/screen/b"]);
        var huge = "1" + new string('0', 300) + "rate";
        var largeRates = Edit(node => node["layouts"]![0]!["children"]![0]!["column-definitions"] = new JsonArray(huge, huge));
        Equal(new(50, 0, 50, 50), StationeryLayoutEngine.Arrange(largeRates, 100, 200).Bounds["/screen/b"]);

        // Declaration order is unrelated to application order.
        var reversed = Edit(node =>
        {
            var layouts = node["layouts"]!.AsArray(); var first = layouts[0]!; layouts.RemoveAt(0); layouts.Add(first);
            var bindings = node["bindings"]!.AsArray(); first = bindings[0]!; bindings.RemoveAt(0); bindings.Add(first);
        });
        Equal(result.Bounds["/screen/c"], StationeryLayoutEngine.Arrange(reversed, 100, 200).Bounds["/screen/c"]);

        foreach (var bad in new[] { "-1rate", "NaNrate", "Infinityrate", "1em", "", "1.2.3rate", "1Rate" })
            Reject(node => node["layouts"]![0]!["children"]![0]!["column-definitions"]![0] = bad);
        Reject(node => node["layouts"]![0]!["children"]![0]!["column-definitions"]![0] = 1);
        Reject(node => node["layouts"]![0]!["children"]![0]!["row-definitions"] = new JsonArray());
        Reject(node => node["layouts"]![0]!["children"]![0]!["row-definitions"] = new JsonArray("0rate", "0px"));
        Reject(node => node["layouts"]![0]!["children"]![0]!["padding"] = JsonNode.Parse("""{"left":"-1px"}"""));
        Reject(node => node["layouts"]![0]!["children"]![0]!["id"] = "frame");
        Reject(node => node["bindings"]![1]!["layout"] = "/unknown");
        Reject(node => node["bindings"]![1]!["parentModel"] = "unknown");
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["model"] = "unknown");
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["model"] = "/screen");
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["row"] = -1);
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["row"] = 2);
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["column"] = 2);
        Reject(node => node["bindings"]![1]!["childrenModel"]![0]!["row"] = .5);
        Reject(node => node["bindings"]![1]!["childrenModel"]![1]!["column"] = 0);
        Reject(node => node["bindings"]![1]!["childrenModel"]![1]!["model"] = "a");
        Reject(node => node["bindings"]!.AsArray().Add(node["bindings"]![0]!.DeepClone()));
        Reject(node => node["bindings"]!.AsArray().Add(node["bindings"]![1]!.DeepClone()));
        Reject(node => node.AsObject().Remove("bindings"));

        var nested = StationeryStyleSettings.Parse("""
        {
          "models":[
            {
              "id":"screen",
              "type":"viewport",
              "children":[
                {"id":"left","type":"container","children":[{"id":"a","type":"button"}]},
                {"id":"right","type":"container","children":[{"id":"a","type":"button"}]}
              ]
            }
          ],
          "layouts":[
            {
              "id":"pair",
              "type":"grid-layout",
              "row-definitions":["1rate"],
              "column-definitions":["1rate","1rate"],
              "cells":[{"row":0,"column":0,"slots":[{"id":"slot1"}]},{"row":0,"column":1,"slots":[{"id":"slot2"}]}]
            },
            {
              "id":"unit",
              "type":"grid-layout",
              "row-definitions":["1rate"],
              "column-definitions":["1rate"],
              "cells":[{"row":0,"column":0,"slots":[{"id":"slot1"}]}]
            }
          ],
          "bindings":[
            {"layout":"/unit","parentModel":"/screen/right","childrenModel":[{"model":"a","slot":"slot1"}]},
            {"layout":"/pair","parentModel":"screen","childrenModel":[{"model":"left","slot":"slot1"},{"model":"right","slot":"slot2"}]},
            {"layout":"/unit","parentModel":"/screen/left","childrenModel":[{"model":"/screen/left/a","slot":"slot1"}]}
          ]
        }
        """);
        var reused = StationeryLayoutEngine.Arrange(nested, 100, 200);
        Equal(new(0, 0, 50, 200), reused.Bounds["/screen/left/a"]);
        Equal(new(50, 0, 50, 200), reused.Bounds["/screen/right/a"]);
    }

    private static void CellKeysWithoutSlots()
    {
        const string source = """
        {"models":[{"id":"screen","type":"viewport","children":[{"id":"a","type":"button"},{"id":"b","type":"button"},{"id":"dockParent","type":"container","children":[{"id":"c","type":"button"},{"id":"d","type":"button"}]}]}],
         "layouts":[{"id":"grid","type":"grid-layout","row-definitions":["1rate"],"column-definitions":["1rate","1rate"],
                     "cells":[{"row":0,"col":0},{"row":0,"col":1}]},
                    {"id":"dock","type":"dock-layout","cells":[{"dock":"top","size":"10px"},{"dock":"top","size":"20px"}] }],
         "bindings":[{"layout":"/grid","parentModel":"screen","childrenModel":[
             {"model":"a","cell":{"row":1,"col":1}},{"model":"b","cell":{"row":1,"col":2}}]},
                     {"layout":"/dock","parentModel":"screen/dockParent","childrenModel":[
             {"model":"c","cell":{"dock":"top","index":1}},{"model":"d","cell":{"dock":"top","index":2}}]}]}
        """;
        var settings = StationeryStyleSettings.Parse(source);
        Require(settings.Bindings[0].Children[0].Row == 0 && settings.Bindings[0].Children[1].Column == 1,
            "grid cell key uses one-based external coordinates");
        Require(settings.Bindings[1].DockChildren[1].CellIndex == 1, "dock cell key counts repeated directions");
        RejectText(source.Replace("\"index\":2", "\"index\":3"));
        RejectText(source.Replace("\"row\":1,\"col\":1", "\"row\":0,\"col\":1"));
    }

    private static void AddElementMargin(JsonNode node, string margin)
    {
        node["layouts"]!.AsArray().Add(JsonNode.Parse("""{"id":"elementMargin","type":"box-layout","padding":{"top":"0px","right":"0px","bottom":"0px","left":"0px"}}"""));
        node["layouts"]!.AsArray().Last()!["margin"] = JsonNode.Parse(margin);
        node["bindings"]!.AsArray().Add(JsonNode.Parse("""{"layout":"/elementMargin","model":"screen/a"}"""));
    }

    private static StationeryStyleSettings Edit(Action<JsonNode> edit)
    {
        var node = JsonNode.Parse(Source)!; edit(node); return StationeryStyleSettings.Parse(node.ToJsonString());
    }
    private static void Reject(Action<JsonNode> edit)
    {
        try { Edit(edit); } catch (JsonException) { return; }
        throw new Exception("Expected invalid grid-layout or binding to be rejected.");
    }
    private static void RejectText(string source)
    {
        try { StationeryStyleSettings.Parse(source); } catch (JsonException) { return; }
        throw new Exception("Expected invalid cell key to be rejected.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Equal(ScreenRectangle expected, ScreenRectangle actual)
    {
        if (Math.Abs(expected.X - actual.X) > .00001 || Math.Abs(expected.Y - actual.Y) > .00001 ||
            Math.Abs(expected.Width - actual.Width) > .00001 || Math.Abs(expected.Height - actual.Height) > .00001 ||
            !double.IsFinite(actual.X + actual.Y + actual.Width + actual.Height))
            throw new Exception($"Expected {expected}, got {actual}");
    }
}
