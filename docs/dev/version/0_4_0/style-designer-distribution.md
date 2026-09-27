# レイアウトデザイナー v0.4.0 配布記録

2026-09-26、[GitHub Release](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.4.0) を正式公開した。draft／prerelease は false。リポジトリーの Latest をこの版へ更新した。

- タグ：`style-designer-v0.4.0`
- ビルド元：`70c6945ddc38a3b21cad081a0cdde4ccc6b9b240`
- 対象：Windows x64、.NET 8.0.31 ランタイム同梱、未署名
- 配布物：`StationeryUI.StyleDesigner-v0.4.0-win-x64.zip`、`SHA256SUMS.txt`
- ZIP SHA-256：`267ac5b61d30e981417ca570ae2d4321ed9167c46ab3ef56147fc15058c242ba`
- ローカル出力：`artifacts/release/style-designer-v0.4.0-20260926-141214/`

発行スクリプトで EXE の版番号、ランタイム、必要なライセンス、482ファイルのZIP展開後のハッシュ一致を確認した。発行時に制限環境から NuGet の脆弱性データを取得できず NU1900 警告が出たが、発行と配布物検証は成功した。

展開したアプリで、新規作成・編集・出力と、既存ファイル読み込み・編集・オートセーブ・バックアップ保持を検査した。既存のGUI検査スクリプトに残っている旧形式の出力検証を、作業用 `obj/Test-Designer040.ps1` で現行形式へ合わせて実行した。アプリ内の検査と、スクリーンショットの目視確認も成功した。

GitHubへのアップロード後、ZIPとチェックサムのサイズ・SHA-256がローカルの検証済みファイルと一致することを確認してから公開した。

併せて、公開済みNuGetを案内する[フレームワーク v0.4.0 のGitHub Release](https://github.com/muzudho/StationeryUI/releases/tag/v0.4.0)も作成した。フレームワークの既存タグとNuGetパッケージは変更していない。
