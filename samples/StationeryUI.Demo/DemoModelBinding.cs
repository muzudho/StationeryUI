using StationeryUI.Inspection;
using StationeryUI.Styling;
using System.Text.Json;

/// <summary>Connects stable code-defined roles to models nodes. Layouts wrappers never change these roles.</summary>
internal sealed record DemoModelBinding(StationeryNode Root, StationeryNode TopPage, StationeryNode SplitPage, StationeryNode Dialog,
    IReadOnlyDictionary<string, StationeryNode> SplitControls,
    IReadOnlyDictionary<string, StationeryNode> Main, IReadOnlyDictionary<string, StationeryNode> DialogControls, string Signature)
{
    public static StationeryStyleSettings Fallback { get; } = StationeryStyleSettings.Parse("""
{
  "models": [
    {
      "id": "demo",
      "type": "viewport",
      "children": [
        {
          "id": "topDemoPage",
          "type": "page",
          "children": [
            {
              "id": "nameField",
              "type": "textBox"
            },
            {
              "id": "memoField",
              "type": "textBox"
            },
            {
              "id": "themeButton",
              "type": "button"
            },
            {
              "id": "scaleButton",
              "type": "button"
            },
            {
              "id": "applyTitleButton",
              "type": "button"
            },
            {
              "id": "openDialogButton",
              "type": "button"
            },
            {
              "id": "sampleTree",
              "type": "tree"
            },
            {
              "id": "editDialog",
              "type": "dialog",
              "children": [
                {
                  "id": "nameField",
                  "type": "textBox"
                },
                {
                  "id": "cancelButton",
                  "type": "button"
                },
                {
                  "id": "saveButton",
                  "type": "button"
                }
              ]
            },
            {
              "id": "splitPaneDemoLink",
              "type": "link"
            },
            {
              "id": "inspectorPanel",
              "type": "container",
              "children": [
                {
                  "id": "toolHint",
                  "type": "textBlock"
                }
              ]
            }
          ]
        },
        {
          "id": "splitPaneDemoPage",
          "type": "page",
          "children": [
            {
              "id": "topDemoLink",
              "type": "link"
            },
            {
              "id": "verticalSplit",
              "type": "splitPane",
              "children": [
                {
                  "id": "leftPane",
                  "type": "textBox"
                },
                {
                  "id": "rightPane",
                  "type": "textBox"
                }
              ]
            },
            {
              "id": "horizontalSplit",
              "type": "splitPane",
              "children": [
                {
                  "id": "topPane",
                  "type": "textBox"
                },
                {
                  "id": "bottomPane",
                  "type": "textBox"
                }
              ]
            },
            {
              "id": "inspectorPanel",
              "type": "container",
              "children": [
                {
                  "id": "toolHint",
                  "type": "textBlock"
                }
              ]
            }
          ]
        }
      ]
    }
  ],
  "layouts": [
    {
      "id": "demoViewport",
      "type": "panel",
      "padding": {
        "top": "0px",
        "right": "0px",
        "bottom": "0px",
        "left": "0px"
      }
    },
    {
      "id": "topDemoLayout",
      "type": "floating-layout",
      "row-definitions": [
        "1rate",
        "1rate",
        "1rate",
        "1rate",
        "1rate"
      ],
      "column-definitions": [
        "1rate",
        "1rate"
      ]
    },
    {
      "id": "splitDemoLayout",
      "type": "floating-layout",
      "row-definitions": [
        "64px",
        "1rate",
        "1rate"
      ],
      "column-definitions": [
        "1rate"
      ]
    },
    {
      "id": "verticalSplitLayout",
      "type": "split-pane",
      "orientation": "vertical",
      "ratio": 0.5,
      "dividerWidth": "10px",
      "minimumPaneSize": "80px"
    },
    {
      "id": "horizontalSplitLayout",
      "type": "split-pane",
      "orientation": "horizontal",
      "ratio": 0.5,
      "dividerWidth": "10px",
      "minimumPaneSize": "60px"
    },
    {
      "id": "fullscreenLayout",
      "type": "fullscreen-layout"
    },
    {
      "id": "workPageLayout",
      "type": "work-page-layout",
      "inspectorHeight": "80px"
    },
    {
      "id": "pagePadding",
      "type": "panel",
      "padding": {
        "top": "8px",
        "right": "8px",
        "bottom": "8px",
        "left": "8px"
      }
    },
    {
      "id": "inspectorContents",
      "type": "floating-layout",
      "row-definitions": [
        "1rate"
      ],
      "column-definitions": [
        "1rate"
      ]
    }
  ],
  "bindings": [
    {
      "layout": "demoViewport",
      "model": "demo"
    },
    {
      "layout": "topDemoLayout",
      "parentModel": "demo/topDemoPage",
      "childrenModel": [
        {
          "model": "nameField",
          "row": 0,
          "column": 0
        },
        {
          "model": "memoField",
          "row": 1,
          "column": 0
        },
        {
          "model": "themeButton",
          "row": 2,
          "column": 0
        },
        {
          "model": "scaleButton",
          "row": 2,
          "column": 1
        },
        {
          "model": "applyTitleButton",
          "row": 3,
          "column": 0
        },
        {
          "model": "openDialogButton",
          "row": 4,
          "column": 0
        },
        {
          "model": "sampleTree",
          "row": 0,
          "column": 1
        },
        {
          "model": "splitPaneDemoLink",
          "row": 4,
          "column": 1
        }
      ]
    },
    {
      "layout": "splitDemoLayout",
      "parentModel": "demo/splitPaneDemoPage",
      "childrenModel": [
        {
          "model": "topDemoLink",
          "row": 0,
          "column": 0
        },
        {
          "model": "verticalSplit",
          "row": 1,
          "column": 0
        },
        {
          "model": "horizontalSplit",
          "row": 2,
          "column": 0
        }
      ]
    },
    {
      "layout": "verticalSplitLayout",
      "model": "demo/splitPaneDemoPage/verticalSplit",
      "firstModel": "leftPane",
      "secondModel": "rightPane"
    },
    {
      "layout": "horizontalSplitLayout",
      "model": "demo/splitPaneDemoPage/horizontalSplit",
      "firstModel": "topPane",
      "secondModel": "bottomPane"
    },
    {
      "layout": "workPageLayout",
      "model": "demo/topDemoPage",
      "inspectorModel": "inspectorPanel"
    },
    {
      "layout": "pagePadding",
      "model": "demo/topDemoPage"
    },
    {
      "layout": "inspectorContents",
      "parentModel": "demo/topDemoPage/inspectorPanel",
      "childrenModel": [
        {
          "model": "toolHint",
          "row": 0,
          "column": 0
        }
      ]
    },
    {
      "layout": "fullscreenLayout",
      "model": "demo/splitPaneDemoPage",
      "inspectorModel": "inspectorPanel"
    },
    {
      "layout": "pagePadding",
      "model": "demo/splitPaneDemoPage"
    },
    {
      "layout": "inspectorContents",
      "parentModel": "demo/splitPaneDemoPage/inspectorPanel",
      "childrenModel": [
        {
          "model": "toolHint",
          "row": 0,
          "column": 0
        }
      ]
    }
  ]
}
""");

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.Models[0].CreateTree();
        var all = Descendants(root).ToArray();
        var topPage = root.Children.SingleOrDefault(node => node.Id == "topDemoPage" && node.Kind == "page")
            ?? throw new JsonException("topDemoPage is required.");
        var splitPage = root.Children.SingleOrDefault(node => node.Id == "splitPaneDemoPage" && node.Kind == "page")
            ?? throw new JsonException("splitPaneDemoPage is required.");
        foreach (var page in new[] { topPage, splitPage })
            if (!settings.Bindings.Any(b => b.ModelPath == page.Path && b.InspectorModel == page.Path + "/inspectorPanel"))
                throw new JsonException($"{page.Path} requires a page layout bound to inspectorPanel.");
        var dialogs = all.Where(node => node.Id == "editDialog" && node.Kind == "dialog").ToArray();
        if (dialogs.Length != 1) throw new JsonException("Demo models requires one editDialog of type dialog.");
        var dialog = dialogs[0];
        if (!dialog.IsWithin(topPage)) throw new JsonException("editDialog must be in topDemoPage.");
        var main = Bind(all.Where(node => node.IsWithin(topPage) && !node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["nameField"] = "textBox", ["memoField"] = "textBox", ["themeButton"] = "button",
            ["scaleButton"] = "button", ["applyTitleButton"] = "button", ["openDialogButton"] = "button", ["sampleTree"] = "tree", ["splitPaneDemoLink"] = "link"
        });
        var splitControls = Bind(Descendants(splitPage), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["topDemoLink"] = "link", ["verticalSplit"] = "splitPane", ["horizontalSplit"] = "splitPane",
            ["leftPane"] = "textBox", ["rightPane"] = "textBox", ["topPane"] = "textBox", ["bottomPane"] = "textBox"
        });
        var dialogControls = Bind(all.Where(node => node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["cancelButton"] = "button", ["saveButton"] = "button"
        });
        var placed = settings.Bindings.SelectMany(binding => binding.Children).Select(child => child.ModelPath).ToHashSet(StringComparer.Ordinal);
        foreach (var binding in settings.Bindings.Where(binding => binding.FirstModel is not null))
        {
            placed.Add(binding.FirstModel!); placed.Add(binding.SecondModel!);
        }
        foreach (var node in main.Values.Concat(splitControls.Values))
            if (!placed.Contains(node.Path)) throw new JsonException($"Demo control {node.Path} needs a floating-layout cell binding.");
        if (settings.Bindings.Any(binding => root.Resolve(binding.ModelPath)!.IsWithin(dialog)) ||
            placed.Any(path => root.Resolve(path)!.IsWithin(dialog)))
            throw new JsonException("The demo dialog currently uses its code-defined layout; bind the main controls only.");
        foreach (var id in new[] { "verticalSplit", "horizontalSplit" })
        {
            var node = splitControls[id];
            var splitBinding = settings.Bindings.SingleOrDefault(binding => binding.ModelPath == node.Path && binding.FirstModel is not null)
                ?? throw new JsonException($"{id} requires a split-pane binding.");
            var expected = id == "verticalSplit" ? new[] { "leftPane", "rightPane" } : new[] { "topPane", "bottomPane" };
            if (splitBinding.FirstModel != splitControls[expected[0]].Path || splitBinding.SecondModel != splitControls[expected[1]].Path)
                throw new JsonException("Split content must match the demo roles.");
        }
        var bound = main.Values.Concat(dialogControls.Values).Concat(splitControls.Values).ToHashSet();
        foreach (var node in all)
        {
            if (bound.Contains(node))
            {
                if (node.Kind != "splitPane" && node.Children.Count != 0) throw new JsonException($"Control {node.Path} cannot own children in this demo.");
            }
            else if (node != root && node != dialog && node.Kind is not ("page" or "container"))
                throw new JsonException($"The demo has no code binding for {node.Path} ({node.Kind}).");
        }
        return new(root, topPage, splitPage, dialog, splitControls, main, dialogControls, string.Join('\n', all.Select(node => node.Path + ":" + node.Kind)));
    }

    private static IReadOnlyDictionary<string, StationeryNode> Bind(IEnumerable<StationeryNode> nodes, Dictionary<string, string> roles)
    {
        var all = nodes.ToArray();
        var result = new Dictionary<string, StationeryNode>(StringComparer.Ordinal);
        foreach (var (id, kind) in roles)
        {
            var matches = all.Where(node => node.Id == id).ToArray();
            if (matches.Length != 1 || matches[0].Kind != kind)
                throw new JsonException($"Demo models requires exactly one {id} of type {kind} in its main/dialog scope.");
            result.Add(id, matches[0]);
        }
        return result;
    }

    private static IEnumerable<StationeryNode> Descendants(StationeryNode node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
}
