# 公開の自動化

継続して NuGet.org に公開する開発者向けの選択肢です。初回の [Web アップロード](publish-v0.2.0.md)には不要です。確認日：2026-09-22。このリポジトリーには以下の公開用設定をまだ追加していません。

## 推奨：GitHub Actions の Trusted Publishing

長期間有効な API キーを保存せず、実行時に短期間有効な認証情報を取得する仕組みです。[公式手順](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)を参照してください。

本人が NuGet.org のユーザーメニューから Trusted Publishing を開き、次の内容でポリシーを登録します。ワークフロー名は今後作成するものとしての提案です。Codex が実装する名前と合わせてから登録してください。

| 項目 | 設定案 |
|---|---|
| Repository Owner | `muzudho` |
| Repository | `StationeryUI` |
| Workflow File | `publish-nuget.yml`（ファイル名のみ） |
| Environment | ワークフロー側で指定しない構成なら空欄 |
| パッケージの範囲 | `StationeryUI*` |
| 公開権限 | 初回登録を含むなら新規パッケージと新バージョンの Push |

Codex は `.github/workflows/publish-nuget.yml` を作り、公開ジョブに `id-token: write` を設定し、`NuGet/login@v1` で認証して Push する処理を用意します。NuGet.org のユーザー名は GitHub のユーザー名やメールアドレスとは区別して設定します。

本人からは、NuGet.org のユーザー名と「ポリシー登録済み」という連絡があれば進められます。タグ `v*` のライブラリーと `style-designer-v*` のエディターを区別し、検証済みの３パッケージだけを公開する構成にします。既存の `build.yml` はビルド・テスト用であり、NuGet.org へは Push しません。

## 手元の端末から公開する場合：API キー

NuGet.org の API Keys で、対象を `StationeryUI*`、必要な Push 権限、有効期限に絞ってキーを作成します。初回登録には新規パッケージを公開できる権限が必要です。詳細は[公式のスコープ付き API キー](https://learn.microsoft.com/en-us/nuget/nuget-org/scoped-api-keys)を参照してください。

キーは本人の端末で安全に扱い、チャット、コマンドの履歴、ソース、ドキュメントへ直接書きません。CLI の実行方法は[公式の公開ガイド](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#push-by-using-a-command-line)を参照してください。キーが漏れた場合は失効・再発行します。

[NuGet の目次](README.md)
