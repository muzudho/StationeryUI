# スタイル設定ファイルの読み方

[目次](README.md) ／ [動かせる最小例](integration.md)




## cells と slots

`cells` は **layouts 内**のセル定義です。bindings 内には追加しません。例えばグリッドは `"cells": [{"row":0,"col":1,"slots":[{"id":"input"}]}]` とします。ドックはセルに `dock` と `size` を書き、cells の配列順が角の優先順です。

slots はセルに名前を付けるだけです。１セルにつき Id は最大１個とし、slots の省略または空配列で名前なしのセルを定義できます。名前がなくてもセルは領域を確保します。Id は同じレイアウト内で一意です。slots 内の id 以外の項目はすべてエラーになります。セルにも margin / padding は書けません。余白は配置されるモデル自身の box / grid / dock レイアウトに指定してください。

bindings は従来どおり `"childrenModel":[{"slot":"input","model":"nameField"}]` という対応付けだけです。モデルの Id と slot Id は一致させる必要はありません。

旧レイアウト直下の slots は受け付けません。row / col / span または dock / size を cells へ移し、id はそのセルの slots 内に残してください。旧 slot の margin は中身のレイアウトへ移します。デモの戻るリンクは余白用 box-layout へ移行済みです。F12 の Compound 図もそのモデル自身のレイアウトの余白を表示します。

## 配置枠とモデルの対応

配置情報は `layouts` に集めます。grid-layout の `cells` に `row`・`col`・必要な span、dock-layout の `cells` に `dock`・`size` を書きます。各セルの `slots` は `[{"id":"input"}]` のように Id だけを持ちます。`bindings.childrenModel` は `{"slot":"input", "model":"nameField"}` のような対応だけを持ちます。枠名とモデル Id は別物なので、一致させる必要はありません。

枠名は同じレイアウト内で一意です。異なるレイアウトでは同じ枠名を再利用できます。未知の枠、同じ枠への二重割り当て、同じモデルの二重配置はエラーです。グリッドの枠は、モデルを割り当てていなくても範囲外・重複・子レイアウトとの重なりを検証します。未割り当ての枠は空き領域として残り、ドックではそのサイズを確保します。ドックの配置順は cells の順で決まり、bindings の並べ替えでは変わりません。

旧形式の `bindings.childrenModel` にある行・列・span・dock・size は読み込みエラーになります。これらを参照先レイアウトの cells に移し、セル内の slots で Id を付け、binding 側は slot と model だけにしてください。共有していたレイアウトで配置が異なる場合は、レイアウト定義を分けます。デモでは下端80px用の pageDock と0px用の pageDockFullscreen を使います。

split-pane の firstModel / secondModel と旧ページレイアウトの inspectorModel は、組み込みの配置枠への対応です。方向・比率・高さなどの配置定義は従来どおり layouts にだけ書きます。１ノードが持つルートレイアウトは最大１つのままです。

## １ノード１レイアウトと共通の padding

各モデルノードが持てるルートレイアウトは最大１つです。`box-layout` / `grid-layout` / `dock-layout` はどれも `padding` を指定できます。例えば `"padding": {"top":"8px","right":"8px","bottom":"8px","left":"8px"}` とします。値は非負の px 文字列です。省略した辺は box-layout では従来どおり8px、grid-layout / dock-layout では0pxです。padding を引いた内側に子を配置し、領域が足りない場合は０サイズまで縮めます。

複合配置はレイアウトの `children` でネストします。同じルートツリー内の `/frame/grid` などへ複数 binding を書いても、所有するレイアウトは１つです。dock の各領域をさらに分割するときは子コンテナーに grid などを持たせます。別々のルートを同一ノードへ binding すると、読み込み時にエラーになります。実行中のリロードでは直前の有効な設定を維持します。

旧設定で余白用 box と配置用 grid を併用していた場合は、box の padding を grid へ移して box の binding を削除してください。margin / border も必要なら、box の `children` に grid を入れ、binding の layout を `/boxId/gridId` に変更します。

## models → bindings → layouts の順にたどる

たとえば `/app/nameField` の配置を知りたい場合、`models` でそのノードを確認し、`bindings.childrenModel` の `slot` を確認し、`layout` が指す `layouts` の cells 内で、その slot Id を持つセルの行・列を調べます。`layouts[0]` がルート用という規則はありません。

| セクション | 意味 | C# での対応 |
| --- | --- | --- |
| `models` | 文房具の所属・親子関係 | `Models`、`CreateTree()` |
| `layouts` | 再利用できる配置定義 | `Layouts`、`StationeryLayoutNode` |
| `bindings` | モデルと配置定義の対応 | `Bindings`、`StationeryLayoutBinding` |

