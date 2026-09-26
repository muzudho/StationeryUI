# 文房具UIエディターの配布

ソースのプロジェクトと実行ファイル名は `StationeryUIEditor` です。公開済み v0.4.0 は旧名 `StationeryUI.StyleDesigner` のまま保存し、名称変更後の成果物は新しい版として公開します。

リポジトリーのルートから次を実行します。先にプロジェクトのバージョンを更新し、対象コミットを確定してください。

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet build StationeryUI.slnx -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StationeryUIEditor.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-EditorStartup.ps1 -Configuration Release -Pages
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StationeryUIEditor.ps1 -Version <版>
```

発行スクリプトは `StationeryUIEditor-v<版>-win-x64.zip` と `SHA256SUMS.txt` を作り、展開後のファイルをハッシュで照合します。公開時は新しい `ui-editor-v<版>` タグを使い、ZIP・チェックサム・対応するリリースノートを添付します。公開済みの `style-designer-v0.4.0` タグや ZIP は変更しません。
