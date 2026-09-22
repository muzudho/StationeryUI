# ビルドと検証

Windows と .NET SDK 10 を使用し、リポジトリーのルートで実行します。ライブラリーの対象は .NET 8 です。

```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project samples/StationeryUI.StyleDesigner -c Release --no-build
dotnet run --project samples/StationeryUI.Demo -c Release --no-build
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet run --project tests/StationeryUI.Windows.Tests -c Release
dotnet pack StationeryUI.slnx -c Release -o artifacts/packages
```

実 IME や DPI の手動確認と自動検査の範囲は[検証記録](validation.md)を参照してください。エディターの発行は[ソースからの起動と発行](style-designer-build.md)、公開用の成果物作成は[配布手順](distribution/style-designer-release.md)を参照してください。

開発日誌は `docs/dev/log/YYYY/MM.md` に月単位で記録します。

[開発者向け目次](README.md)