3 配列はすべて必須です。現在の `models` は **`type: "viewport"` のルート1個**に限ります。その子にページやコンテナーを作ります。`layouts` と `bindings` は空配列にもできますが、コントロールが正しく配置される保証にはなりません。アプリ側で必要な binding を検証します。

モデルには `id`、`type`、必要に応じて `children` を置きます。レイアウトの型とモデルの型は別です。たとえばモデルは `splitPane`、レイアウトは `split-pane` です。モデルの `type` 文字列を任意に増やしても、対応する UI が自動実装されるわけではありません。

## Id とパス

- Id は空でない `[A-Za-z0-9_]+`。大小文字を区別します。camelCase を推奨します。
- 同じ親の直下のモデル Id は一意にします。異なる親の下なら同じ Id を使えます。
- レイアウト Id は同じ親の下で一意にします。モデル Id とは別の名前空間です。`bindings.layout` は `/frame/grid/inner` のような先頭 `/` 付きのスラッシュ区切りの完全パスで参照します。
- `model` / `parentModel` はルートからのパスです。`app/pageA` と `/app/pageA` は同じ参照です。
- `childrenModel[].model` は `parentModel` からの相対パス、または `/` で始まる完全パスです。
- `firstModel` / `secondModel` / `inspectorModel` も、その binding の `model` からの相対パスか完全パスです。
- `..`、ワイルドカード、全ツリーからの短い Id 検索はありません。

パーサーは数字始まりや非 camelCase も許可します。スタイル設定エディターはそれらに警告を表示しますが、警告表示はパーサーの機能ではありません。`StationeryNode.Resolve` を直接呼ぶ場合は、JSON の相対参照と違い **完全パス**を渡します。

## dock-layout — 配列順に四辺を配置

`layouts.cells` に `dock`・`size` を指定し、各セルの slots には id だけを書き、`bindings.childrenModel` には `slot` と `model` だけを書きます。四辺は cells の配列順、`center` は最後に残りを使用します。方向の重複と任意個数に対応します。`row-definitions` / `column-definitions` は使いません。[完全な設定例とエラー時の代替配置](../dock-layout.md)を参照してください。

## box-layout — 余白と外枠

旧名 `panel` は読み込み時の互換用別名として受け付け、解析結果の Type は `box-layout` に統一します。エディターで開いて保存すると type を新名で出力します。既存の Id・モデル名・bindings の参照先や、余白・枠の計算は変えません。

```json
{
    "id": "contentPanel",
    "type": "box-layout",
    "margin": { "top": "4px", "right": "4px", "bottom": "4px", "left": "4px" },
    "padding": { "top": "8px", "right": "8px", "bottom": "8px", "left": "8px" },
    "border": { "top": "1px", "right": "1px", "bottom": "1px", "left": "1px" }
}
```

これは `layouts` の1要素です。対応する binding は `{ "layout": "/contentPanel", "model": "/app" }` です。

割り当て矩形から margin を引くと `Bounds`、そこから padding を引くと `ContentBounds` です。border は **全体サイズにも子の配置サイズにも含めず**、外側へ広がる `BorderBounds` になります。実際の線は `ui.DrawPanelBorders(result)` などで描画します。JSON の border を指定しただけでは描画されません。

padding は省略した辺が **8px**、margin / border は省略した辺が **0px** です。余白をなくすなら padding の四辺に明示的に `"0px"` を指定します。各値は非負の px 文字列です。padding は部品内部の文字余白である `Theme.Padding` とは異なります。

## grid-layout — 行・列の配分

旧名 `floating-layout` は読み込み時の互換用別名として受け付け、解析結果の Type は `grid-layout` に統一します。エディターで旧形式を開いて保存すると、レイアウトの type を新名で出力します。Id・bindings の参照先は変更しません。

```json
{
    "id": "mainGrid",
    "type": "grid-layout",
    "row-definitions": ["48px", "1rate"],
    "column-definitions": ["240px", "1rate", "1.5rate"]
}
```

binding は `layout`、`parentModel`、`childrenModel` を使い、子には `model` と `slot` だけを指定します。行・列は参照先レイアウトの `cells` に `row`、`col`（別名 `column`）として定義します。セル内の slots には id だけを書きます。`rowspan`・`colspan` は省略時1で、複数セルに跨る範囲を指定できます。行・列番号は **0 始まり**です。範囲の重複と範囲外はエラーです。空きセルへ自動拡張しません。[ネストとセル範囲指定](nested-layouts-proposal.md)を参照してください。

