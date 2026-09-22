# 文房具 UI ライブラリー v0.2.0

2026-09-22 の NuGet パッケージ配布記録です。スタイル設定エディターの同名バージョンとは別の成果物です。

## 配布物

[GitHub Release v0.2.0](https://github.com/muzudho/StationeryUI/releases/tag/v0.2.0) に以下を添付します。NuGet.org への公開は含みません。

- `StationeryUI.0.2.0.nupkg`
- `StationeryUI.MonoGame.0.2.0.nupkg`
- `StationeryUI.Windows.0.2.0.nupkg`
- `SHA256SUMS.txt`

版番号は `Directory.Build.props` の 0.2.0、ソースはタグ `v0.2.0` を参照してください。3 つのパッケージを同じローカル NuGet ソースへ配置する手順は[ライブラリーの組み込み](../library-integration.md)を参照してください。

## 主な変更

- `box-layout` と `grid-layout` の `children` による入れ子、ドット区切りのレイアウト参照。
- `row` / `col` / `rowspan` / `colspan` による配置。ボックスの子は最大 1 個、グリッドの子は複数。
- 旧名 `panel` / `floating-layout` の読み込み互換性を維持。
- 開発者ウィンドウのキャプチャー選択、文房具 Id のパス表示、インスペクターパネル。
- ソース内のデモにレイアウトページを追加。実行アプリは NuGet パッケージに含めない。

## 検証

- Release のソリューションビルド：警告 0、エラー 0。
- 共通テスト：29/29 成功。ネスト、スパン、配置設定の検証を含む。
- Windows の SDL 入力ライフサイクル・イベント・文字描画検査：10 項目成功。
- 3 パッケージのバージョン、ライブラリー間の依存バージョン、DLL の収録を確認。

実 IME の候補選択、各 OS の DPI、タッチ操作、別 PC での手動検証は今回の検証に含みません。
