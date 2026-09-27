# 文房具UIエディターの配布

ソースのプロジェクトと実行ファイル名は `StationeryUIEditor` です。v0.4.0 は旧名 `StationeryUI.StyleDesigner`、v0.5.0 以降は新名で配布しています。過去の配布物は[版別の記録](../version/README.md)から確認してください。

リポジトリーのルートから次を実行します。先にプロジェクトのバージョンを更新し、対象コミットを確定してください。

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet build StationeryUI.slnx -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StationeryUIEditor.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-EditorStartup.ps1 -Configuration Release -Pages
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StationeryUIEditor.ps1 -Version <版>
```

発行スクリプトは `StationeryUIEditor-v<版>-win-x64.zip` と `SHA256SUMS.txt` を作り、展開後のファイルをハッシュで照合します。公開時は新しい `ui-editor-v<版>` タグを使い、ZIP・チェックサム・対応するリリースノートを添付します。公開済みの `style-designer-v0.4.0` タグや ZIP は変更しません。