`px` の固定分を先に確保し、残りを `rate` の比で分けます。幅740pxで `["240px", "1rate", "1.5rate"]` なら 240px / 200px / 300px です。固定分が領域を超えたら固定部分を比例縮小し、rate 部分は0になります。固定値だけで領域を埋めない場合は末尾に空白を残します。

各トラックは非負の小数を含む文字列です。`"12.5px"`、`"1.5rate"`、`"0rate"` を使えます。数値だけ、負数、指数表記、`%`、`em`、`auto` は未対応です。行・列それぞれに少なくとも1つ正の値が必要です。

親は `viewport` / `page` / `container` / `dialog` 型で、配置するモデルはその子孫である必要があります。同じセルへの重複配置、同じモデルへの重複配置、範囲外の行・列はエラーです。各モデルに適用できるルートレイアウトは最大１つです。余白は grid-layout 自身の padding へ指定し、複合配置は children でネストするか子コンテナーへ別のレイアウトを適用します。

セルへ配置していないモデルは親の内側矩形を引き継ぎます。**未配置は非表示という意味ではありません。** これが意図しない重なりを生む場合は、アプリの検証で必須部品のセル配置を要求します。

## 入れ子のグリッドレイアウト

**配置エンジンでは対応済み**です。外側のグリッドのセルへ `container` モデルを配置し、そのコンテナーを `parentModel` とする別の grid-layout の binding を作ります。内側のグリッドは、そのコンテナーの `ContentBounds` を分割します。

```text
models:   /app → contentArea (container) → nameField / applyButton
layouts:  outerGrid、innerGrid（それぞれ独立した定義）
bindings: outerGrid のセルに contentArea を配置
          innerGrid の親を /app/contentArea にして、その子を配置
```

この既存方式に加え、レイアウト自身の `children` で入れ子を表現できます。ボックスの子は0～1個、グリッドの子は0～複数個です。グリッドの子にセル位置と span を指定します。どちらの方式も配置は外側から内側へ計算します。

エディターはネストしたレイアウトの追加・編集・保存に対応しています。必要な models / bindings の生成・移し替えは自動ではありません。[ネストの実装と配置基準](nested-layouts-proposal.md)も参照してください。

## split-pane — 左右・上下の分割

`layouts` の要素：

```json
{
    "id": "editorSplit",
    "type": "split-pane",
    "orientation": "vertical",
    "ratio": 0.4,
    "dividerWidth": "8px",
    "minimumPaneSize": "40px"
}
```

`bindings` の要素：

```json
{
    "layout": "/editorSplit",
    "model": "/app/editorSplit",
    "firstModel": "leftPane",
    "secondModel": "rightPane"
}
```

対象モデルは `type: "splitPane"`、直下の子は指定した2個だけにします。`vertical` は縦の仕切りで左右分割、`horizontal` は横の仕切りで上下分割です。最初のペーンは左または上です。ratio は単位のない JSON **数値**で0～1、既定0.5。dividerWidth は正の px（既定8）、minimumPaneSize は非負の px（既定40）です。

`Arrange` は設定から静的な分割矩形を返します。ドラッグ可能な UI には C# で `AddSplitPane(node, bounds, label, options)` を作り、`BindSplitContent(split, first, second)` を接続します。`options` には対象 binding から引いた `StationeryLayoutNode.Split` を渡します。子は同じホストに登録された直接の子コントロールである必要があります。

ドラッグ中の比率は `element.Split.Ratio` が持ちます。ホストが `Update` / `Draw` で子を再配置するので、静的な `Arrange` の値を最後に上書きしてドラッグ結果を戻さないようにします。同じ値の `Configure` は比率を保持します。設定を変えると設定側の比率へ戻ります。ドラッグ結果の JSON への書き戻しは自動ではありません。[デモの接続例](../../../samples/StationeryUI.Demo/DemoPages.cs)と[スプリットペーンの説明](../../user/split-pane.md)を参照してください。

## ページ — 本文とインスペクター

`fullscreen-layout` はウィンドウ内のページ全体を本文に使います。OS のフルスクリーン切り替えではありません。`work-page-layout` は下部にインスペクターを確保します。

```json
{
    "id": "workPage",
    "type": "work-page-layout",
    "inspectorHeight": "80px"
}
```

binding は `{ "layout": "/workPage", "model": "/app/editorPage", "inspectorModel": "inspectorPanel" }` の形です。対象は `page`、inspectorModel はその直下の `container` にします。fullscreen-layout でも inspectorModel は必要で、高さ0になります。切り替えるときは binding の layout Id を変えます。

