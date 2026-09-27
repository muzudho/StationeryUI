# スタイルデザイナー v0.3.0 配布記録

- タグ：`style-designer-v0.3.0`
- EXE の FileVersion：`0.3.0.0`
- 配布形式：Windows x64、.NET ランタイム同梱 ZIP
- NuGet：デザイナーは公開せず、フレームワーク 3 パッケージを別途公開

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StyleDesigner.ps1 -Version 0.3.0 -RuntimeVersion 8.0.31
```

スクリプトが作成した `artifacts/release/style-designer-v0.3.0-日時/` の ZIP、`SHA256SUMS.txt`、`build-record.json` を確認し、ZIP を展開して EXE 起動と GUI 検査を行います。確認済みの ZIP と SHA-256 を GitHub Release `style-designer-v0.3.0` に添付します。公開済みのタグや ZIP は差し替えず、修正版には新しい版番号を付けます。

## 公開実績

- GitHub Release: https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.3.0
- Assets: `StationeryUI.StyleDesigner-v0.3.0-win-x64.zip` と `SHA256SUMS.txt`
- ZIP の SHA-256: `d13d59e85a2f149b78cbd531ebc2a4b278de789c0cf7c616c291340913c18ede`
- Release は draft / prerelease ではないことを確認した。
