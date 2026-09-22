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

## v0.3.0 公開実績（2026-09-23）

今回の公開は、長期 API キーを作らず GitHub Actions の Trusted Publishing で完了した。

1. NuGet.org のアカウントで Trusted Publishing policy を作成した。
   - Repository owner: `muzudho`
   - Repository: `StationeryUI`
   - Workflow file: `publish-nuget.yml`
   - Environment: 空欄
2. GitHub CLI のデバイス認証を行った。
   - `gh auth login -h github.com`
   - `https://github.com/login/device` で表示されたワンタイムコードを入力した。
3. `main` と `v0.3.0`、`style-designer-v0.3.0` を GitHub へ push した。
4. `publish-nuget.yml` がテスト、復元、pack、OIDC ログイン、NuGet push を実行した。
5. 初回実行は Windows プロジェクトの assets 不足で失敗したため、workflow に `dotnet restore StationeryUI.slnx` を追加し、タグを修正版コミットへ更新して再実行した。
6. 再実行は成功し、3 パッケージが NuGet.org に公開された。
7. `Publish-StyleDesigner.ps1 -Version 0.3.0` で作成した ZIP と `SHA256SUMS.txt` を `style-designer-v0.3.0` Release に添付した。

公開確認：

- [StationeryUI 0.3.0](https://www.nuget.org/packages/StationeryUI/0.3.0)
- [StationeryUI.MonoGame 0.3.0](https://www.nuget.org/packages/StationeryUI.MonoGame/0.3.0)
- [StationeryUI.Windows 0.3.0](https://www.nuget.org/packages/StationeryUI.Windows/0.3.0)
- [Style Designer v0.3.0 Release](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.3.0)
- ZIP SHA-256: `d13d59e85a2f149b78cbd531ebc2a4b278de789c0cf7c616c291340913c18ede`

同じ ID・同じ版の NuGet パッケージは上書きできない。修正時は新しい版番号を作り、タグも新しくする。
