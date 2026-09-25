# レイアウト・インスペクターの表示

## ツリーノードのレイアウト表示

スタイル設定を使うアプリは `ui.Inspect(settings)`（表示状態も指定する場合は `ui.Inspect(settings, visible)`）で最新の `StationeryStyleSettings` を渡す。複数ホストの検査情報をまとめる場合は、結合後に `DeveloperInspectionLayout.Apply(entries, settings)` を呼び、その結果を開発者ウィンドウへ渡す。デモもこの方法で接続している。

`StationeryInspectionEntry.LayoutTypes` に所有するレイアウトの種類、`Cell` にセルの `Column` / `Row` / `ColumnSpan` / `RowSpan` を付加する。ツリーは `(demoPage : Page) (- : gridLayout)` や `(btn123 : Button) (1, 1, 1, 1 : -)` と表示する。左側は子としてのセル位置、右側は親として持つレイアウトを表示し、情報がない側は `-` とする。ネストしたレイアウトは最上位の種類１つだけを表示する。セル位置は 0 始まりの設定値をそのまま使う。レイアウト情報だけの更新ではツリーの選択・開閉状態を維持する。

従来の `Inspect()` や追加情報のないスナップショットも利用でき、その場合のレイアウト表示は `(—)`。独自の配置処理を使うアプリは `LayoutTypes` / `Cell` を自分で設定できる。`Apply` は渡された設定を正として情報を付け直すので、削除された binding の情報は残らない。

デモの F12 は、読み込んだ `models` の階層を表示する。デモの原本には `topDemoPage` と `splitPaneDemoPage` を置き、名前欄は `/demo/topDemoPage/body/nameField` になる。models と layouts の役割は [スタイル設定ガイド](../user/stationery-ui-settings.md) を参照。

子としての配置と親としてのレイアウトの両方がある場合は `(content : Container) (1, 0, 1, 1 : gridLayout)` と表示する。セル情報がないドック配置などでは左側は `-` になる。
