# デモのモデルとコードの契約

## デモの文房具とコードの役割

原本の models は、demo ルートの下に topDemoPage、splitPaneDemoPage、layoutDemoPage を持つ。
各ページの直下は `body` と `inspectorPanel` の２つのコンテナー。`pageDock`（スプリットページのみ `pageDockFullscreen`、どちらも dock-layout）で `inspectorPanel` を bottom、`body` を center に配置する。トップとレイアウトデモは bottom の size が80px、スプリットペーンデモは0px。
topDemoPage/body には 9 部品と editDialog を定義する。本文のグリッド自身に padding: 8px（四辺）を指定し、body に結び付け、ページ自身はドックだけを持つ。
Id はコードの動作との接続にも使う固定名で、AI コーディング時に models と C# を合わせて生成・保守する。
表示文字列、入力処理、保存などの動作は C# が担当する。
JSON の type だけから任意の新しいコントロールを生成するわけではない。

メインには nameField / memoField（textBox）、
themeButton / scaleButton / applyTitleButton / openDialogButton（button）、sampleTree（tree）、splitPaneDemoLink / layoutDemoLink（link）が各 1 個必要。
**トップページの 9 部品すべてに grid-layout のセルへの binding が必要。**
splitPaneDemoPage には topDemoLink、verticalSplit、horizontalSplit と、それぞれの子のテキスト欄を定義する。
スプリットペーンの子は split-pane の firstModel / secondModel で配置する。
layoutDemoPage/body は topDemoLink、見出しと各セルの textBlock を持ち、ページ直下に inspectorPanel/toolHint を置く。layoutShowcase の子にボックスとグリッドを配置し、さらにその子へ先頭 `/` 付きのスラッシュ区切りの layout パスで部品を binding する。これらの parentModel は demo/layoutDemoPage/body。入れ子のグリッドは rowspan: 2 / colspan: 2、最下行の部品は colspan: 3 を使う。各ページの toolHint にもセルへの binding が必要。
ツリーの項目と開閉操作は [ツリーの使い方](../../user/tree.md) を参照。
ダイアログには nameField（textBox）、cancelButton / saveButton（button）が各 1 個必要。
ダイアログの配置は現時点では C# が担当するため、ダイアログやその子への binding はデモではエラーにする。

各範囲内で必要な部品を page / container で包み直せる。
欠落・重複・型違い・未接続の部品など、コードとの契約を満たさない場合は設定全体を採用せず最後の正常な状態を維持する。親子を特定できるドック設定エラーは、[エラー表示と縦並びへの代替配置](../dock-layout.md#エラー時の表示と復旧)を使用する。
起動時に不正なスタイルだった場合は、コード内の標準モデル、5 行 × 2 列、四辺 8px を使う。

models / layouts / bindings の変更はすべて自動リロード／F5 の対象。
階層や配置を変更しても、既存コントロールのテキスト・選択範囲・テーマ・イベント処理は維持する。
モデルを変更するとフォーカスのパスも更新し、マウスキャプチャは解除する。
F12 は models のツリーと実際の文房具の配置座標を表示する。レイアウト専用ツリーの表示は未対応。

未知のプロパティは原則無視するため、設定名は上記の綴りに合わせる。
