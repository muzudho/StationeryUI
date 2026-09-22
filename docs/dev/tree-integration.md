# ツリーの組み込み

## C# で作る

```csharp
using StationeryUI.Controls;

var tree = new TreeView();
var stationery = tree.AddNode("stationery", "文房具");
var writing = tree.AddNode("writing", "筆記用具", stationery);
tree.AddNode("pencil", "鉛筆", writing);
var paper = tree.AddNode("paper", "紙製品", stationery, expanded: false);
tree.AddNode("notebook", "ノート", paper);

var element = ui.AddTree("sampleTree", new(500, 8, 400, 200), "文房具のツリー", tree);
```

AddNode は既定で開いた状態を作る。expanded: false で閉じた状態から始められる。
TreeView.SetExpanded / Toggle / Select でも操作できる。SelectedItem が現在の選択、TargetItem が操作対象。SetTarget で操作対象を変更し、ClearSelection / ClearTarget でそれぞれ解除する。
VisibleRows() は表示対象のノードと深さを返す。描画領域外まで含み、閉じた枝の子孫は含まない。

Id はコードで付ける識別名、Label は画面上のノード名。同じ親の下の Id は一意にする。
別の親の下では同じ Id を使える。Id の許可文字はほかの文房具と同じ英字・数字・アンダースコア。
F12 では /demo/topDemoPage/sampleTree/stationery/writing/pencil のような完全パスで確認できる。
閉じた子やスクロール領域外の子は、F12 で非表示として確認できる。

## スタイルとの関係

デモでは models に {"id":"sampleTree","type":"tree"} を追加し、bindings で row: 0 / column: 1 に配置している。
既存のモデルノードに接続するときは、AddTree の第 1 引数に StationeryNode を渡す。
ツリー全体の配置は grid-layout に従い、ノード行の高さと字下げはツリー側が管理する。
スタイルの再読み込みやモデルパスの変更で、ツリーの選択・開閉状態は失われない。

現時点ではツリー内の項目は C# の TreeView.AddNode で作る。models の tree に children を定義する形式や、
ツリー内部のノードに対するスタイル binding は未対応。F12 のツリー表示とは別の、通常画面に配置できる部品。

[開発者向け目次](README.md)
