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
      "padding": {
        "top": "0px",
        "right": "0px",
        "bottom": "0px",
        "left": "0px"
      }
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
    "ctrlNameField": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/1y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlNameField"
    },
    "ctrlMemoField": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/2y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlMemoField"
    },
    "ctrlThemeButton": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/3y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlThemeButton"
    },
    "ctrlScaleButton": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/3y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlScaleButton"
    },
    "ctrlApplyTitleButton": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/4y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlApplyTitleButton"
    },
    "ctrlOpenDialogButton": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/5y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlOpenDialogButton"
    },
    "ctrlSampleTree": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/1y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlSampleTree"
    },
    "ctrlSplitPaneDemoLink": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/5y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlSplitPaneDemoLink"
    },
    "ctrlLayoutDemoLink": {
      "layoutPath": "root:tabbedPages/0:pageDock/center.2:topDemoLayout/4y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlTopDemoPage/mdlBody/mdlLayoutDemoLink"
    },
    "ctrlVerticalSplit": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/2y.1x.1w.1h:verticalSplitLayout",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlVerticalSplit"
    },
    "ctrlHorizontalSplit": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/3y.1x.1w.1h:horizontalSplitLayout",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlHorizontalSplit"
    },
    "ctrlLeftPane": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/2y.1x.1w.1h:verticalSplitLayout/first",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlVerticalSplit/mdlLeftPane"
    },
    "ctrlRightPane": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/2y.1x.1w.1h:verticalSplitLayout/second",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlVerticalSplit/mdlRightPane"
    },
    "ctrlTopPane": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/3y.1x.1w.1h:horizontalSplitLayout/first",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlHorizontalSplit/mdlTopPane"
    },
    "ctrlBottomPane": {
      "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/3y.1x.1w.1h:horizontalSplitLayout/second",
      "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlHorizontalSplit/mdlBottomPane"
    },
    "ctrlTitle": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/1y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlTitle"
    },
    "ctrlBoxTitle": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/2y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlBoxTitle"
    },
    "ctrlBoxContent": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.1x.1w.1h:box",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlBoxContent"
    },
    "ctrlGridTitle": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/2y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlGridTitle"
    },
    "ctrlSpanCell": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/1y.1x.2w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlSpanCell"
    },
    "ctrlGridFooter": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/3y.1x.1w.3h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlGridFooter"
    },
    "ctrlNestedA": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/1y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlNestedA"
    },
    "ctrlNestedB": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/1y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlNestedB"
    },
    "ctrlNestedC": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/2y.1x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlNestedC"
    },
    "ctrlNestedD": {
      "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/3y.2x.1w.1h:grid/1y.2x.2w.2h:nestedGrid/2y.2x.1w.1h",
      "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlNestedD"
    },
    "ctrlTopDemoLink": {
      "lytSplitPaneDemoPage": {
        "layoutPath": "root:tabbedPages/1:pageDockFullscreen/center.2:splitDemoLayout/1y.1x.1w.1h",
        "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlBody/mdlTopDemoLink"
      },
      "lytLayoutDemoPage": {
        "layoutPath": "root:tabbedPages/2:pageDock/center.2:layoutShowcase/1y.1x.1w.1h",
        "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlBody/mdlTopDemoLink"
      }
    },
    "ctrlToolHint": {
      "lytTopDemoPage": {
        "layoutPath": "root:tabbedPages/0:pageDock/bottom.1:inspectorContents/1y.1x.1w.1h",
        "modelPath": "/mdlDemo/mdlTopDemoPage/mdlInspectorPanel/mdlToolHint"
      },
      "lytSplitPaneDemoPage": {
        "layoutPath": "root:tabbedPages/1:pageDockFullscreen/bottom.1:inspectorContents/1y.1x.1w.1h",
        "modelPath": "/mdlDemo/mdlSplitPaneDemoPage/mdlInspectorPanel/mdlToolHint"
      },
      "lytLayoutDemoPage": {
        "layoutPath": "root:tabbedPages/2:pageDock/bottom.1:inspectorContents/1y.1x.1w.1h",
        "modelPath": "/mdlDemo/mdlLayoutDemoPage/mdlInspectorPanel/mdlToolHint"
      }
    }
  }
}
""");

    public static DemoModelBinding Create(StationeryStyleSettings settings)
    {
        var root = settings.Models[0].CreateTree();
        var all = Descendants(root).ToArray();
        var topPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "topDemoPage" && node.Kind == "page")
            ?? throw new JsonException("topDemoPage is required.");
        var splitPage = root.Children.SingleOrDefault(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "splitPaneDemoPage" && node.Kind == "page")
            ?? throw new JsonException("splitPaneDemoPage is required.");
        var layoutPage = root.Children.Single(node => StationeryControlHandle.ModelRoleFromId(node.Id) == "layoutDemoPage" && node.Kind == "page");
        foreach (var page in new[] { topPage, splitPage, layoutPage })
            if (!settings.BindingsV2.Values.Any(binding =>
                (binding.ModelPath is { } modelPath && modelPath.Contains("/" + page.Id + "/mdlInspectorPanel/", StringComparison.Ordinal)) ||
                binding.ModelPaths.Values.Any(modelPath => modelPath.Contains("/" + page.Id + "/mdlInspectorPanel/", StringComparison.Ordinal))))
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
            binding.ModelPaths.Values.Any(path => root.Resolve(path)?.IsWithin(dialog) == true)))
            throw new JsonException("The demo dialog currently uses its code-defined layout; bind the main controls only.");
        foreach (var id in new[] { "verticalSplit", "horizontalSplit" })
        {
            var node = splitControls[id];
            var expected = id == "verticalSplit" ? new[] { "leftPane", "rightPane" } : new[] { "topPane", "bottomPane" };
            var first = settings.BindingsV2.Values.Any(binding => binding.ModelPath == splitControls[expected[0]].Path &&
                binding.LayoutPath?.EndsWith("/first", StringComparison.Ordinal) == true);
            var second = settings.BindingsV2.Values.Any(binding => binding.ModelPath == splitControls[expected[1]].Path &&
                binding.LayoutPath?.EndsWith("/second", StringComparison.Ordinal) == true);
            var hasSplitLayout = settings.BindingsV2.Values.Any(binding => binding.ModelPath == node.Path &&
                binding.LayoutPath is { } route && route.Contains(":" + id + "Layout", StringComparison.Ordinal));
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

