# グリッド・レイアウト

子要素をグリッド状に配置したいコンテナーに紐づけてください。  
コンテナーというのは例えば、ページやダイアログボックスなど。  


## JSON 設定例

```json
{
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
        }
    ]
}
```

## 解説

* `id` - レイアウトを一意に識別するパスの一部分（ノード名）です。
* `"type": "grid-layout"` - グリッド・レイアウトであることを示します。
* `row-definitions` - グリッドの各行の高さを指定します。
    * `rate` - 比率です。実数で指定できます。
* `column-definitions` - グリッドの各列の幅を指定します。
* `padding` - レイアウトの内側の余白です。
* `margin` - レイアウトの外側の余白です。
* `cells` - セルの配置枠です。`row`・`col` は layouts と bindings の両方で 0 始まりです。モデルとの紐づけは `bindings.childrenModel` で指定します。


## 子要素の配置指定

左上を原点 (0, 0) として、行番号と列番号を指定します。  

* `col` - 列番号を指定します。左から 0, 1, 2, ... と数えます。
* `row` - 行番号を指定します。上から 0, 1, 2, ... と数えます。
* `colspan` - 子要素が占める列数を指定します。右へ延びます。デフォルトは 1 です。
* `rowspan` - 子要素が占める行数を指定します。下へ延びます。デフォルトは 1 です。
