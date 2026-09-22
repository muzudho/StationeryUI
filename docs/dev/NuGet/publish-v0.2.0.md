# v0.2.0 を公開する手順

NuGet.org にサインイン済みの公開担当者向けです。確認日：2026-09-22。今回は Web アップロードを使うため、API キーは不要です。

## 配布するファイル

[GitHub Release v0.2.0](https://github.com/muzudho/StationeryUI/releases/tag/v0.2.0) の Assets から、次の３ファイルと `SHA256SUMS.txt` を取得します。この作業環境では `D:\github.com\muzudho\StationeryUI\artifacts\packages` にもあります。

| 順番 | ファイル | NuGet.org の Package ID |
|---|---|---|
| 1 | `StationeryUI.0.2.0.nupkg` | `StationeryUI` |
| 2 | `StationeryUI.MonoGame.0.2.0.nupkg` | `StationeryUI.MonoGame` |
| 3 | `StationeryUI.Windows.0.2.0.nupkg` | `StationeryUI.Windows` |

ソース ZIP、エディターの ZIP、ハッシュ一覧は NuGet.org にアップロードしません。GitHub で配布済みのパッケージと同じファイルを使います。PowerShell の `Get-FileHash -Algorithm SHA256 <ファイルのパス>` の結果を `SHA256SUMS.txt` と比較できます。

## Web 画面で公開する

1. NuGet.org の [Upload](https://www.nuget.org/packages/manage/upload) を開く。
2. 表の１番目の `.nupkg` を選択する。
3. Verify 画面で Package ID と Version `0.2.0`、説明、MIT ライセンス、依存関係を確認する。README の Preview も確認する。
4. 内容が正しければ Submit を押す。
5. 残り２ファイルも同じ手順で公開する。MonoGame と Windows の `StationeryUI` 依存が `0.2.0` であることを確認する。

v0.2.0 の３件は本人による登録が完了しました。今後、新規 ID が他者所有や予約名により拒否されたら、エラーを控えて Codex に知らせてください。ファイル名だけを変更しても Package ID は変わりません。改名が必要ならプロジェクトと依存関係から調整します。

手順と所有権の説明は[公式のパッケージ公開ガイド](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)を参照してください。

## 公開結果を確認する

Submit 後は検証と検索への反映を待ちます。すぐ検索に出なくても再アップロードせず、アカウントの Manage packages と通知メールを確認します。長く処理が進まない場合は [NuGet の稼働状況](https://status.nuget.org/)を確認してください。

公開先は次の３ページです。2026-09-22 に３件とも NuGet.org からの取得を確認しました。

- [StationeryUI 0.2.0](https://www.nuget.org/packages/StationeryUI/0.2.0)
- [StationeryUI.MonoGame 0.2.0](https://www.nuget.org/packages/StationeryUI.MonoGame/0.2.0)
- [StationeryUI.Windows 0.2.0](https://www.nuget.org/packages/StationeryUI.Windows/0.2.0)

v0.2.0 は、NuGet.org のみをソースにした新規保存先への復元と、３パッケージを参照するプロジェクトのビルドに成功しました。[組み込みガイド](../library-integration.md)も公開済みの内容へ更新しました。今後の公開で一部だけ成功した場合は、成功した ID を伝え、失敗したものだけを再開します。

公開済みの同一 ID・同一バージョンは上書きできません。修正が必要なら次の版を作ります。掲載を非表示にする操作は版番号の再利用にはなりません。[公式の削除ポリシー](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages)を参照してください。

[NuGet の目次](README.md)
