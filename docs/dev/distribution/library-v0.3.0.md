# 文房具 UI v0.3.0 配布記録

2026-09-22 の v0.3.0 配布記録です。`StationeryUI`、`StationeryUI.MonoGame`、`StationeryUI.Windows` を同じ版番号で作成します。

## 作成と確認

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet pack src/StationeryUI/StationeryUI.csproj -c Release -o artifacts/nuget/v0.3.0
dotnet pack src/StationeryUI.MonoGame/StationeryUI.MonoGame.csproj -c Release -o artifacts/nuget/v0.3.0
dotnet pack src/StationeryUI.Windows/StationeryUI.Windows.csproj -c Release -o artifacts/nuget/v0.3.0
```

3 個の `.nupkg` の版番号、依存関係、README、MIT ライセンス、AI エージェント向け導入ガイドを確認してから公開します。公開済みの同一 ID・同一版は上書きできないため、修正には新しい版を使います。

## NuGet.org 公開

NuGet.org の Web Upload または、端末内だけに保持した API キーを使う `dotnet nuget push` で 3 個を公開します。API キーをソース、ログ、チャットへ書きません。公開後は各パッケージページと新しい一時フォルダーからの復元を確認します。

```powershell
dotnet nuget push artifacts/nuget/v0.3.0/StationeryUI.0.3.0.nupkg --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
dotnet nuget push artifacts/nuget/v0.3.0/StationeryUI.MonoGame.0.3.0.nupkg --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
dotnet nuget push artifacts/nuget/v0.3.0/StationeryUI.Windows.0.3.0.nupkg --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
```

`StationeryUI.StyleDesigner` は `IsPackable=false` なので NuGet には含めません。フレームワークの Git タグは `v0.3.0`、デザイナーのタグは `style-designer-v0.3.0` と分けます。
