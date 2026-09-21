# ツリー

デモ画面の右上に、文房具のツリーを表示する。
子を持つノードの名前の左横に、開いているときは［－］、閉じているときは［＋］を表示する。
［－］をクリックすると子孫が隠れ、［＋］をクリックすると再表示する。子を持たないノードにはボタンを表示しない。

名前のクリックは選択だけを行う。開閉ボタンは、押してから同じボタンの上で離したときに動作する。
閉じた枝の中の開閉状態は保持する。選択中の子を隠した場合、選択は閉じた親に移る。

## キーボードとスクロール

- Tab / Shift+Tab：ほかの文房具とツリーの間を移動。
- ↑ / ↓：表示中のノードを選択。
- ←：選択ノードを閉じる。すでに閉じている場合や末端では親へ移動。
- →：選択ノードを開く。すでに開いている場合は最初の子へ移動。
- Enter / Space：選択ノードを開閉。
- Home / End：表示中の先頭／末尾を選択。
- ツリー上のマウスホイール：縦スクロール。

キーボードで移動した選択ノードは、見える位置まで自動スクロールする。
右端のスクロールバーは幅 17px（画面上のピクセル）で、表示倍率を変えても同じ幅を保つ。
つまみをマウスでドラッグすると縦スクロールできる。ドラッグ中にツリーの外へ出ても追従し、
ボタンを離すと終了する。つまみの上下の空き部分をクリックすると、表示領域の高さ分だけ移動する。
全項目が収まる場合はバーを表示しない。
色・文字は StationeryUiHost のテーマに従い、Element.Theme による個別指定も使える。

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
TreeView.SetExpanded / Toggle / Select でも操作できる。SelectedItem が現在の選択。
VisibleRows() は表示対象のノードと深さを返す。描画領域外まで含み、閉じた枝の子孫は含まない。

Id はコードで付ける識別名、Label は画面上のノード名。同じ親の下の Id は一意にする。
別の親の下では同じ Id を使える。Id の許可文字はほかの文房具と同じ英字・数字・アンダースコア。
F12 では /demo/topDemoPage/sampleTree/stationery/writing/pencil のような完全パスで確認できる。
閉じた子やスクロール領域外の子は、F12 で非表示として確認できる。

## スタイルとの関係

デモでは models に {"id":"sampleTree","type":"tree"} を追加し、bindings で row: 0 / column: 1 に配置している。
既存のモデルノードに接続するときは、AddTree の第 1 引数に StationeryNode を渡す。
ツリー全体の配置は floating-layout に従い、ノード行の高さと字下げはツリー側が管理する。
スタイルの再読み込みやモデルパスの変更で、ツリーの選択・開閉状態は失われない。

現時点ではツリー内の項目は C# の TreeView.AddNode で作る。models の tree に children を定義する形式や、
ツリー内部のノードに対するスタイル binding は未対応。F12 のツリー表示とは別の、通常画面に配置できる部品。
