# bindings セクション

## セルキーとモデルの対応

`cells` は **layouts 内**の配置枠です。セルに `slots` は書きません。モデルとの対応は bindings の `cell` で指定します。

GridLayout のセルキーは、`layouts.cells` と同じく 0 始まりの `row` と `col` です。`rowspan` と `colspan` はセルの大きさであり、キーには含めません。

```json
"cells": [
  {"row": 0, "col": 1, "colspan": 2},
  {"row": 1, "col": 0}
]
```

```json
"childrenModel": [
  {"model": "header", "cell": {"row": 0, "col": 1}},
  {"model": "body", "cell": {"row": 1, "col": 0}}
]
```

DockLayout のセルキーは、方向ごとの出現順を 1 始まりで数えます。空セルも数え、配列の宣言順が配置順です。

```json
"cells": [
  {"dock": "top", "size": "40px"},
  {"dock": "top", "size": "30px"},
  {"dock": "center", "size": "remaining"}
]
```

```json
"childrenModel": [
  {"model": "header", "cell": {"dock": "top", "index": 1}},
  {"model": "subHeader", "cell": {"dock": "top", "index": 2}},
  {"model": "body", "cell": {"dock": "center", "index": 1}}
]
```

セルキーはレイアウト定義を読み込むたびに計算します。セルの追加・削除・並び替えで番号が変わることは正常で、永続的な識別子ではありません。同じ GridLayout 内の同じ `row`・`col`、同じ DockLayout 内の同じ方向・`index` は重複できません。旧 `slots` と binding の `slot` は互換読み込みだけを行い、新しい設定では使用しません。

セルには margin / padding を書けません。余白は配置されるモデル自身の box / grid / dock レイアウトに指定してください。

旧レイアウト直下の slots は受け付けません。row / col / span または dock / size を cells へ移し、binding の cell で配置枠を選んでください。旧 slot の margin は中身のレイアウトへ移します。デモの戻るリンクはモデル自身の margin へ移行済みです。F12 の Compound 図もそのモデル自身のレイアウトの余白を表示します。

## 配置枠とモデルの対応

配置情報は `layouts` に集めます。grid-layout の `cells` に `row`・`col`・必要な span、dock-layout の `cells` に `dock`・`size` を書きます。`bindings.childrenModel` は GridLayout なら `{"cell":{"row":0,"col":0}, "model":"nameField"}`、DockLayout なら `{"cell":{"dock":"top","index":1}, "model":"nameField"}` のように対応させます。

セルキーは同じレイアウト内で一意です。GridLayout は 0 始まりの `row`・`col`、DockLayout は方向ごとの 1 始まりの `index` で指定します。未知のセル、同じセルへの二重割り当て、同じモデルの二重配置はエラーです。グリッドの枠は、モデルを割り当てていなくても範囲外・重複・子レイアウトとの重なりを検証します。未割り当ての枠は空き領域として残り、ドックではそのサイズを確保します。ドックの配置順は cells の順で決まり、bindings の並べ替えでは変わりません。

旧形式の `bindings.childrenModel` にある行・列・span・dock・size は読み込みエラーになります。これらを参照先レイアウトの cells に移し、binding 側は cell と model にしてください。共有していたレイアウトで配置が異なる場合は、レイアウト定義を分けます。デモでは下端80px用の pageDock と0px用の pageDockFullscreen を使います。

split-pane の firstModel / secondModel と旧ページレイアウトの inspectorModel は、組み込みの配置枠への対応です。方向・比率・高さなどの配置定義は従来どおり layouts にだけ書きます。１ノードが持つルートレイアウトは最大１つのままです。


