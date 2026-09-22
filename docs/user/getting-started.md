# ダウンロードして起動する

1. [最新のリリース](https://github.com/muzudho/StationeryUI/releases/latest)を開きます。Assets から `StationeryUI.StyleDesigner-<版>-win-x64.zip` を選びます。`Source code` は通常の利用には不要です。
2. ZIP を新しいフォルダーへすべて展開します。
3. 展開先の `StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe` を起動します。ランタイム同梱版なので、.NET の追加インストールは不要です。EXE だけを取り出さず、DLL・licenses などを含むフォルダー全体を使います。
4. ［スタイル設定ファイル選択］で、新規作成または既存ファイルの編集を選びます。
5. ツリーの `layouts` から操作対象を選び、列幅・行高や余白を変更してプレビューで確認します。
6. 新規プランは［エクスポート］から［書き出す］または［新規作成］で保存先を決めます。以後はオートセーブされます。

既存ファイルは、読み込み時にバックアップを作り、元ファイルへ自動保存します。変更を戻すときは［セーブポイントに戻す］を使います。保存先のない新規プランは、最初の保存前に終了すると残りません。

対象は Windows x64 です。起動要件や署名状態は、入手した版のリリースノートで確認してください。

[詳しい操作と復元方法](style-designer.md) ／ [製品の説明](products.md) ／ [利用者向け目次](README.md)
