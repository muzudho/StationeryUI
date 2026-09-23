# layouts セクションのボックス・レイアウト

余白を設定したいコントロールに紐づけてください。  
コントロールというのは例えば、ボタンやテキストボックスなどです。  


## JSON 設定例

```json
{
    "layouts": [
        {
            "id": "spanCellBox",
            "type": "box-layout",
            "padding": { "top": "0px", "right": "0px", "bottom": "0px", "left": "0px" },
            "margin": {
                "top": "16px",
                "right": "16px",
                "bottom": "16px",
                "left": "16px"
            }
        }
    ]
}
```

## 解説

* `id` - レイアウトを一意に識別するパスの一部分（ノード名）です。
* `"type": "box-layout"` - ボックス・レイアウトであることを示します。
* `padding` - レイアウトの内側の余白です。
* `margin` - レイアウトの外側の余白です。
