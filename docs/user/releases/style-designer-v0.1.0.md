> [!IMPORTANT]
> 通常の利用には、Assets の **[Windows x64 用 ZIP](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.1.0/StationeryUI.StyleDesigner-v0.1.0-win-x64.zip)** をダウンロードしてください。

`Source code` は開発者向けです。

## スタイル設計ツール v0.1.0

文房具 UI の画面構成を GUI で設計し、`*.stationery-ui.json` を出力する独立した Windows アプリです。出力した設計図を AI コーディングへ渡し、C# の実装を依頼できます。

### 使い方

1. ZIP を新しいフォルダーへすべて展開します。
2. `StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe` を起動します。ランタイム同梱版なので .NET の追加インストールは不要です。
3. １ページ目で新規作成、または Windows のファイル選択ダイアログから既存ファイルを開きます。
4. ２ページ目で左のスタイルツリーと右の表を見ながら編集し、新しい名前の JSON にエクスポートします。

### 主な機能

- 最大 8×8 のフローティングレイアウトを作成。
- 行・列のサイズを数値と `rate`／`px` に分けて指定。
- 新規設計のセルに文房具の種類と表示名を設定。Id は自動生成。
- `models`・`layouts`・`bindings` のツリー表示と、複数の既存レイアウトの切り替え。
- 既存ファイルのモデル・bindings・追加情報を保持したサイズ編集と別名出力。
- 文房具 UI の枠付きボタン、明暗テーマ、サイズ配分のプレビュー。

### 対象範囲

Windows x64 向け、.NET 8.0.30 ランタイム同梱です。設計ツール本体は未署名です。Windows の保護機能によっては起動が制限される場合があります。

新規設計は１ページ・１つのフローティングレイアウトが対象です。既存ファイルの編集は行列のサイズとセル数が対象で、スプリットペーンなど他のレイアウト種類はツリーで確認できます。C# の生成、既存ファイルへの上書き、アプリ終了後の未出力プランの復元は行いません。

このリリースはスタイル設計ツールの v0.1.0 です。文房具 UI ライブラリーの既存 `v0.1.0` とは別の成果物で、タグは `style-designer-v0.1.0` です。

### 検証・資料

自動テスト 27 件、新規作成・既存編集・Windows ファイル選択／キャンセルの GUI 検査を実施。配布 ZIP を別フォルダーへ展開し、起動と JSON 出力を確認しています。

- [使い方](https://github.com/muzudho/StationeryUI/blob/style-designer-v0.1.0/docs/user/style-designer.md)
- [開発日誌](https://github.com/muzudho/StationeryUI/blob/style-designer-v0.1.0/docs/dev/log/2026/09.md)
- [SHA-256 チェックサム](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.1.0/SHA256SUMS.txt)

EXE だけを取り出さず、DLL・README・licenses を含むフォルダー全体を使用してください。第三者ライブラリーのライセンスは ZIP 内に同梱しています。
