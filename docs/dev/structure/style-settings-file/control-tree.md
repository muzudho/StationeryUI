# Viewports, styles, and model tree

The demo style file separates three concerns:

- `viewports` describes the visible view and control tree.
- `styles` contains reusable layout definitions referenced by a view node's `style` property.
- `modelTree` describes model ownership and model instances.

## View nodes

Each view node has a `type`, a `name`, optional `style`, and optional `children`:

```json
{
    "type": "Page",
    "name": "vTopDemoPage",
    "style": "csTopDemoGridLayout",
    "children": []
}
```

The `style` property names an entry in the top-level `styles` array. Layout definitions are kept out of the view tree, like CSS rules kept out of HTML.

## Styles

Style entries have a unique `name` and a layout `type`. Their remaining properties define the layout:

```json
{
    "name": "csTopDemoGridLayout",
    "type": "GridLayout",
    "row-definitions": ["1rate", "1rate"],
    "column-definitions": ["1rate", "1rate"],
    "cells": [{ "row": 0, "col": 0 }, { "row": 1, "col": 1 }]
}
```

## Control and model references

Control bindings live on their corresponding view nodes. Each node stores its handle and matching context-keyed `layoutPath` and `modelPath` maps:

```json
{
    "type": "Button",
    "name": "vSaveButton",
    "controlHandle": "ctrlSaveButton",
    "layoutPath": {
        "in /ctrlViewPort/ctrlDemoPage": "csTabbedPages[0].csPageDock[center_2].csMainGrid[2y_1x_1w_1h]"
    },
    "modelPath": {
        "in /ctrlViewPort/ctrlDemoPage": "/mdlViewPort/mdlDemoPage/mdlSaveButton"
    }
}
```

The same handle may appear on separate view nodes for different pages. The parser combines those nodes into one runtime binding while preserving each context. `modelTree` remains independent so model ownership and lifecycle do not have to match the visual tree.
