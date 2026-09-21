using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class FloatingLayoutTests
{
    private const string Source = """
        {
          "models":[{"id":"screen","type":"viewport","children":[
            {"id":"a","type":"button"},{"id":"b","type":"button"},{"id":"c","type":"button"}
          ]}],
          "layouts":[
            {"id":"frame","type":"panel","padding":{"top":"0px","right":"0px","bottom":"0px","left":"0px"}},
            {"id":"grid","type":"floating-layout","row-definitions":["1rate","3rate"],"column-definitions":["1rate","1.5rate"]}
          ],
          "bindings":[
            {"layout":"frame","model":"screen"},
            {"layout":"grid","parentModel":"screen","childrenModel":[
              {"model":"a","row":0,"column":0},{"model":"b","row":0,"column":1},{"model":"c","row":1,"column":0}
            ]}
          ]
        }
        """;

    public static void Run()
    {
        var settings = StationeryStyleSettings.Parse(Source);
        var result = StationeryLayoutEngine.Arrange(settings, 100, 200);
        Equal(new(0, 0, 40, 50), result.Bounds["/screen/a"]);
        Equal(new(40, 0, 60, 50), result.Bounds["/screen/b"]);
        Equal(new(0, 50, 40, 150), result.Bounds["/screen/c"]);
        Equal(new(80, 0, 120, 100), StationeryLayoutEngine.Arrange(settings, 200, 400).Bounds["/screen/b"]);
        Equal(new(0, 0, 0, 0), StationeryLayoutEngine.Arrange(settings, 0, 0).Bounds["/screen/a"]);

        var padded = Edit(node => node["layouts"]![0]!["padding"] = JsonNode.Parse("""{"top":"10px","right":"20px","bottom":"30px","left":"40px"}"""));
        Equal(new(40, 10, 96, 90), StationeryLayoutEngine.Arrange(padded, 300, 400).Bounds["/screen/a"]);
        Equal(new(20, 10, 0, 0), StationeryLayoutEngine.Arrange(padded, 20, 20).Bounds["/screen/a"]);

        var fixedRows = Edit(node => node["layouts"]![1]!["row-definitions"] = JsonNode.Parse("""["100px","1rate"]"""));
        Equal(new(0, 100, 40, 300), StationeryLayoutEngine.Arrange(fixedRows, 100, 400).Bounds["/screen/c"]);
        var overflow = Edit(node => node["layouts"]![1]!["column-definitions"] = JsonNode.Parse("""["200px","100px"]"""));
        Equal(new(0, 0, 100, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/a"]);
        Equal(new(100, 0, 50, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/b"]);
        var collapsed = Edit(node => node["layouts"]![1]!["column-definitions"] = JsonNode.Parse("""["0rate","1rate"]"""));
        Equal(new(0, 0, 0, 50), StationeryLayoutEngine.Arrange(collapsed, 100, 200).Bounds["/screen/a"]);
        Equal(new(0, 0, 100, 50), StationeryLayoutEngine.Arrange(collapsed, 100, 200).Bounds["/screen/b"]);
        var huge = "1" + new string('0', 300) + "rate";
        var largeRates = Edit(node => node["layouts"]![1]!["column-definitions"] = new JsonArray(huge, huge));
        Equal(new(50, 0, 50, 50), StationeryLayoutEngine.Arrange(largeRates, 100, 200).Bounds["/screen/b"]);

        // Declaration order is unrelated to application order.
        var reversed = Edit(node =>
        {
            var layouts = node["layouts"]!.AsArray(); var first = layouts[0]!; layouts.RemoveAt(0); layouts.Add(first);
            var bindings = node["bindings"]!.AsArray(); first = bindings[0]!; bindings.RemoveAt(0); bindings.Add(first);
        });
        Equal(result.Bounds["/screen/c"], StationeryLayoutEngine.Arrange(reversed, 100, 200).Bounds["/screen/c"]);

        foreach (var bad in new[] { "-1rate", "NaNrate", "Infinityrate", "1em", "", "1.2.3rate", "1Rate" })
            Reject(node => node["layouts"]![1]!["column-definitions"]![0] = bad);
        Reject(node => node["layouts"]![1]!["column-definitions"]![0] = 1);
        Reject(node => node["layouts"]![1]!["row-definitions"] = new JsonArray());
        Reject(node => node["layouts"]![1]!["row-definitions"] = new JsonArray("0rate", "0px"));
        Reject(node => node["layouts"]![1]!["padding"] = new JsonObject());
        Reject(node => node["layouts"]![1]!["id"] = "frame");
        Reject(node => node["bindings"]![1]!["layout"] = "unknown");
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
            {"models":[{"id":"screen","type":"viewport","children":[
              {"id":"left","type":"container","children":[{"id":"a","type":"button"}]},
              {"id":"right","type":"container","children":[{"id":"a","type":"button"}]}
            ]}],"layouts":[
              {"id":"pair","type":"floating-layout","row-definitions":["1rate"],"column-definitions":["1rate","1rate"]},
              {"id":"unit","type":"floating-layout","row-definitions":["1rate"],"column-definitions":["1rate"]}
            ],"bindings":[
              {"layout":"unit","parentModel":"/screen/right","childrenModel":[{"model":"a","row":0,"column":0}]},
              {"layout":"pair","parentModel":"screen","childrenModel":[{"model":"left","row":0,"column":0},{"model":"right","row":0,"column":1}]},
              {"layout":"unit","parentModel":"/screen/left","childrenModel":[{"model":"/screen/left/a","row":0,"column":0}]}
            ]}
            """);
        var reused = StationeryLayoutEngine.Arrange(nested, 100, 200);
        Equal(new(0, 0, 50, 200), reused.Bounds["/screen/left/a"]);
        Equal(new(50, 0, 50, 200), reused.Bounds["/screen/right/a"]);
    }

    private static StationeryStyleSettings Edit(Action<JsonNode> edit)
    {
        var node = JsonNode.Parse(Source)!; edit(node); return StationeryStyleSettings.Parse(node.ToJsonString());
    }
    private static void Reject(Action<JsonNode> edit)
    {
        try { Edit(edit); } catch (JsonException) { return; }
        throw new Exception("Expected invalid floating-layout or binding to be rejected.");
    }
    private static void Equal(ScreenRectangle expected, ScreenRectangle actual)
    {
        if (Math.Abs(expected.X - actual.X) > .00001 || Math.Abs(expected.Y - actual.Y) > .00001 ||
            Math.Abs(expected.Width - actual.Width) > .00001 || Math.Abs(expected.Height - actual.Height) > .00001 ||
            !double.IsFinite(actual.X + actual.Y + actual.Width + actual.Height))
            throw new Exception($"Expected {expected}, got {actual}");
    }
}
