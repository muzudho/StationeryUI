> [!IMPORTANT]
> 通常の利用には、Assets の **[Windows x64 用 ZIP](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.2.0/StationeryUI.StyleDesigner-v0.2.0-win-x64.zip)** をダウンロードしてください。

`Source code` は開発者向けです。

## スタイル設定エディター v0.2.0

文房具 UI のレイアウトをツリーとプレビューで設計する独立アプリです。MonoGame を使ったツール向けクロスプラットフォーム GUI を目指すプロジェクトですが、今回の配布物は Windows x64 用です。

### 起動と保存

1. ZIP を新しいフォルダーにすべて展開します。
2. `StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe` を起動します。.NET 8.0.31 ランタイム同梱のため、.NET の追加インストールは不要です。
3. 新規作成か既存ファイルの編集を選び、ツリーの layouts からレイアウトを指定します。
4. 新規プランは［エクスポート］から［書き出す］または［新規作成］で保存先を決めます。以後は自動保存されます。

**v0.1.0 と異なり、既存ファイルは元のパスへ上書き保存されます。** 読み込み時に `.1.bak`、`.2.bak`…のバックアップを作り、最大20世代を保持します。破棄したい変更は［セーブポイントに戻す］から復元できます。

### 主な変更

- 最後の変更から1.5秒後のオートセーブ、保存状態・待ち時間バー、変更日時付きの復元一覧。
- panel／floating-layout の追加、Idの指定・変更、margin・padding・border の編集。
- layouts に編集を集中。models と bindings は閲覧専用に整理。
- 列数・行数の自動反映と縮小確認、ウィンドウサイズに追従する配置プレビュー。
- 初期表示するレイアウトを水色の操作対象枠で示し、プレビューにもIdを表示。対象なしでは空表示。
- 上部アプリケーションバーと下部インスペクター、エクスポートダイアログ、半透明の幕付きモーダル。
- タスクバーを避ける起動サイズ、縦並びのファイル選択画面、ダークモードで小さい文字が欠ける問題の修正。

### 対象範囲

未署名の Windows x64 アプリです。macOS・Linux・Windows ARM64 での動作や、.NET 未導入のクリーンPCでの起動は今回の検証対象に含みません。C# の自動生成は行いません。新規プランは最初の保存先を決めるまではメモリー上だけにあります。

ライブラリーの既存タグとは別の `style-designer-v0.2.0` で公開します。EXE だけを取り出さず、DLL・README・licenses を含むフォルダー全体を使ってください。

### 資料

コア自動テスト28件、Windowsの入力・文字描画検査10件を実施。ランタイム同梱ZIPを別フォルダーに展開し、新規作成、既存編集、OSのファイル選択／キャンセル、タイマー保存と復元を検証しています。

- [使い方](https://github.com/muzudho/StationeryUI/blob/style-designer-v0.2.0/docs/user/style-designer.md)
- [開発日誌](https://github.com/muzudho/StationeryUI/blob/style-designer-v0.2.0/docs/dev/log/2026/09.md)
- [SHA-256](https://github.com/muzudho/StationeryUI/releases/download/style-designer-v0.2.0/SHA256SUMS.txt)
