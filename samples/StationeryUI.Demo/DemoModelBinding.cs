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
      "id": "demo",
      "type": "viewport",
      "children": [
        {
          "id": "topDemoPage",
          "type": "page",
          "children": [
            {
              "id": "body",
              "type": "container",
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
                  "id": "layoutDemoLink",
                  "type": "link"
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
        },
        {
          "id": "splitPaneDemoPage",
          "type": "page",
          "children": [
            {
              "id": "body",
              "type": "container",
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
        },
        {
          "id": "layoutDemoPage",
          "type": "page",
          "children": [
            {
              "id": "body",
              "type": "container",
              "children": [
                {
                  "id": "topDemoLink",
                  "type": "link",
                  "margin": {
                    "top": "4px",
                    "right": "4px",
                    "bottom": "4px",
                    "left": "4px"
                  }
                },
                {
                  "id": "title",
                  "type": "textBlock"
                },
                {
                  "id": "boxTitle",
                  "type": "textBlock"
                },
                {
                  "id": "boxContent",
                  "type": "textBlock"
                },
                {
                  "id": "gridTitle",
                  "type": "textBlock"
                },
                {
                  "id": "spanCell",
                  "type": "textBlock"
                },
                {
                  "id": "nestedA",
                  "type": "textBlock"
                },
                {
                  "id": "nestedB",
                  "type": "textBlock"
                },
                {
                  "id": "nestedC",
                  "type": "textBlock"
                },
                {
                  "id": "nestedD",
                  "type": "textBlock"
                },
                {
                  "id": "gridFooter",
                  "type": "textBlock"
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
          },
          "children": [
            {
              "id": "content",
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
            }
          ],
          "row": 2,
          "col": 0
        },
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
        }
      ]
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
  "bindings": [
    {
      "layout": "/topDemoLayout",
      "parentModel": "demo/topDemoPage/body",
      "childrenModel": [
        {
          "model": "nameField",
          "cell": {
            "row": 1,
            "col": 1
          }
        },
        {
          "model": "memoField",
          "cell": {
            "row": 2,
            "col": 1
          }
        },
        {
          "model": "themeButton",
          "cell": {
            "row": 3,
            "col": 1
          }
        },
        {
          "model": "scaleButton",
          "cell": {
            "row": 3,
            "col": 2
          }
        },
        {
          "model": "applyTitleButton",
          "cell": {
            "row": 4,
            "col": 1
          }
        },
        {
          "model": "openDialogButton",
          "cell": {
            "row": 5,
            "col": 1
          }
        },
        {
          "model": "sampleTree",
          "cell": {
            "row": 1,
            "col": 2
          }
        },
        {
          "model": "splitPaneDemoLink",
          "cell": {
            "row": 5,
            "col": 2
          }
        },
        {
          "model": "layoutDemoLink",
          "cell": {
            "row": 4,
            "col": 2
          }
        }
      ]
    },
    {
      "layout": "/splitDemoLayout",
      "parentModel": "demo/splitPaneDemoPage/body",
      "childrenModel": [
        {
          "model": "topDemoLink",
          "cell": {
            "row": 1,
            "col": 1
          }
        },
        {
          "model": "verticalSplit",
          "cell": {
            "row": 2,
            "col": 1
          }
        },
        {
          "model": "horizontalSplit",
          "cell": {
            "row": 3,
            "col": 1
          }
        }
      ]
    },
    {
      "layout": "/verticalSplitLayout",
      "model": "demo/splitPaneDemoPage/body/verticalSplit",
      "firstModel": "leftPane",
      "secondModel": "rightPane"
    },
    {
      "layout": "/horizontalSplitLayout",
      "model": "demo/splitPaneDemoPage/body/horizontalSplit",
      "firstModel": "topPane",
      "secondModel": "bottomPane"
    },
    {
      "layout": "/pageDock",
      "parentModel": "demo/topDemoPage",
      "childrenModel": [
        {
          "model": "inspectorPanel",
          "cell": {
            "dock": "bottom",
            "index": 1
          }
        },
        {
          "model": "body",
          "cell": {
            "dock": "center",
            "index": 1
          }
        }
      ]
    },
    {
      "layout": "/inspectorContents",
      "parentModel": "demo/topDemoPage/inspectorPanel",
      "childrenModel": [
        {
          "model": "toolHint",
          "cell": {
            "row": 1,
            "col": 1
          }
        }
      ]
    },
    {
      "layout": "/pageDockFullscreen",
      "parentModel": "demo/splitPaneDemoPage",
      "childrenModel": [
        {
          "model": "inspectorPanel",
          "cell": {
            "dock": "bottom",
            "index": 1
          }
        },
        {
          "model": "body",
          "cell": {
            "dock": "center",
            "index": 1
          }
        }
      ]
    },
    {
      "layout": "/inspectorContents",
      "parentModel": "demo/splitPaneDemoPage/inspectorPanel",
      "childrenModel": [
        {
          "model": "toolHint",
          "cell": {
            "row": 1,
            "col": 1
          }
        }
      ]
    },
    {
      "layout": "/layoutShowcase",
      "parentModel": "demo/layoutDemoPage/body",
      "childrenModel": [
        {
          "model": "topDemoLink",
          "cell": {
            "row": 1,
            "col": 1
          }
        },
        {
          "model": "title",
          "cell": {
            "row": 1,
            "col": 2
          }
        },
        {
          "model": "boxTitle",
          "cell": {
            "row": 2,
            "col": 1
          }
        },
        {
          "model": "gridTitle",
          "cell": {
            "row": 2,
            "col": 2
          }
        }
      ]
    },
    {
      "layout": "/layoutShowcase/box/content",
      "parentModel": "demo/layoutDemoPage/body",
      "childrenModel": [
        {
          "model": "boxContent",
          "cell": {
            "row": 1,
            "col": 1
          }
        }
      ]
    },
    {
      "layout": "/layoutShowcase/grid",
      "parentModel": "demo/layoutDemoPage/body",
      "childrenModel": [
        {
          "model": "spanCell",
          "cell": {
            "row": 1,
            "col": 1
          }
        },
        {
          "model": "gridFooter",
          "cell": {
            "row": 3,
            "col": 1
          }
        }
      ]
    },
    {
      "layout": "/layoutShowcase/grid/nestedGrid",
      "parentModel": "demo/layoutDemoPage/body",
      "childrenModel": [
        {
          "model": "nestedA",
          "cell": {
            "row": 1,
            "col": 1
          }
        },
        {
          "model": "nestedB",
          "cell": {
            "row": 1,
            "col": 2
          }
        },
        {
          "model": "nestedC",
          "cell": {
            "row": 2,
            "col": 1
          }
        },
        {
          "model": "nestedD",
          "cell": {
            "row": 2,
            "col": 2
          }
        }
      ]
    },
    {
      "layout": "/pageDock",
      "parentModel": "demo/layoutDemoPage",
      "childrenModel": [
        {
          "model": "inspectorPanel",
          "cell": {
            "dock": "bottom",
            "index": 1
          }
        },
        {
          "model": "body",
          "cell": {
            "dock": "center",
            "index": 1
          }
        }
      ]
    },
    {
      "layout": "/inspectorContents",
      "parentModel": "demo/layoutDemoPage/inspectorPanel",
      "childrenModel": [
        {
          "model": "toolHint",
          "cell": {
            "row": 1,
            "col": 1
          }
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
        var layoutPage = root.Children.Single(node => node.Id == "layoutDemoPage" && node.Kind == "page");
        foreach (var page in new[] { topPage, splitPage, layoutPage })
            if (!settings.Bindings.Any(b => b.ModelPath == page.Path && (b.InspectorModel == page.Path + "/inspectorPanel"
                || b.DockChildren.Any(c => c.ModelPath == page.Path + "/inspectorPanel"))))
                throw new JsonException($"{page.Path} requires a page layout bound to inspectorPanel.");
        var dialogs = all.Where(node => node.Id == "editDialog" && node.Kind == "dialog").ToArray();
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
        var placed = settings.Bindings.SelectMany(binding => binding.Children).Select(child => child.ModelPath).ToHashSet(StringComparer.Ordinal);
        placed.UnionWith(settings.Bindings.SelectMany(binding => binding.DockChildren).Select(child => child.ModelPath));
        foreach (var binding in settings.Bindings.Where(binding => binding.FirstModel is not null))
        {
            placed.Add(binding.FirstModel!); placed.Add(binding.SecondModel!);
        }
        foreach (var node in main.Values.Concat(splitControls.Values).Concat(layoutControls.Values))
            if (!placed.Contains(node.Path)) throw new JsonException($"Demo control {node.Path} needs a grid-layout cell binding.");
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
