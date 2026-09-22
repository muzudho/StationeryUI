# NuGet.org 公開手順 v0.3.0

1. `dotnet run --project tests/StationeryUI.Tests -c Release` でテストを実行します。
2. `StationeryUI`、`StationeryUI.MonoGame`、`StationeryUI.Windows` を `dotnet pack -c Release` で同じ `0.3.0` として作成します。
3. `.nupkg` の README、ライセンス、依存関係、パッケージ ID、版番号を確認します。
4. NuGet.org の Upload 画面、または `dotnet nuget push` で 3 個を公開します。
5. 各パッケージページで公開状態を確認し、空の一時フォルダーから NuGet.org のみをソースにして復元・ビルドします。

同一 ID・同一版は再公開できません。公開後に問題が見つかったら、修正して `0.3.1` など新しい版を作ります。API キーは環境変数など端末内だけで扱い、リポジトリーへ保存しません。

デザイナーは NuGet に含めません。`scripts/Publish-StyleDesigner.ps1 -Version 0.3.0` で作成した Windows x64 ZIP を、GitHub Release `style-designer-v0.3.0` に添付します。