インスペクターはページの横幅いっぱいに配置され、ページ本文の padding の影響を受けません。祖先の余白は影響します。work-page-layout の inspectorHeight は省略時80px、低い画面では収まる高さへ縮みます。本文を子コンテナーにし、そのコンテナーのレイアウトに余白や行・列を指定します。同じページへ別のルートレイアウトは適用できません。ページレイアウト自体に padding やトラック定義は書けません。

インスペクター内の文言、ツールヒント、タイマーバーは C# 側で作ります。アプリケーションバー専用のレイアウト型はまだありません。先頭行に `"40px"` を割り当てるなど、既存のグリッドで構成できます。

## 対応していない指定

| したいこと | 現在の扱い |
| --- | --- |
| CSS の色・フォント・セレクター・継承・状態スタイル | 未対応。C# の Theme / Element 等で指定 |
| gap、内容に合わせる auto サイズ、自動折り返し | 未対応。空き行・列や C# の配置処理で補う |
| 最小・最大幅やメディアクエリー | 汎用指定は未対応。アプリ側で制御。split-pane の minimumPaneSize は専用機能 |
| JSON から任意の UI・イベント・データ接続を自動生成 | 未対応。C# で実装 |
| 配置だけで非表示ページ・モーダルの入力を遮断 | 未対応。アプリがホストの Update / Draw を制御 |
| 任意の window.width / height によるウィンドウ変更 | 汎用パーサーは適用しない。開発者ウィンドウ専用の拡張とは別 |

旧ルートキー `viewport` / `model` / `layout` はエラーになります。未知のプロパティは原則無視されるため、`color` や `gap` を追加してエラーが出ないことは「対応している」証拠になりません。レイアウト間の組み合わせも、アプリ側の `validate` で実際の配置まで検証してください。


`bindings.layout` は `/frame/grid/inner` のように、先頭 `/` 付きのスラッシュ区切り絶対パスを指定します。最上位も `/mainGrid` と書きます。レイアウトの `id` は `mainGrid` のようなローカル名のままです。旧ドット区切り、先頭 `/` の省略、末尾 `/`、空の区間（`//`）、`.` / `..` は受け付けません。旧設定は `frame.grid` → `/frame/grid` と置き換えてください。モデル参照の既存ルールと slot のローカル Id は変更しません。


## 配置枠を基準にした margin

box-layout / grid-layout / dock-layout に `margin` を指定できます。cells と slots は margin・padding を持ちません。`{"left":"10px","top":"10px","right":"10px","bottom":"10px"}` の四辺形式で、非負の px 文字列を使い、省略した辺は0pxです。bindings へは書きません。

親の padding の内側を grid / dock で配置枠へ分け、その枠から margin を引きます。margin は隣の枠の位置や大きさを変えません。モデル自身のルートレイアウトの margin を引き、次に padding を引いて子の配置領域を得ます。余白が大きすぎる場合、幅・高さは0まで縮みます。ネストした子レイアウトも自身の割り当て枠が基準です。

F12 の詳細欄には margin を外側、padding を内側とする Compound 図を表示します。数値は設定上の px で、帯の幅は模式図です。margin と padding はそのモデル自身のルートレイアウトの値を表示します。祖先や内部の子レイアウトの余白は合算しません。スタイル情報を渡していない従来の検査データには図を表示しません。

デモ本文のグリッドは四辺 margin 4px + padding 4px とし、従来の合計8pxの余白を保ちます。レイアウトデモの戻るリンク自身に余白用 box-layout を持たせ、margin 4px・padding 0px を設定しています。


### モデル自身の margin

モデルにも `"margin": {"left": "4px", "top": "4px", "right": "4px", "bottom": "4px"}` を指定できる。非負の px 指定で、未指定の辺は 0。割り当てられた配置枠から内側へ縮めるので、余白だけのために boxLayout を挟む必要はない。セル位置・結合・dock の方向などは引き続き layouts に定義し、slots は Id だけを持つ。

モデル自身と所有するルートレイアウトの両方に margin がある場合は、モデル、ルートレイアウトの順に適用する。モデルの Compound 図は両者の margin の合計とルートレイアウトの padding、レイアウト定義の Compound 図はその定義自身の値を示す。
デモの `demoViewport`（余白ゼロ）と `elementMargin1`（リンクの余白のみ）は削除し、後者の 4px を `topDemoLink` の margin へ移した。枠線と padding を実演する二つの boxLayout は残す。
