# スタイル設定ファイルの読み方

[目次](README.md) ／ [動かせる最小例](integration.md)

## models → bindings → layouts の順にたどる

たとえば `/app/nameField` の配置を知りたい場合、`models` でそのノードを確認し、`bindings` の `childrenModel` でセルを確認し、そこから `layout` Id で `layouts` の行・列定義へたどります。`layouts[0]` がルート用という規則はありません。

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
- レイアウト Id は `layouts` 配列全体で一意にします。モデル Id とは別の名前空間です。
- `model` / `parentModel` はルートからのパスです。`app/pageA` と `/app/pageA` は同じ参照です。
- `childrenModel[].model` は `parentModel` からの相対パス、または `/` で始まる完全パスです。
- `firstModel` / `secondModel` / `inspectorModel` も、その binding の `model` からの相対パスか完全パスです。
- `..`、ワイルドカード、全ツリーからの短い Id 検索はありません。

パーサーは数字始まりや非 camelCase も許可します。スタイル設定エディターはそれらに警告を表示しますが、警告表示はパーサーの機能ではありません。`StationeryNode.Resolve` を直接呼ぶ場合は、JSON の相対参照と違い **完全パス**を渡します。

## panel — 余白と外枠

```json
{
    "id": "contentPanel",
    "type": "panel",
    "margin": { "top": "4px", "right": "4px", "bottom": "4px", "left": "4px" },
    "padding": { "top": "8px", "right": "8px", "bottom": "8px", "left": "8px" },
    "border": { "top": "1px", "right": "1px", "bottom": "1px", "left": "1px" }
}
```

これは `layouts` の1要素です。対応する binding は `{ "layout": "contentPanel", "model": "/app" }` です。

割り当て矩形から margin を引くと `Bounds`、そこから padding を引くと `ContentBounds` です。border は **全体サイズにも子の配置サイズにも含めず**、外側へ広がる `BorderBounds` になります。実際の線は `ui.DrawPanelBorders(result)` などで描画します。JSON の border を指定しただけでは描画されません。

padding は省略した辺が **8px**、margin / border は省略した辺が **0px** です。余白をなくすなら padding の四辺に明示的に `"0px"` を指定します。各値は非負の px 文字列です。padding は部品内部の文字余白である `Theme.Padding` とは異なります。

## floating-layout — 行・列の配分

```json
{
    "id": "mainGrid",
    "type": "floating-layout",
    "row-definitions": ["48px", "1rate"],
    "column-definitions": ["240px", "1rate", "1.5rate"]
}
```

binding は `layout`、`parentModel`、`childrenModel` を使い、子には `model`、`row`、`column` を指定します。[最小例](integration.md)を参照してください。行・列番号は **0 始まり**で、1セルに1モデルを配置します。空きセルがあっても隣へ自動拡張しません。

`px` の固定分を先に確保し、残りを `rate` の比で分けます。幅740pxで `["240px", "1rate", "1.5rate"]` なら 240px / 200px / 300px です。固定分が領域を超えたら固定部分を比例縮小し、rate 部分は0になります。固定値だけで領域を埋めない場合は末尾に空白を残します。

各トラックは非負の小数を含む文字列です。`"12.5px"`、`"1.5rate"`、`"0rate"` を使えます。数値だけ、負数、指数表記、`%`、`em`、`auto` は未対応です。行・列それぞれに少なくとも1つ正の値が必要です。

親は `viewport` / `page` / `container` / `dialog` 型で、配置するモデルはその子孫である必要があります。同じセルへの重複配置、同じモデルへの重複配置、範囲外の行・列はエラーです。panel と floating-layout を同じモデルへ併用すると、panel の内側をグリッドに分けます。

セルへ配置していないモデルは親の内側矩形を引き継ぎます。**未配置は非表示という意味ではありません。** これが意図しない重なりを生む場合は、アプリの検証で必須部品のセル配置を要求します。

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
    "layout": "editorSplit",
    "model": "/app/editorSplit",
    "firstModel": "leftPane",
    "secondModel": "rightPane"
}
```

対象モデルは `type: "splitPane"`、直下の子は指定した2個だけにします。`vertical` は縦の仕切りで左右分割、`horizontal` は横の仕切りで上下分割です。最初のペーンは左または上です。ratio は単位のない JSON **数値**で0～1、既定0.5。dividerWidth は正の px（既定8）、minimumPaneSize は非負の px（既定40）です。

`Arrange` は設定から静的な分割矩形を返します。ドラッグ可能な UI には C# で `AddSplitPane(node, bounds, label, options)` を作り、`BindSplitContent(split, first, second)` を接続します。`options` には対象 binding から引いた `StationeryLayoutNode.Split` を渡します。子は同じホストに登録された直接の子コントロールである必要があります。

ドラッグ中の比率は `element.Split.Ratio` が持ちます。ホストが `Update` / `Draw` で子を再配置するので、静的な `Arrange` の値を最後に上書きしてドラッグ結果を戻さないようにします。同じ値の `Configure` は比率を保持します。設定を変えると設定側の比率へ戻ります。ドラッグ結果の JSON への書き戻しは自動ではありません。[デモの接続例](../../../samples/StationeryUI.Demo/DemoPages.cs)と[スプリットペーンの説明](../split-pane.md)を参照してください。

## ページ — 本文とインスペクター

`fullscreen-layout` はウィンドウ内のページ全体を本文に使います。OS のフルスクリーン切り替えではありません。`work-page-layout` は下部にインスペクターを確保します。

```json
{
    "id": "workPage",
    "type": "work-page-layout",
    "inspectorHeight": "80px"
}
```

binding は `{ "layout": "workPage", "model": "/app/editorPage", "inspectorModel": "inspectorPanel" }` の形です。対象は `page`、inspectorModel はその直下の `container` にします。fullscreen-layout でも inspectorModel は必要で、高さ0になります。切り替えるときは binding の layout Id を変えます。

インスペクターはページの横幅いっぱいに配置され、ページ本文の padding の影響を受けません。祖先の余白は影響します。work-page-layout の inspectorHeight は省略時80px、低い画面では収まる高さへ縮みます。本文の余白は別の panel、行・列は別の floating-layout で同じページに適用します。ページレイアウト自体に padding やトラック定義は書けません。

インスペクター内の文言、ツールヒント、タイマーバーは C# 側で作ります。アプリケーションバー専用のレイアウト型はまだありません。先頭行に `"40px"` を割り当てるなど、既存のグリッドで構成できます。

## 対応していない指定

| したいこと | 現在の扱い |
| --- | --- |
| CSS の色・フォント・セレクター・継承・状態スタイル | 未対応。C# の Theme / Element 等で指定 |
| gap、rowspan / colspan、内容に合わせる auto サイズ、自動折り返し | 未対応。空き行・列や C# の配置処理で補う |
| 最小・最大幅やメディアクエリー | 汎用指定は未対応。アプリ側で制御。split-pane の minimumPaneSize は専用機能 |
| JSON から任意の UI・イベント・データ接続を自動生成 | 未対応。C# で実装 |
| 配置だけで非表示ページ・モーダルの入力を遮断 | 未対応。アプリがホストの Update / Draw を制御 |
| 任意の window.width / height によるウィンドウ変更 | 汎用パーサーは適用しない。開発者ウィンドウ専用の拡張とは別 |

旧ルートキー `viewport` / `model` / `layout` はエラーになります。未知のプロパティは原則無視されるため、`color` や `gap` を追加してエラーが出ないことは「対応している」証拠になりません。レイアウト間の組み合わせも、アプリ側の `validate` で実際の配置まで検証してください。
