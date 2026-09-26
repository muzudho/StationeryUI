# ページ内の部分切替

デモのトップページで［リンク：ページ内の部分切替］を押すと実例を開けます。左側の［概要を表示］と［詳細を表示］は、右側の `vContentSwitcher` だけを切り替えます。ページの見出し・戻るリンク・下部の説明は残ります。

文房具UIファイルの `viewports` では、切替領域に `TabbedBoxLayout` を付け、その直下に表示候補を並べます。候補には 0 始まりの `place` を付けます。部分切替の候補はページ全体でなくてもよく、このデモでは `Container` です。対応する `modelTree` にも同じ親子構造を用意し、表示する部品の `modelPath` を設定します。

```json
{
    "type": "Container",
    "name": "vContentSwitcher",
    "layout": { "name": "csPartialContentTabs", "type": "TabbedBoxLayout" },
    "children": [
        { "type": "Container", "name": "vSummaryPane", "place": 0 },
        { "type": "Container", "name": "vDetailsPane", "place": 1 }
    ]
}
```

リンクには切替領域の絶対パスと、その直下で表示する子の名前を指定します。デモの C# コードはリンクのクリック時にこの `onClick` を読み、対象の `TabbedBoxLayout` の選択を変更します。

```json
{
    "type": "Link",
    "name": "vDetailsLink",
    "onClick": {
        "action": "selectView",
        "target": "/vMainViewport/vPartialDemoPage/vBody/vContentSwitcher",
        "child": "vDetailsPane"
    }
}
```

`viewportsSnapshot` は起動時の選択を領域ごとに記録します。表示していないページ内の領域も指定できます。現在の `initialViewportSnapshot` はページ全体の初期選択にも引き続き使えます。

```json
"viewportsSnapshot": [
    {
        "target": "/vMainViewport/vPartialDemoPage/vBody/vContentSwitcher",
        "child": "vSummaryPane"
    }
]
```

実行中に切り替えた選択は、ページを離れて戻ってもメモリー上で保持されます。実行中の変更を文房具UIファイルへ自動保存する機能はありません。初期表示を変える場合は `viewportsSnapshot` の `child` を編集してください。JSON に `onClick` を書いただけでアプリのリンクが自動生成されるわけではなく、アプリ側でリンクとクリック処理を接続します。
