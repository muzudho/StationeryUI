# ページレイアウトとツールヒント

`fullscreen-layout` と `work-page-layout` は本文とインスペクターの領域を分ける。本文の floating-layout と組み合わせて使い、モデルへ本文用のラッパーを追加する必要はない。

StationeryStyleSettings は次を検証する。

- `model` は page。同じページのページレイアウトは一つ。
- `inspectorModel` はページ直下の container。他のセルや split に重複配置しない。
- `inspectorHeight` は work-page-layout の非負 px 値。省略時は 80px、rate は不可。
- fullscreen-layout は高さ 0。inspectorHeight の指定はエラー。

StationeryLayoutEngine はページの Bounds から下部パネルを確保し、残りに panel の padding を適用して ContentBounds とする。本文の floating-layout は ContentBounds を使う。インスペクターの Bounds は本文の padding の外側に配置する。
フルスクリーンではインスペクターとその通常の子レイアウトが高さ 0 になり、DesktopUi が描画・フォーカス対象から外す。
ホストは本文とヒントの Bounds を更新する。物理 px から論理座標へ変換する際は Viewport.Scale で割り、80px の高さを拡大しない。

デモは両ページの toolHint と inspectorPanel の binding を検証する。無効な設定への変更は StationeryStyleFile が拒否し、直前の有効な状態を維持する。
binding の layout だけの変更ではモデルを再接続せず、コントロールの状態を保持する。

## ホバー説明

`DesktopUi.Element.ToolHint` に説明文を設定する。`DesktopUi.Update` 後の `HoveredToolHint` は最前面のヒット要素の説明を返す。非アクティブ時や説明がない要素上では null。
デモはこれを読み取り専用の toolHint の Label に渡す。説明文は機能の意味に関わるため C# 側で設定し、配置は JSON が担当する。
既存のテーマ・折り返し・キーボードスクロール・コピーを使う。下部ヒントは Edit／Popup／Move のホバーバッジとは別に表示する。

## 検査

PageLayoutTests は全幅の 80px、本文領域、フルスクリーン、小さいウィンドウ、無効な binding、再読み込みと Id の維持を検査する。
Test-DemoPages.ps1 は通常／150% の明暗テーマで、ヒントの表示・解除、ページ移動、分割操作を検査する。

設定例と移行方法は [ユーザー向けスタイル設定](../user/stationery-style-settings.md) を参照。
