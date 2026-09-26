## 文房具 UI フレームワーク v0.4.0

ビュー構成・レイアウト・モデル参照を扱う設定形式を整理し、配置計算と読み込み検証を改善しました。

- `viewports` の各ノードに `type`、親による `layout`、子の配置を表す `place` を指定します。設定ファイルの拡張子は `*.stationery-ui.json` です。
- ルートのグリッド／ドックに設定した padding が反映されない問題を修正しました。
- 範囲外・重複・不正なグリッド配置経路と、同じスプリットペーン領域への重複割り当てを読み込み時に検出します。
- 不正なドック設定から、診断付きの縦並びへ復旧する処理を現行の参照形式に対応させました。

### 互換性

旧 `models`／`bindings`／`bindingsV2` セクションは受け付けません。モデルは `modelTree`、参照は `viewports` 内の指定または `controlTree` を使う現行形式へ移行してください。

### NuGet

以下の3パッケージを0.4.0へ揃えて更新してください。レイアウトデザイナーは別配布です。

- [StationeryUI 0.4.0](https://www.nuget.org/packages/StationeryUI/0.4.0)
- [StationeryUI.MonoGame 0.4.0](https://www.nuget.org/packages/StationeryUI.MonoGame/0.4.0)
- [StationeryUI.Windows 0.4.0](https://www.nuget.org/packages/StationeryUI.Windows/0.4.0)

### 検証

コアテスト33件、Windowsネイティブ検証10件が成功し、Releaseビルドは警告0・エラー0です。NuGet.orgのみから新しいキャッシュへ3パッケージを復元し、利用側プロジェクトのビルド・実行も確認しました。

[設定ファイルの説明](https://github.com/muzudho/StationeryUI/blob/v0.4.0/docs/user/stationery-ui-settings.md) ／ [レイアウトデザイナー v0.4.0](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.4.0)
