# bindings セクションの ドック・レイアウト

## JSON 設定例（こうしたい）

```json
{
    "bindings": [
        {
            "control": "inspectorPanel"
        }
    ]
}
```

## JSON 設定例（旧バージョン）

```json
{
    "bindings": [
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
        }
    ]
}
```

## 解説

* `layout` - `layouts` セクションの要素をルートからのパスで指定してください。 例： `/pageDockFullscreen`
* `parentModel` - 配置するモデルの親を指定します。 例： `demo/splitPaneDemoPage`
* `childrenModel` - 配置する子モデルの配列を指定します。
    * 各要素
        * `model`
        * `cell`
          * `dock` - くっつける先の四辺のいずれかです。
              * `top` - 上側。複数個可能。
              * `bottom` - 下側。複数個可能。
              * `left` - 左側。複数個可能。
              * `right` - 右側。複数個可能。
              * `center` - 中央。最後の残り１つです。
          * `index` - 1 始まりの順序番号です.

