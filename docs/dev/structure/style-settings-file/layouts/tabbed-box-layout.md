# layouts セクションのタブド・ボックス・レイアウト

余白を設定したいコントロールに紐づけてください。  
コントロールというのは例えば、ボタンやテキストボックスなどです。  


## プロパティ

* `SelectedTabIndex` - 実行時に使う int 型のプロパティ。現在選択されているタブのインデックスを示します。0 始まりで、初期値は `0` です。設定 JSON には保存しません。


## JSON 設定例

```json
{
    "layouts": [
        {
            "id": "viewPort",
            "type": "tabbed-box-layout",
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
* `"type": "tabbed-box-layout"` - タブド・ボックス・レイアウトであることを示します。
* `padding` - レイアウトの内側の余白です。
* `margin` - レイアウトの外側の余白です。

## 子要素の配置指定

TabbedBoxLayout はルートレイアウトとして使います。`bindings.childrenModel` にページを表示順で並べます。配列のインデックスは 0 始まりで、ページのモデル名は `model` に指定します。

```json
{
    "layout": "/viewPort",
    "parentModel": "app",
    "childrenModel": [
        { "model": "homePage" },
        { "model": "settingsPage" }
    ]
}
```

一度に領域が割り当てられるページは１つです。`SelectedTabIndex` を変更すると、そのページへ切り替わります。選択中のページには TabbedBoxLayout の内側領域全体を割り当て、ほかのページには０サイズを割り当てます。ページ自身のレイアウトとコントロール状態は保持されます。

現在の `viewports` / `modelTree` 形式では、ページ内の `Container` にも TabbedBoxLayout を適用し、その直下の `Container` を切り替えられます。デモは `viewportsSnapshot` に初期選択、リンクの `onClick` に `selectView` の対象領域と子を記述します。[部分切替の設定例](../../../../user/partial-navigation.md)を参照してください。
