# ドック・レイアウト

こちらも参照：  
📖 [ドック・レイアウト（詳細）](./dock-layout-detail.md)  


## JSON 設定例

```json
{
    "layouts": [
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
        }
    ]
}
```

## 解説

* `id` - レイアウトを一意に識別するパスの一部分（ノード名）です。
* `"type": "dock-layout"` - ドック・レイアウトであることを示します。
* `cells` - 子要素をぶら下げる領域を設定します。
    * `dock` - 子要素をぶら下げる位置を指定します。  
        * `top` - 上にぶら下げます。
        * `bottom` - 下にぶら下げます。
        * `left` - 左にぶら下げます。
        * `right` - 右にぶら下げます。
        * `center` - 残りの領域を使います。
    * `size` - 子要素のサイズを指定します。
        * `px` - ピクセル単位で指定します。
        * `remaining` - 残りの領域を使います。


