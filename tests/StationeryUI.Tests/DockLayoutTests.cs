using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

static class DockLayoutTests
{
    internal const string Source = """
        {
          "modelTree": {
            "id": "app",
            "type": "viewport",
            "children": [
              {
                "id": "header",
                "type": "textBlock"
              },
              {
                "id": "tools",
                "type": "container"
              },
              {
                "id": "side",
                "type": "container"
              },
              {
                "id": "footer",
                "type": "textBlock"
              },
              {
                "id": "body",
                "type": "container",
                "children": [
                  {
                    "id": "button",
                    "type": "button"
                  }
                ]
              }
            ]
          },
          "layouts": [
            {
              "id": "dock",
              "type": "dock-layout",
              "cells": [
                {
                  "dock": "center",
                  "size": "remaining",
                  "slots": [
                    {
                      "id": "slot1"
                    }
                  ]
                },
                {
                  "dock": "top",
                  "size": "40px",
                  "slots": [
                    {
                      "id": "slot2"
                    }
                  ]
                },
                {
                  "dock": "top",
                  "size": "30px",
                  "slots": [
                    {
                      "id": "slot3"
                    }
                  ]
                },
                {
                  "dock": "right",
                  "size": "80px",
                  "slots": [
                    {
                      "id": "slot4"
                    }
                  ]
                },
                {
                  "dock": "bottom",
                  "size": "20px",
                  "slots": [
                    {
                      "id": "slot5"
                    }
                  ]
                }
              ]
            },
            {
              "id": "grid",
              "type": "grid-layout",
              "row-definitions": [
                "1rate"
              ],
              "column-definitions": [
                "1rate"
              ],
              "cells": [
                {
                  "row": 0,
                  "col": 0,
                  "slots": [
                    {
                      "id": "slot1"
                    }
                  ]
                }
              ]
            }
          ],
          "controlTree": {
            "root": {
              "modelPath": {
                "in /": "/app"
              },
              "layoutPath": {
                "in /": "dock"
              }
            },
            "body": {
              "modelPath": {
                "in /": "/app/body"
              },
              "layoutPath": {
                "in /": "dock[center_1].grid"
              }
            },
            "button": {
              "modelPath": {
                "in /": "/app/body/button"
              },
              "layoutPath": {
                "in /": "dock[center_1].grid[1y_1x_1w_1h]"
              }
            },
            "header": {
              "modelPath": {
                "in /": "/app/header"
              },
              "layoutPath": {
                "in /": "dock[top_2]"
              }
            },
            "tools": {
              "modelPath": {
                "in /": "/app/tools"
              },
              "layoutPath": {
                "in /": "dock[top_3]"
              }
            },
            "side": {
              "modelPath": {
                "in /": "/app/side"
              },
              "layoutPath": {
                "in /": "dock[right_4]"
              }
            },
            "footer": {
              "modelPath": {
                "in /": "/app/footer"
              },
              "layoutPath": {
                "in /": "dock[bottom_5]"
              }
            }
          }
        }
        """;
    public static void Run()
    {
        var settings = StationeryStyleSettings.Parse(Source);
        var result = StationeryLayoutEngine.Arrange(settings, 300, 200);
        Equal(new(0, 0, 300, 40), result.Bounds["/app/header"]);
        Equal(new(0, 40, 300, 30), result.Bounds["/app/tools"]);
        Equal(new(220, 70, 80, 130), result.Bounds["/app/side"]);
        Equal(new(0, 180, 220, 20), result.Bounds["/app/footer"]);
        Equal(new(0, 70, 220, 110), result.Bounds["/app/body"]);
        Equal(result.Bounds["/app/body"], result.Bounds["/app/body/button"]);
        var reorder = JsonNode.Parse(Source)!;
        reorder["controlTree"] = new JsonObject(reorder["controlTree"]!.AsObject().Reverse().Select(p => KeyValuePair.Create<string, JsonNode?>(p.Key, p.Value!.DeepClone())));
        Equal(result.Bounds["/app/side"], StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(reorder.ToJsonString()), 300, 200).Bounds["/app/side"]);
        // An empty named slot still reserves space; binding order never controls docking.
        reorder["controlTree"]!.AsObject().Remove("header");
        var withEmptySlot = StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(reorder.ToJsonString()), 300, 200);
        Equal(result.Bounds["/app/body"], withEmptySlot.Bounds["/app/body"]);
        Equal(new(0, 0, 0, 0), withEmptySlot.Bounds["/app/header"]);
        // An unnamed cell still participates in docking; a slot is only its optional name.
        reorder["layouts"]![0]!["cells"]![1]!.AsObject().Remove("slots");
        Equal(result.Bounds["/app/body"], StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(reorder.ToJsonString()), 300, 200).Bounds["/app/body"]);
        foreach (var bad in new[] { "dock[diagonal_2]", "dock[top_99]" })
        {
            var invalid = JsonNode.Parse(Source)!; invalid["controlTree"]!["header"]!["layoutPath"]!["in /"] = bad;
            try { StationeryStyleSettings.Parse(invalid.ToJsonString()); throw new Exception("Invalid dock slot reference accepted."); }
            catch (JsonException) { }
        }
        var paddedJson = JsonNode.Parse(Source)!;
        paddedJson["layouts"]![0]!["padding"] = JsonNode.Parse("""{"left":"10px","top":"20px","right":"30px","bottom":"40px"}""");
        paddedJson["layouts"]![1]!["padding"] = JsonNode.Parse("""{"left":"5px","top":"6px"}""");
        var padded = StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(paddedJson.ToJsonString()), 300, 200);
        Equal(new(10, 20, 260, 140), padded.ContentBounds["/app"]);
        Equal(new(10, 20, 260, 40), padded.Bounds["/app/header"]);
        Equal(new(15, 96, 175, 44), padded.Bounds["/app/body/button"]);
        Equal(padded.ContentBounds["/app/body"], padded.Bounds["/app/body/button"]);
        Equal(padded.ContentBounds["/app"], padded.LayoutContentBounds["/app:/dock"]);
        paddedJson["layouts"]![0]!["margin"] = JsonNode.Parse("""{"left":"5px","top":"7px"}""");
        paddedJson["layouts"]!.AsArray().Add(JsonNode.Parse("""{"id":"headerMargin","type":"box-layout","margin":{"left":"2px","top":"3px","bottom":"4px"},"padding":{"left":"0px","top":"0px","right":"0px","bottom":"0px"}}"""));
        paddedJson["controlTree"]!["header"]!["layoutPath"]!["in /"] = "dock[top_2].headerMargin";
        var marginStyle = StationeryStyleSettings.Parse(paddedJson.ToJsonString());
        var marginLayout = StationeryLayoutEngine.Arrange(marginStyle, 300, 200);
        Equal(new(5, 7, 295, 193), marginLayout.Bounds["/app"]);
        Equal(new(17, 30, 253, 33), marginLayout.Bounds["/app/header"]);
        Equal(new(15, 67, 255, 30), marginLayout.Bounds["/app/tools"]);
        var snapshot = StationeryUI.Inspection.DeveloperInspectionLayout.Apply(
            [new("app", "/app", null, "viewport", "", true, null), new("header", "/app/header", "/app", "textBlock", "", true, null)], marginStyle);
        var roundtrip = JsonSerializer.Deserialize<StationeryUI.Inspection.StationeryInspectionEntry[]>(JsonSerializer.Serialize(snapshot))!;
        Check(roundtrip[0].BoxModel!.Margin.Left == 5 && roundtrip[0].BoxModel!.Padding.Top == 20 && roundtrip[1].BoxModel!.Margin.Top == 3,
            "margin and padding survive inspector transport");
        var reverse = StationeryDockLayout.Arrange(new(10, 20, 100, 100), [new("right", "right", 30), new("top", "top", 40), new("left", "left", 10), new("center", "center")]);
        Equal(new(80, 20, 30, 100), reverse["right"]);
        Equal(new(10, 20, 70, 40), reverse["top"]);
        Equal(new(20, 60, 60, 60), reverse["center"]);
        var many = Enumerable.Range(0, 10).Select(i => new StationeryDockBinding("item" + i, "top", 2)).ToArray();
        Check(StationeryDockLayout.Arrange(new(0, 0, 100, 100), many).Count == 10, "more than five children");
        Check(StationeryDockLayout.Arrange(new(), []).Count == 0, "empty dock");
        var small = StationeryLayoutEngine.Arrange(settings, 10, 5);
        Equal(new(0, 0, 10, 5), small.Bounds["/app/header"]);
        Check(small.Bounds.Values.All(b => b.Width >= 0 && b.Height >= 0), "oversized edges never yield negative dimensions");
        var zero = StationeryLayoutEngine.Arrange(settings, 0, 0);
        Check(zero.Bounds.Values.All(b => b.Width == 0 && b.Height == 0), "zero viewport");
        var empty = settings with { Bindings = [settings.Bindings[0] with { DockChildren = [] }, settings.Bindings[1]] };
        Check(StationeryLayoutEngine.Arrange(empty, 300, 200).Bounds.Where(p => p.Key != "/app").All(p => p.Value.Width == 0 && p.Value.Height == 0), "unbound children do not overlap center");
        var nestedJson = JsonNode.Parse(Source)!;
        nestedJson["layouts"]![0] = JsonNode.Parse("""
        {"id":"frame","type":"box-layout","padding":{"top":"8px","right":"8px","bottom":"8px","left":"8px"},
          "children":[{"id":"dock","type":"dock-layout"}]}
        """);
        nestedJson["layouts"]![0]!["children"]![0]!["cells"] = JsonNode.Parse(Source)!["layouts"]![0]!["cells"]!.DeepClone();
        foreach (var entry in nestedJson["controlTree"]!.AsObject()) { var route = entry.Value!["layoutPath"]!["in /"]!.GetValue<string>(); entry.Value["layoutPath"]!["in /"] = "frame[single]." + route; }
        nestedJson["controlTree"]!["root"]!["layoutPath"]!["in /"] = "frame";
        var nested = StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(nestedJson.ToJsonString()), 300, 200);
        Equal(new(8, 8, 284, 40), nested.Bounds["/app/header"]);
        Check(nested.LayoutBounds.ContainsKey("/app:/frame/dock"), "nested dock gets layout bounds");
        StationeryUI.Inspection.StationeryInspectionEntry[] entries = [new("app", "/app", null, "viewport", "", true, null)];
        var inspected = StationeryUI.Inspection.DeveloperInspectionLayout.Apply(entries, StationeryStyleSettings.Parse(nestedJson.ToJsonString()));
        Check(inspected[0].LayoutTypes!.SequenceEqual(new[] { "box-layout" }), "nested dock reports only its root layout");
        foreach (var invalid in new[] {
            Source.Replace("\"right\"", "\"diagonal\""),
            Source.Replace("40px", "remaining"), Source.Replace("40px", "-1px"), Source.Replace("40px", "1rate"),
            Source.Replace("\"remaining\"", "\"20px\""),
            Source.Replace("80px", "remaining"),
            Source.Replace("/app/header", "/app/body"),
            Source.Replace("/app/header", "/app/missing") })
        {
            try { StationeryStyleSettings.Parse(invalid); throw new Exception("Invalid dock accepted"); }
            catch (JsonException) { }
        }
        var plan = StationeryUI.Editor.StyleBlueprint.Parse(Source);
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].DockChildren.Count == 5, "designer roundtrip preserves dock");
        plan.RenameId(["modelTree", "children", "0"], "heading");
        Check(StationeryStyleSettings.Parse(plan.BuildJson()).Bindings[0].DockChildren.Any(c => c.ModelPath == "/app/heading"), "designer renames dock references");
        var invalidDock = Source.Replace("40px", "invalid");
        var recovered = StationeryStyleSettings.ParseWithDockFallback(invalidDock);
        Check(recovered.Bindings[0].LayoutError!.Contains("size"), "recovery retains diagnostic path");
        var fallback = StationeryLayoutEngine.Arrange(recovered, 300, 400);
        Check(fallback.Errors.Count == 1 && fallback.Errors[0].ModelPath == "/app", "parent receives error header");
        Equal(new(0, 0, 300, 64), fallback.Errors[0].Bounds);
        Equal(new(0, 64, 300, 48), fallback.Bounds["/app/header"]);
        Equal(new(0, 112, 300, 48), fallback.Bounds["/app/tools"]);
        Equal(new(0, 256, 300, 48), fallback.Bounds["/app/body"]);
        Equal(fallback.Bounds["/app/body"], fallback.Bounds["/app/body/button"]);
        Check(StationeryStyleSettings.ParseWithDockFallback(Source).Bindings.All(b => b.LayoutError is null), "correction removes diagnostics");
        TestReload(invalidDock);
    }
    private static void TestReload(string invalidDock)
    {
        var directory = Path.Combine(Path.GetTempPath(), "dock-reload-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            var style = Path.Combine(directory, "style.json");
            var config = Path.Combine(directory, "config.json");
            File.WriteAllText(config, """{"styleFile":"style.json","autoReload":true}""");
            File.WriteAllText(style, Source);
            var file = new StationeryStyleFile(config);
            Check(file.LastError is null, "initial load");
            File.WriteAllText(style, invalidDock); file.Reload();
            Check(file.LastError is not null && file.Current.Bindings[0].LayoutError is not null, "recoverable dock error accepted for display");
            file.Reload(); Check(file.LastError is not null, "unchanged error remains visible");
            var last = file.Current;
            File.WriteAllText(style, "{"); file.Reload();
            Check(ReferenceEquals(last, file.Current) && file.LastError is not null, "broken JSON retains last usable snapshot");
            File.WriteAllText(style, Source); file.Reload();
            Check(file.LastError is null && file.Current.Bindings.All(b => b.LayoutError is null), "fixed JSON restores normal layout");
        }
        finally { Directory.Delete(directory, true); }
    }
    private static void Equal(ScreenRectangle expected, ScreenRectangle actual)
    { if (expected != actual) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
}
