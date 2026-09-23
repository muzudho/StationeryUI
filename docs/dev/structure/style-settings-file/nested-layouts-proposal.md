# レイアウトのネストとセル範囲指定

この拡張はリポジトリー版に実装済みです。JSON の記法、既定値、互換性とエディターの操作は[利用者向け仕様](../../user/nested-layouts.md)を参照してください。

## 解析結果

`StationeryStyleSettings.Layouts` は、親から子の順に並ぶ全レイアウトの一覧です。`StationeryLayoutNode.Id` はローカル名、`Path` は先頭 `/` 付きのスラッシュ区切りの完全パス、`ParentPath` は親の完全パスです。`Children` でも直接の子を辿れます。

binding の `Layout` と比較するときは、`Id` ではなく `Path` を使用します。セル指定は `Row`・`Column`・`RowSpan`・`ColumnSpan` に格納します。文房具のセル割り当てである `StationeryCellBinding` も span を持ちます。

## 配置の基準

binding の `ModelPath` ごとに、参照先を含む最上位のレイアウトツリーを適用します。子への参照しかなくても、親の余白・グリッドを含めて計算します。同じモデルを基準とする同じツリーは一度だけ計算し、各グリッドの binding からその子孫モデルの位置を決めます。

同じツリーを別のモデルへ適用すれば別インスタンスになります。親への binding と子への binding が異なるモデルを基準にしている場合も、別インスタンスです。同一モデルに適用できるルートレイアウトは最大１つです。独立したボックスとグリッドの併用もエラーになります。同じルートの子孫への複数 binding は、その１つのツリー内の配置指定として扱います。

モデルの `Bounds`・`ContentBounds` に加え、配置結果には `LayoutBounds`・`LayoutContentBounds`・`LayoutBorderBounds` があります。キーは `モデルの完全パス:レイアウトの完全パス`、例は `/demo:/frame/grid/inner` です。ネストしたグリッドのセルを描く際は、その `LayoutContentBounds` を `ArrangeGridCells` へ渡してください。

## エディターと検証

レイアウトの検索は JSON の `children` を再帰的に辿り、完全パスで区別します。Id変更では子孫のパス参照も更新します。追加・位置変更・削除は、一時的な JSON を検証してから採用します。

`NestedLayoutTests` は、異なる行高・列幅を跨ぐ矩形、余白の適用、同名の子、旧形式、範囲外・重複・子数制限、リサイズ、親Id変更、保存と再読み込みを検査します。

[スタイル設定の組み込みガイド](README.md)
