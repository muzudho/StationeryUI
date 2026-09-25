# Control tree and model tree

The current style file uses three top-level sections:

- `controlTree`: control handles and their context-specific model and layout paths.
- `modelTree`: one viewport model root and its owned model instances.
- `layouts`: reusable layout definitions.

The former `models`, `bindings`, and `bindingsV2` sections are not accepted. Existing style files must be rewritten to this format.

## Model tree

`modelTree` is one model node, not an array:

```json
"modelTree": {
    "id": "mdlViewPort",
    "type": "viewport",
    "children": []
}
```

Children express model ownership and identity. A page may own separate model instances for its controls. Presentation grouping belongs in the control and layout trees when it does not represent model ownership.

## Control tree entries

Each top-level property is a control handle. `layoutPath` and `modelPath` are maps keyed by the same `in /...` absolute control context:

```json
"controlTree": {
    "ctrlSaveButton": {
        "layoutPath": {
            "in /ctrlViewPort/ctrlDemoPage": "tabbedPages[0].pageDock[center_2].mainGrid[2y_1x_1w_1h]",
            "in /ctrlViewPort/ctrlSplitPaneDemoPage": "tabbedPages[1].pageDock[center_2].splitGrid[3y_1x_1w_1h]"
        },
        "modelPath": {
            "in /ctrlViewPort/ctrlDemoPage": "/mdlViewPort/mdlDemoPage/mdlSaveButton",
            "in /ctrlViewPort/ctrlSplitPaneDemoPage": "/mdlViewPort/mdlSplitPaneDemoPage/mdlSaveButton"
        }
    }
}
```

The context after `in` starts with `/` and names the absolute control path where that variant is used. The same context key pairs its layout and model paths. A single-context control still uses maps with one entry.

Layout paths use dot syntax. A cell address is written in brackets, with `_` separating its parts: `grid[2y_1x_1w_1h]`. Model paths remain absolute slash-separated paths such as `/mdlViewPort/mdlDemoPage/mdlSaveButton`.
