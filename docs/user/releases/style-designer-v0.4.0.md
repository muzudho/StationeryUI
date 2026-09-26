> [!IMPORTANT]
> アプリを使う場合は **[Windows x64 用 ZIP](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.4.0/StationeryUI.StyleDesigner-v0.4.0-win-x64.zip)** をダウンロードしてください。`Source code` は開発者向けです。

## レイアウトデザイナー v0.4.0

新しい文房具 UI ファイルを開いても編集ページへ進まない問題を修正しました。

- `layout: { "ref": "grid" }` に対応し、参照先の入れ子のグリッドを読み込み・編集できるようにしました。参照と未編集の設定を保持して出力します。
- 複数ページで共有するコントロールについて、プレビュー対象ページに合う参照経路を選ぶようにしました。
- 新規デザインの外側に意図しない余白が付く問題を修正しました。
- ファイルの拡張子は `*.stationery-ui.json` です。旧 `models`／`bindings`／`bindingsV2` 形式を使っているファイルは現行形式への移行が必要です。

## 起動方法

ZIPを展開して `StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe` を起動してください。Windows x64 用で、.NET 8.0.31 ランタイムを同梱しています。実行ファイルは未署名です。

[SHA256SUMS.txt](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.4.0/SHA256SUMS.txt)でZIPのハッシュを確認できます。

## 検証

コアテスト33件とWindowsネイティブ検証10件が成功しています。今回の配布ZIPを展開したアプリでも、新規作成・編集・出力と、既存ファイルの読み込み・編集・オートセーブ・元データのバックアップ保持を確認しました。

モデルや参照情報の直接編集は対象外です。新形式に対する編集機能の拡充は引き続き進めています。

[使い方](https://github.com/muzudho/StationeryUI/blob/style-designer-v0.4.0/docs/user/style-designer.md) ／ [フレームワーク v0.4.0](https://github.com/muzudho/StationeryUI/releases/tag/v0.4.0)
