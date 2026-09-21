# スタイル設計ツールのリリース手順

リポジトリーのルートで PowerShell を開いて実行します。Git、GitHub CLI、対象をビルドできる .NET SDK が必要です。GUI 検査は Windows のデスクトップセッションで実行します。

## 1. 版とソースを固定する

1. `samples/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.csproj` の `Version`、`AssemblyVersion`、`FileVersion` を更新します。例：`0.1.1`、`0.1.1.0`、`0.1.1.0`。
2. 利用者向け資料、開発日誌、`docs/dev/releases/style-designer-v<版>.md` のリリース本文を更新します。
3. 自動テストを実行し、対象の変更だけをコミットします。個人用メモなどをまとめて追加しないでください。

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
git status --short
git rev-parse HEAD
```

コミット SHA を記録します。発行スクリプトは作業フォルダーをビルドするため、HEAD の記録だけでは未コミット変更の混入を防げません。ビルド対象と同梱資料がコミット内容と一致することを確認します。

## 2. 発行する

以下は **v0.1.0 の再現用の例**です。次の公開ではアプリ版と採用するランタイム版を変更します。公開済みの版は上書きしません。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StyleDesigner.ps1 -Version 0.1.0 -RuntimeVersion 8.0.30
```

[発行スクリプト](../../../scripts/Publish-StyleDesigner.ps1) は Release / win-x64 / self-contained で発行し、PDB を除外します。新しい日時付きフォルダーへ出力し、ZIP・SHA-256 を作成、別フォルダーへ展開して元ファイルとのハッシュ一致まで検査します。GUI 検査と GitHub 公開は行いません。

`RELEASE_DIRECTORY` に表示される出力先を記録します。

| 出力 | 用途 |
| --- | --- |
| `StationeryUI.StyleDesigner/` | 発行されたアプリ全体 |
| `StationeryUI.StyleDesigner-v<版>-win-x64.zip` | 添付する配布物 |
| `SHA256SUMS.txt` | 添付するチェックサム |
| `extracted/StationeryUI.StyleDesigner/` | ZIP 展開後の検査対象 |
| `build-record.json` | SHA、版、ランタイム、ファイル数、ローカルパスの作業記録。添付対象外 |

## 3. 配布内容と GUI を検査する

[注意事項](notes.md) に従い、ライセンスと不要ファイルの混入を確認します。発行先と ZIP 展開先の両方で検査します。スクリプトは GUI 検査より先に ZIP を作りますが、検査が済むまでは公開しません。

`$releaseDir` は実際の出力先へ置き換えます。

```powershell
$releaseDir = 'artifacts/release/style-designer-v0.1.0-YYYYMMDD-HHMMSS'
$publishedExe = Join-Path $releaseDir 'StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe'
$extractedExe = Join-Path $releaseDir 'extracted/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $publishedExe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe -Existing -NativeDialog -Dark
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe -Existing -NativeDialog -CancelDialog
```

各コマンドが成功してから次へ進みます。新規作成、JSON 出力、既存編集、モデル・bindings の保持、元ファイルが変更されないこと、OS のファイル選択とキャンセルを確認します。日本語、ボタン、明暗テーマも画像または実画面で確認します。

修正でソースが変わったらコミットと発行からやり直します。検査後のフォルダーを不用意に再圧縮してテスト出力を混入させないでください。

## 4. GitHub に公開する

以下は値を今回の記録へ置き換え、各コマンドの失敗時には中断してください。タグ・push・公開は公開依頼の範囲内で実行します。

```powershell
$version = '0.1.0'
$tag = "style-designer-v$version"
$revision = '<検証したコミットの完全な SHA>'
$zip = Join-Path $releaseDir "StationeryUI.StyleDesigner-v$version-win-x64.zip"
$checksum = Join-Path $releaseDir 'SHA256SUMS.txt'
gh auth status
git remote -v
git rev-parse HEAD
```

HEAD と対象 SHA が一致すること、タグが既存でないこと、送信先が `muzudho/StationeryUI` であることを確認します。

```powershell
git tag -a $tag $revision -m "Style Designer v$version"
git push --atomic origin HEAD:refs/heads/main "refs/tags/$tag"
```

push 成功後に公開します。

```powershell
gh release create $tag $zip $checksum --repo muzudho/StationeryUI --verify-tag --title "スタイル設計ツール v$version" --notes-file "docs/dev/releases/style-designer-v$version.md" --latest
```

`--latest` はリポジトリー全体の最新リリース表示を変更します。ライブラリーと共用のため毎回意図を確認します。v0.1.0 では設計ツールを最新として公開しました。

本文先頭は次の形式とし、今回のタグ・ZIP 名へ直接リンクします。

```markdown
> [!IMPORTANT]
> 通常の利用には、Assets の **[Windows x64 用 ZIP](今回の ZIP への直接リンク)** をダウンロードしてください。

`Source code` は開発者向けです。
```

機能、対象範囲、起動方法、ランタイム同梱、署名状態、検証結果、利用手順へのリンクも記載します。[初回の本文](../releases/style-designer-v0.1.0.md) を参考にしてください。

## 5. 公開後を確認する

```powershell
gh release view $tag --repo muzudho/StationeryUI --json url,tagName,name,isDraft,isPrerelease,body,assets
git ls-remote origin "refs/tags/$tag" "refs/tags/$tag^{}"
Get-FileHash -LiteralPath $zip -Algorithm SHA256
```

- 本文の `[!IMPORTANT]` とリンクが保持され、正式公開では draft / prerelease が false であること。
- 注釈付きタグの `^{}` が検証したコミット SHA と一致すること。
- ZIP と `SHA256SUMS.txt` が uploaded で、名前・サイズが一致すること。
- asset の `digest` が得られる場合は ZIP の SHA-256 と一致すること。取得できない場合は公開 ZIP を別の場所へダウンロードして照合すること。

アップロード失敗時は、まず既存リリースと添付状態を確認します。成功したタグや添付を無条件に作り直さず、公開済みの内容修正が必要なら新しい版で配布します。結果を配布記録へ追記します。
