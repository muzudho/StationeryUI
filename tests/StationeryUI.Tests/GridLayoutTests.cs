using StationeryUI.Canvas;
using StationeryUI.Styling;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class GridLayoutTests
{
    private const string Source = """
        {
          "modelTree": {
            "id": "screen",
            "type": "viewport",
            "children": [
              {
                "id": "a",
                "type": "button"
              },
              {
                "id": "b",
                "type": "button"
              },
              {
                "id": "c",
                "type": "button"
              }
            ]
          },
          "layouts": [
            {
              "id": "frame",
              "type": "box-layout",
              "padding": {
                "top": "0px",
                "right": "0px",
                "bottom": "0px",
                "left": "0px"
              },
              "children": [
                {
                  "id": "grid",
                  "type": "grid-layout",
                  "row-definitions": [
                    "1rate",
                    "3rate"
                  ],
                  "column-definitions": [
                    "1rate",
                    "1.5rate"
                  ],
                  "cells": [
                    {
                      "row": 0,
                      "column": 0,
                      "slots": [
                        {
                          "id": "slot1"
                        }
                      ]
                    },
                    {
                      "row": 0,
                      "column": 1,
                      "slots": [
                        {
                          "id": "slot2"
                        }
                      ]
                    },
                    {
                      "row": 1,
                      "column": 0,
                      "slots": [
                        {
                          "id": "slot3"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ],
          "controlTree": {
            "root": {
              "modelPath": {
                "in /": "/screen"
              },
              "layoutPath": {
                "in /": "frame"
              }
            },
            "a": {
              "modelPath": {
                "in /": "/screen/a"
              },
              "layoutPath": {
                "in /": "frame[single].grid[1y_1x_1w_1h]"
              }
            },
            "b": {
              "modelPath": {
                "in /": "/screen/b"
              },
              "layoutPath": {
                "in /": "frame[single].grid[1y_2x_1w_1h]"
              }
            },
            "c": {
              "modelPath": {
                "in /": "/screen/c"
              },
              "layoutPath": {
                "in /": "frame[single].grid[2y_1x_1w_1h]"
              }
            }
          }
        }
        """;

    public static void Run()
    {
        var settings = StationeryStyleSettings.Parse(Source);
        var result = StationeryLayoutEngine.Arrange(settings, 100, 200);
        Require(settings.Layouts.Single(l => l.Id == "grid").Path == "/frame/grid", "canonical nested path");
        Equal(new(0, 0, 40, 50), result.Bounds["/screen/a"]);
        Equal(new(40, 0, 60, 50), result.Bounds["/screen/b"]);
        Equal(new(0, 50, 40, 150), result.Bounds["/screen/c"]);
        Equal(new(80, 0, 120, 100), StationeryLayoutEngine.Arrange(settings, 200, 400).Bounds["/screen/b"]);
        Equal(new(), StationeryLayoutEngine.Arrange(settings, 0, 0).Bounds["/screen/a"]);
        var legacy = StationeryStyleSettings.Parse(Source.Replace("grid-layout", "floating-layout"));
        Equal(result.Bounds["/screen/b"], StationeryLayoutEngine.Arrange(legacy, 100, 200).Bounds["/screen/b"]);
        var padded = Edit(n => n["layouts"]![0]!["padding"] = JsonNode.Parse("""{"top":"10px","right":"20px","bottom":"30px","left":"40px"}"""));
        Equal(new(40, 10, 96, 90), StationeryLayoutEngine.Arrange(padded, 300, 400).Bounds["/screen/a"]);
        Equal(new(20, 10, 0, 0), StationeryLayoutEngine.Arrange(padded, 20, 20).Bounds["/screen/a"]);
        var gridPadding = Edit(n => Grid(n)["padding"] = JsonNode.Parse("""{"left":"10px","top":"20px"}"""));
        Equal(new(10, 20, 36, 45), StationeryLayoutEngine.Arrange(gridPadding, 100, 200).Bounds["/screen/a"]);
        var margin = Edit(n => {
            Grid(n)["margin"] = JsonNode.Parse("""{"left":"10px","top":"20px","right":"10px","bottom":"20px"}""");
            Grid(n)["padding"] = JsonNode.Parse("""{"left":"5px","top":"5px","right":"5px","bottom":"5px"}""");
            AddElementMargin(n, """{"left":"2px","top":"3px","right":"4px","bottom":"5px"}""");
        });
        Equal(new(17, 28, 22, 29.5), StationeryLayoutEngine.Arrange(margin, 100, 200).Bounds["/screen/a"]);
        Equal(new(43, 25, 42, 37.5), StationeryLayoutEngine.Arrange(margin, 100, 200).Bounds["/screen/b"]);
        var collapsedMargin = Edit(n => AddElementMargin(n, """{"left":"999px"}"""));
        Equal(new(40, 0, 0, 50), StationeryLayoutEngine.Arrange(collapsedMargin, 100, 200).Bounds["/screen/a"]);
        var fixedRows = Edit(n => Grid(n)["row-definitions"] = new JsonArray("100px", "1rate"));
        Equal(new(0, 100, 40, 300), StationeryLayoutEngine.Arrange(fixedRows, 100, 400).Bounds["/screen/c"]);
        var overflow = Edit(n => Grid(n)["column-definitions"] = new JsonArray("200px", "100px"));
        Equal(new(0, 0, 100, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/a"]);
        Equal(new(100, 0, 50, 50), StationeryLayoutEngine.Arrange(overflow, 150, 200).Bounds["/screen/b"]);
        var collapsed = Edit(n => Grid(n)["column-definitions"] = new JsonArray("0rate", "1rate"));
        Equal(new(0, 0, 0, 50), StationeryLayoutEngine.Arrange(collapsed, 100, 200).Bounds["/screen/a"]);
        var huge = "1" + new string('0', 300) + "rate";
        var rates = Edit(n => Grid(n)["column-definitions"] = new JsonArray(huge, huge));
        Equal(new(50, 0, 50, 50), StationeryLayoutEngine.Arrange(rates, 100, 200).Bounds["/screen/b"]);
        var reordered = Edit(n => n["controlTree"] = new JsonObject(n["controlTree"]!.AsObject().Reverse().Select(p => KeyValuePair.Create<string, JsonNode?>(p.Key, p.Value!.DeepClone()))));
        Equal(result.Bounds["/screen/b"], StationeryLayoutEngine.Arrange(reordered, 100, 200).Bounds["/screen/b"]);
        foreach (var bad in new[] { "-1rate", "NaNrate", "Infinityrate", "1em", "", "1.2.3rate", "1Rate" })
            Reject(n => Grid(n)["column-definitions"]![0] = bad);
        foreach (var bad in new[] { "-1px", "1rate", "NaNpx" }) Reject(n => Grid(n)["margin"] = new JsonObject { ["left"] = bad });
        Reject(n => Grid(n)["column-definitions"]![0] = 1);
        Reject(n => Grid(n)["row-definitions"] = new JsonArray());
        Reject(n => Grid(n)["row-definitions"] = new JsonArray("0rate", "0px"));
        Reject(n => Grid(n)["cells"]![0]!["row"] = 2);
        Reject(n => Grid(n)["cells"]![1]!["column"] = 0);
        Reject(n => Grid(n)["cells"]![1]!["slots"]![0]!["id"] = "slot1");
        foreach (var field in new[] { "row", "col", "rowspan", "colspan", "dock", "size", "margin", "padding", "model" })
            Reject(n => Grid(n)["cells"]![0]!["slots"]![0]![field] = "forbidden");
        foreach (var field in new[] { "margin", "padding", "id" }) Reject(n => Grid(n)["cells"]![0]![field] = "forbidden");
        var unnamed = Edit(n => Grid(n)["cells"]![0]!["slots"] = new JsonArray());
        Equal(result.Bounds["/screen/a"], StationeryLayoutEngine.Arrange(unnamed, 100, 200).Bounds["/screen/a"]);
        foreach (var route in new[] { "missing[1y_1x_1w_1h]", "frame[single].missing[1y_1x_1w_1h]", "frame[single].grid[0y_1x_1w_1h]", "frame[single].grid[3y_1x_1w_1h]", "frame[single].grid[1y_3x_1w_1h]" })
            Reject(n => n["controlTree"]!["a"]!["layoutPath"]!["in /"] = route);
        foreach (var edge in new[] { "-1y_1x_1w_1h", "1x_1y_1w_1h", "1y_1x_0w_1h", "99999999999999y_1x_1w_1h", "1y_1x_1w_1hgarbage" })
            Reject(n => n["controlTree"]!["a"]!["layoutPath"]!["in /"] = "frame[single].grid[" + edge + "]");
        Reject(n => n["controlTree"]!["a"]!["modelPath"]!["in /"] = "/missing");
        var unbound = JsonNode.Parse(Source)!;
        unbound["controlTree"] = new JsonObject { ["root"] = StyleTestData.Control("/screen", "frame") };
        var plan = StationeryUI.Editor.StyleBlueprint.Parse(unbound.ToJsonString());
        var before = plan.BuildJson();
        try { plan.Resize(1, 1); throw new Exception("Out-of-bounds unbound cells accepted."); } catch (ArgumentException) { }
        Require(plan.BuildJson() == before, "invalid resize is atomic");
        // Slot names are metadata; current control routes address cells directly.
        var named = StationeryUI.Editor.StyleBlueprint.Parse(Source);
        named.RenameId(["layouts", "0", "children", "0", "cells", "0", "slots", "0"], "firstCell");
        Equal(result.Bounds["/screen/a"], StationeryLayoutEngine.Arrange(StationeryStyleSettings.Parse(named.BuildJson()), 100, 200).Bounds["/screen/a"]);
    }
    private static JsonNode Grid(JsonNode n) => n["layouts"]![0]!["children"]![0]!;

    private static void AddElementMargin(JsonNode node, string margin)
    {
        node["layouts"]!.AsArray().Add(JsonNode.Parse("""{"id":"elementMargin","type":"box-layout","padding":{"top":"0px","right":"0px","bottom":"0px","left":"0px"}}"""));
        node["layouts"]!.AsArray().Last()!["margin"] = JsonNode.Parse(margin);
        node["controlTree"]!["a"]!["layoutPath"]!["in /"] = "frame[single].grid[1y_1x_1w_1h].elementMargin";
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
