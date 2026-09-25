using StationeryUI.Inspection;
using StationeryUI.Styling;
using System.Text.Json;

/// <summary>Connects stable code-defined roles to models nodes. Layouts wrappers never change these roles.</summary>
internal sealed record DemoModelBinding(StationeryNode Root, StationeryNode TopPage, StationeryNode SplitPage, StationeryNode LayoutPage, StationeryNode Dialog,
    IReadOnlyDictionary<string, StationeryNode> LayoutControls,
    IReadOnlyDictionary<string, StationeryNode> SplitControls,
    IReadOnlyDictionary<string, StationeryNode> Main, IReadOnlyDictionary<string, StationeryNode> DialogControls, string Signature)
{
    public static StationeryStyleSettings Fallback { get; } = StationeryStyleSettings.Parse("""
{
  "models": [
    {
      "id": "mdlDemo",
      "type": "viewport",
      "children": [
        {
          "id": "mdlTopDemoPage",
          "type": "page",
          "children": [
            {
              "id": "mdlBody",
              "type": "container",
              "children": [
                {
                  "id": "mdlNameField",
                  "type": "textBox"
                },
                {
                  "id": "mdlMemoField",
                  "type": "textBox"
                },
                {
                  "id": "mdlThemeButton",
                  "type": "button"
                },
                {
                  "id": "mdlScaleButton",
                  "type": "button"
                },
                {
                  "id": "mdlApplyTitleButton",
                  "type": "button"
                },
                {
                  "id": "mdlOpenDialogButton",
                  "type": "button"
                },
                {
                  "id": "mdlSampleTree",
                  "type": "tree"
                },
                {
                  "id": "mdlEditDialog",
                  "type": "dialog",
                  "children": [
                    {
                      "id": "mdlNameField",
                      "type": "textBox"
                    },
                    {
                      "id": "mdlCancelButton",
                      "type": "button"
                    },
                    {
                      "id": "mdlSaveButton",
                      "type": "button"
                    }
                  ]
                },
                {
                  "id": "mdlSplitPaneDemoLink",
                  "type": "link"
                },
                {
                  "id": "mdlLayoutDemoLink",
                  "type": "link"
                }
              ]
            },
            {
              "id": "mdlInspectorPanel",
              "type": "container",
              "children": [
                {
                  "id": "mdlToolHint",
                  "type": "textBlock"
                }
              ]
            }
          ]
        },
        {
          "id": "mdlSplitPaneDemoPage",
          "type": "page",
          "children": [
            {
              "id": "mdlBody",
              "type": "container",
              "children": [
                {
                  "id": "mdlTopDemoLink",
                  "type": "link"
                },
                {
                  "id": "mdlVerticalSplit",
                  "type": "splitPane",
                  "children": [
                    {
                      "id": "mdlLeftPane",
                      "type": "textBox"
                    },
                    {
                      "id": "mdlRightPane",
                      "type": "textBox"
                    }
                  ]
                },
                {
                  "id": "mdlHorizontalSplit",
                  "type": "splitPane",
                  "children": [
                    {
                      "id": "mdlTopPane",
                      "type": "textBox"
                    },
                    {
                      "id": "mdlBottomPane",
                      "type": "textBox"
                    }
                  ]
                }
              ]
            },
            {
              "id": "mdlInspectorPanel",
              "type": "container",
              "children": [
                {
                  "id": "mdlToolHint",
                  "type": "textBlock"
                }
              ]
            }
          ]
        },
        {
          "id": "mdlLayoutDemoPage",
          "type": "page",
          "children": [
            {
              "id": "mdlBody",
              "type": "container",
              "children": [
                {
                  "id": "mdlTopDemoLink",
                  "type": "link",
                  "margin": {
                    "top": "4px",
                    "right": "4px",
                    "bottom": "4px",
                    "left": "4px"
                  }
                },
                {
                  "id": "mdlTitle",
                  "type": "textBlock"
                },
                {
                  "id": "mdlBoxTitle",
                  "type": "textBlock"
                },
                {
                  "id": "mdlBoxContent",
                  "type": "textBlock"
                },
                {
                  "id": "mdlGridTitle",
                  "type": "textBlock"
                },
                {
                  "id": "mdlSpanCell",
                  "type": "textBlock",
                  "margin": {
                    "top": "16px",
                    "right": "16px",
                    "bottom": "16px",
                    "left": "16px"
                  }
                },
                {
                  "id": "mdlNestedA",
                  "type": "textBlock"
                },
                {
                  "id": "mdlNestedB",
                  "type": "textBlock"
                },
                {
                  "id": "mdlNestedC",
                  "type": "textBlock"
                },
                {
                  "id": "mdlNestedD",
                  "type": "textBlock"
                },
                {
                  "id": "mdlGridFooter",
                  "type": "textBlock"
                }
              ]
            },
            {
              "id": "mdlInspectorPanel",
              "type": "container",
              "children": [
                {
                  "id": "mdlToolHint",
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
      "id": "tabbedPages",
      "type": "tabbed-box-layout",
      "padding": { "top": "0px", "right": "0px", "bottom": "0px", "left": "0px" }
    },
    {
      "id": "topDemoLayout",
      "type": "grid-layout",
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
      ],
      "padding": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "margin": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "cells": [
        {
          "row": 0,
          "col": 0
        },
        {
          "row": 1,
          "col": 0
        },
        {
          "row": 2,
          "col": 0
        },
        {
          "row": 2,
          "col": 1
        },
        {
          "row": 3,
          "col": 0
        },
        {
          "row": 4,
          "col": 0
        },
        {
          "row": 0,
          "col": 1
        },
        {
          "row": 4,
          "col": 1
        },
        {
          "row": 3,
          "col": 1
        }
      ]
    },
    {
      "id": "splitDemoLayout",
      "type": "grid-layout",
      "row-definitions": [
        "64px",
        "1rate",
        "1rate"
      ],
      "column-definitions": [
        "1rate"
      ],
      "padding": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "margin": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "cells": [
        {
          "row": 0,
          "col": 0
        },
        {
          "row": 1,
          "col": 0
        },
        {
          "row": 2,
          "col": 0
        }
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
      "id": "pageDock",
      "type": "dock-layout",
      "cells": [
        {
          "dock": "bottom",
          "size": "80px"
        },
        {
          "dock": "center",
          "size": "remaining"
        }
      ]
    },
    {
      "id": "inspectorContents",
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
          "col": 0
        }
      ]
    },
    {
      "id": "layoutShowcase",
      "type": "grid-layout",
      "row-definitions": [
        "60px",
        "52px",
        "1rate"
      ],
      "column-definitions": [
        "1rate",
        "2rate"
      ],
      "children": [
        {
          "id": "grid",
          "type": "grid-layout",
          "row-definitions": [
            "1rate",
            "1rate",
            "1rate"
          ],
          "column-definitions": [
            "1rate",
            "1rate",
            "1rate"
          ],
          "row": 2,
          "col": 1,
          "children": [
            {
              "id": "nestedGrid",
              "type": "grid-layout",
              "row-definitions": [
                "1rate",
                "1rate"
              ],
              "column-definitions": [
                "1rate",
                "1rate"
              ],
              "row": 0,
              "col": 1,
              "rowspan": 2,
              "colspan": 2,
              "cells": [
                {
                  "row": 0,
                  "col": 0
                },
                {
                  "row": 0,
                  "col": 1
                },
                {
                  "row": 1,
                  "col": 0
                },
                {
                  "row": 1,
                  "col": 1
                }
              ]
            }
          ],
          "cells": [
            {
              "row": 0,
              "col": 0,
              "rowspan": 2
            },
            {
              "row": 2,
              "col": 0,
              "colspan": 3
            }
          ]
        }
      ],
      "padding": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "margin": {
        "top": "4px",
        "right": "4px",
        "bottom": "4px",
        "left": "4px"
      },
      "cells": [
        {
          "row": 0,
          "col": 0
        },
        {
          "row": 0,
          "col": 1
        },
        {
          "row": 1,
          "col": 0
        },
        {
          "row": 1,
          "col": 1
        },
        {
          "row": 2,
          "col": 0
        }
      ]
    },
    {
      "id": "box",
      "type": "box-layout",
      "padding": {
        "top": "24px",
        "right": "24px",
        "bottom": "24px",
        "left": "24px"
      },
      "margin": {
        "top": "6px",
        "right": "6px",
        "bottom": "6px",
        "left": "6px"
      },
      "border": {
        "top": "2px",
        "right": "2px",
        "bottom": "2px",
        "left": "2px"
      }
    },
    {
      "id": "pageDockFullscreen",
      "type": "dock-layout",
      "cells": [
        {
          "dock": "bottom",
          "size": "0px"
        },
        {
          "dock": "center",
          "size": "remaining"
        }
      ]
    }
  ],
  "bindingsV2": {
    "ctrlNameField": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/1y.1x.1w.1h:mdlNameField",
    "ctrlMemoField": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/2y.1x.1w.1h:mdlMemoField",
    "ctrlThemeButton": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/3y.1x.1w.1h:mdlThemeButton",
    "ctrlScaleButton": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/3y.2x.1w.1h:mdlScaleButton",
    "ctrlApplyTitleButton": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/4y.1x.1w.1h:mdlApplyTitleButton",
    "ctrlOpenDialogButton": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/5y.1x.1w.1h:mdlOpenDialogButton",
    "ctrlSampleTree": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/1y.2x.1w.1h:mdlSampleTree",
    "ctrlSplitPaneDemoLink": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/5y.2x.1w.1h:mdlSplitPaneDemoLink",
    "ctrlLayoutDemoLink": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/center.2:mdlBody@topDemoLayout/4y.2x.1w.1h:mdlLayoutDemoLink",
    "ctrlVerticalSplit": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/2y.1x.1w.1h:mdlVerticalSplit@verticalSplitLayout",
    "ctrlHorizontalSplit": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/3y.1x.1w.1h:mdlHorizontalSplit@horizontalSplitLayout",
    "ctrlLeftPane": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/2y.1x.1w.1h:mdlVerticalSplit@verticalSplitLayout/first:mdlLeftPane",
    "ctrlRightPane": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/2y.1x.1w.1h:mdlVerticalSplit@verticalSplitLayout/second:mdlRightPane",
    "ctrlTopPane": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/3y.1x.1w.1h:mdlHorizontalSplit@horizontalSplitLayout/first:mdlTopPane",
    "ctrlBottomPane": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/3y.1x.1w.1h:mdlHorizontalSplit@horizontalSplitLayout/second:mdlBottomPane",
    "ctrlTitle": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/1y.2x.1w.1h:mdlTitle",
    "ctrlBoxTitle": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/2y.1x.1w.1h:mdlBoxTitle",
    "ctrlBoxContent": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.1x.1w.1h:mdlBoxContent@box",
    "ctrlGridTitle": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/2y.2x.1w.1h:mdlGridTitle",
    "ctrlSpanCell": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/1y.1x.2w.1h:mdlSpanCell",
    "ctrlGridFooter": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/3y.1x.1w.3h:mdlGridFooter",
    "ctrlNestedA": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/1y.1x.1w.1h:mdlNestedA",
    "ctrlNestedB": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/1y.2x.1w.1h:mdlNestedB",
    "ctrlNestedC": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/2y.1x.1w.1h:mdlNestedC",
    "ctrlNestedD": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/2y.2x.1w.1h:mdlNestedD",
    "ctrlTopDemoLink": {
      "lytSplitPaneDemoPage": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/center.2:mdlBody@splitDemoLayout/1y.1x.1w.1h:mdlTopDemoLink",
      "lytLayoutDemoPage": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/center.2:mdlBody@layoutShowcase/1y.1x.1w.1h:mdlTopDemoLink"
    },
    "ctrlToolHint": {
      "lytTopDemoPage": "root:mdlDemo@tabbedPages/0:mdlTopDemoPage@pageDock/bottom.1:mdlInspectorPanel@inspectorContents/1y.1x.1w.1h:mdlToolHint",
      "lytSplitPaneDemoPage": "root:mdlDemo@tabbedPages/1:mdlSplitPaneDemoPage@pageDockFullscreen/bottom.1:mdlInspectorPanel@inspectorContents/1y.1x.1w.1h:mdlToolHint",
      "lytLayoutDemoPage": "root:mdlDemo@tabbedPages/2:mdlLayoutDemoPage@pageDock/bottom.1:mdlInspectorPanel@inspectorContents/1y.1x.1w.1h:mdlToolHint"
    }
  }
}
""");

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.Models[0].CreateTree();
        static IEnumerable<string> Routes(StationeryControlBindingV2 binding)
            => binding.LayoutPath is { } route ? [route] : binding.LayoutPaths.Values;
        var all = Descendants(root).ToArray();
        var topPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "topDemoPage" && node.Kind == "page")
            ?? throw new JsonException("topDemoPage is required.");
        var splitPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "splitPaneDemoPage" && node.Kind == "page")
            ?? throw new JsonException("splitPaneDemoPage is required.");
        var layoutPage = root.Children.Single(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "layoutDemoPage" && node.Kind == "page");
        foreach (var page in new[] { topPage, splitPage, layoutPage })
            if (!settings.BindingsV2.Values.Any(binding => Routes(binding).Any(route =>
                route.Contains("/bottom", StringComparison.Ordinal) && route.Contains(":mdlInspectorPanel@", StringComparison.Ordinal) &&
                route.Contains(":" + page.Id + "@", StringComparison.Ordinal))))
                throw new JsonException($"{page.Path} requires a page layout bound to inspectorPanel.");
        var dialogs = all.Where(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "editDialog" && node.Kind == "dialog").ToArray();
        if (dialogs.Length != 1) throw new JsonException("Demo models requires one editDialog of type dialog.");
        var dialog = dialogs[0];
        if (!dialog.IsWithin(topPage)) throw new JsonException("editDialog must be in topDemoPage.");
        var main = Bind(all.Where(node => node.IsWithin(topPage) && !node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["nameField"] = "textBox", ["memoField"] = "textBox", ["themeButton"] = "button",
            ["scaleButton"] = "button", ["applyTitleButton"] = "button", ["openDialogButton"] = "button", ["sampleTree"] = "tree", ["splitPaneDemoLink"] = "link", ["layoutDemoLink"] = "link"
        });
        var splitControls = Bind(Descendants(splitPage), new Dictionary<string, string>
        {
            ["toolHint"] = "textBlock", ["topDemoLink"] = "link", ["verticalSplit"] = "splitPane", ["horizontalSplit"] = "splitPane",
            ["leftPane"] = "textBox", ["rightPane"] = "textBox", ["topPane"] = "textBox", ["bottomPane"] = "textBox"
        });
        var layoutControls = Bind(Descendants(layoutPage), new Dictionary<string, string>
        {
            ["topDemoLink"] = "link", ["toolHint"] = "textBlock",
            ["title"] = "textBlock",
            ["boxTitle"] = "textBlock",
            ["boxContent"] = "textBlock",
            ["gridTitle"] = "textBlock",
            ["spanCell"] = "textBlock",
            ["nestedA"] = "textBlock",
            ["nestedB"] = "textBlock",
            ["nestedC"] = "textBlock",
            ["nestedD"] = "textBlock",
            ["gridFooter"] = "textBlock"
        });
        var dialogControls = Bind(all.Where(node => node.IsWithin(dialog)), new Dictionary<string, string>
        {
            ["nameField"] = "textBox", ["cancelButton"] = "button", ["saveButton"] = "button"
        });
        foreach (var node in main.Values.Concat(splitControls.Values).Concat(layoutControls.Values))
        {
            var handle = StationeryControlHandle.FromModelId(node.Id);
            if (!settings.BindingsV2.TryGetValue(handle, out var controlBinding))
                throw new JsonException($"Demo control {node.Path} requires a bindingsV2 entry for '{handle}'.");
            var resolvedPaths = controlBinding.LayoutPath is not null
                ? new[] { controlBinding.ModelPath }
                : controlBinding.ModelPaths.Values.ToArray();
            if (!resolvedPaths.Contains(node.Path, StringComparer.Ordinal))
                throw new JsonException($"bindingsV2 entry '{handle}' does not resolve to demo control {node.Path}.");
        }
        if (settings.BindingsV2.Values.Any(binding =>
            (binding.ModelPath is { } modelPath && root.Resolve(modelPath)?.IsWithin(dialog) == true) ||
            binding.ModelPaths.Values.Any(path => root.Resolve(path)?.IsWithin(dialog) == true) ||
            Routes(binding).Any(route => route.Contains(":" + dialog.Id + "@", StringComparison.Ordinal))))
            throw new JsonException("The demo dialog currently uses its code-defined layout; bind the main controls only.");
        foreach (var id in new[] { "verticalSplit", "horizontalSplit" })
        {
            var node = splitControls[id];
            var expected = id == "verticalSplit" ? new[] { "leftPane", "rightPane" } : new[] { "topPane", "bottomPane" };
            var first = settings.BindingsV2.Values.Any(binding => binding.ModelPath == splitControls[expected[0]].Path &&
                binding.LayoutPath?.EndsWith("/first:mdl" + char.ToUpperInvariant(expected[0][0]) + expected[0][1..], StringComparison.Ordinal) == true);
            var second = settings.BindingsV2.Values.Any(binding => binding.ModelPath == splitControls[expected[1]].Path &&
                binding.LayoutPath?.EndsWith("/second:mdl" + char.ToUpperInvariant(expected[1][0]) + expected[1][1..], StringComparison.Ordinal) == true);
            var hasSplitLayout = settings.BindingsV2.Values.Any(binding => binding.ModelPath == node.Path &&
                binding.LayoutPath is { } route && route.Contains("@" + id + "Layout", StringComparison.Ordinal));
            if (!hasSplitLayout || !first || !second)
                throw new JsonException("Split content must match the demo roles.");
        }
        var bound = main.Values.Concat(dialogControls.Values).Concat(splitControls.Values).Concat(layoutControls.Values).ToHashSet();
        foreach (var node in all)
        {
            if (bound.Contains(node))
            {
                if (node.Kind != "splitPane" && node.Children.Count != 0) throw new JsonException($"Control {node.Path} cannot own children in this demo.");
            }
            else if (node != root && node != dialog && node.Kind is not ("page" or "container"))
                throw new JsonException($"The demo has no code binding for {node.Path} ({node.Kind}).");
        }
        return new(root, topPage, splitPage, layoutPage, dialog, layoutControls, splitControls, main, dialogControls, string.Join('\n', all.Select(node => node.Path + ":" + node.Kind)));
    }

    private static IReadOnlyDictionary<string, StationeryNode> Bind(IEnumerable<StationeryNode> nodes, Dictionary<string, string> roles)
    {
        var all = nodes.ToArray();
        var result = new Dictionary<string, StationeryNode>(StringComparer.Ordinal);
        foreach (var (id, kind) in roles)
        {
            var matches = all.Where(node => StationeryControlHandle.ModelRoleFromId(node.Id) == id).ToArray();
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
