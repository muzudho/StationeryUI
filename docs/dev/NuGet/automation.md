# 公開の自動化

現行ソースでは [publish-nuget.yml](../../../.github/workflows/publish-nuget.yml) を使い、`v*` タグからライブラリーの3パッケージを公開します。初回の Web アップロードは [v0.2.0 の記録](../version/0_2_0/nuget-publishing.md) に残しています。エディターは別のタグと ZIP で配布します。

## 推奨：GitHub Actions の Trusted Publishing

長期間有効な API キーを保存せず、実行時に短期間有効な認証情報を取得する仕組みです。[公式手順](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)を参照してください。

NuGet.org の Trusted Publishing に登録したポリシーと、ワークフローの設定を対応させます。登録と公開の結果は [v0.3.0 の公開記録](../version/0_3_0/nuget-publishing.md) にあります。

| 項目 | ワークフローに合わせる値 |
|---|---|
| Repository Owner | `muzudho` |
| Repository | `StationeryUI` |
| Workflow File | `publish-nuget.yml`（ファイル名のみ） |
| Environment | ワークフロー側で指定しない構成なら空欄 |
| パッケージの範囲 | `StationeryUI*` |
| 公開権限 | 初回登録を含むなら新規パッケージと新バージョンの Push |

ワークフローには `id-token: write` と `NuGet/login@v1` を設定済みです。NuGet.org のユーザー名は GitHub のユーザー名やメールアドレスとは区別して確認します。

タグ `v*` はライブラリーの公開に使います。エディターの `ui-editor-v*` と旧 `style-designer-v*` はこのワークフローの対象外です。`build.yml` はビルド・テスト用で、NuGet.org へは Push しません。

## 手元の端末から公開する場合：API キー

NuGet.org の API Keys で、対象を `StationeryUI*`、必要な Push 権限、有効期限に絞ってキーを作成します。初回登録には新規パッケージを公開できる権限が必要です。詳細は[公式のスコープ付き API キー](https://learn.microsoft.com/en-us/nuget/nuget-org/scoped-api-keys)を参照してください。

キーは本人の端末で安全に扱い、チャット、コマンドの履歴、ソース、ドキュメントへ直接書きません。CLI の実行方法は[公式の公開ガイド](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#push-by-using-a-command-line)を参照してください。キーが漏れた場合は失効・再発行します。

[NuGet の目次](README.md)
