# コントロールのプログラム解説

## ツリーの選択と操作対象

`TreeView.SelectedItem` は選択状態、`TargetItem` はクリック・矢印キーによる操作対象。描画はそれぞれ `StationeryTheme.Selected`（緑の背景）と `TreeTarget`（水色の枠）を使用し、同じノードに両方を表示できる。

`Select(item)` / `ClearSelection()` で選択を設定する。`SetTarget(item)` / `ClearTarget()` で操作対象を設定する。従来のツリーとの互換性のため `SelectOnInteraction` の既定値は true で、操作対象への移動時に選択も連動する。独立して扱う場合は false にする。スタイル設計ツールでは false とし、選択を設定しない。折り畳みで隠れる操作対象は親へ移し、false の場合は選択を保持する。

```csharp
var tree = new TreeView { SelectOnInteraction = false };
var selected = tree.AddNode("selectedNode", "選択中");
var target = tree.AddNode("targetNode", "操作対象");
tree.Select(selected);
tree.SetTarget(target);
```

## panel の枠と配置

`StationeryLayoutEngine.Arrange` は margin → padding の順で領域を計算する。border はレイアウトに影響せず `StationeryLayoutResult.BorderBounds` に外側の描画領域を返す。MonoGame では `host.Draw()` の後に `host.DrawPanelBorders(result)` を呼ぶとテーマの Border 色で描画できる。result はウィンドウ座標で渡す。複数ページを持つ場合は第２引数の述語で表示中のモデルパスに絞る。

margin / border は panel 専用で、他の種類に直接指定するとパーサーが拒否する。padding の既定値 8px は維持し、margin / border の既定値は 0px。

## 既存コントロールの解説

きふわらべから共通コードとともに引き継いだ解説です。現在のクラス名と責務に合わせて更新しました。

- [描画境界](StationeryUI.MonoGame/プログラム解説.md)
- [文字編集と入力操作](StationeryUI.MonoGame/Controls/プログラム解説.md)
- [Button](StationeryUI.MonoGame/Controls/Button/プログラム解説.md)
- [ChartAxisSectionLabel](StationeryUI.MonoGame/Controls/ChartAxisSectionLabel/プログラム解説.md)
- [LinkUnderline](StationeryUI.MonoGame/Controls/LinkUnderline/プログラム解説.md)
- [MultilineTextUnderline](StationeryUI.MonoGame/Controls/MultilineTextUnderline/プログラム解説.md)
- [PopupNumberUnderline](StationeryUI.MonoGame/Controls/PopupNumberUnderline/プログラム解説.md)
- [SectionLabel](StationeryUI.MonoGame/Controls/SectionLabel/プログラム解説.md)
- [Shared/Underline](StationeryUI.MonoGame/Controls/Shared/Underline/プログラム解説.md)
- [SinglelineTextUnderline](StationeryUI.MonoGame/Controls/SinglelineTextUnderline/プログラム解説.md)
- [TableRowLabel](StationeryUI.MonoGame/Controls/TableRowLabel/プログラム解説.md)

GTP オプションの下書き保存、SGF コメントの反映、画面別の付箋配置は利用アプリ固有のため、アプリ側の解説に残しています。

[リストUI設計の目安](list-ui-guidelines.md)：上下移動の最悪回数と、50回以内になる10件の目安。
