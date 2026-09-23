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
* `cells` - XXX: 仕様確認中。子要素をぶら下げるときに使う？ 使っていない分まで書く必要があるか？

