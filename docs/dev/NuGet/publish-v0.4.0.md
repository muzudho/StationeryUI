# NuGet.org 公開記録 v0.4.0

2026-09-26、検証済みコミット `857e9d420792a73754c807b944bbd59fab660083` にタグ `v0.4.0` を作成して GitHub へ送信した。

[公開ワークフロー](https://github.com/muzudho/StationeryUI/actions/runs/36218880447)は成功した。GitHub 上の全体テスト、ソリューションの復元、3パッケージの作成、Trusted Publishing による認証、NuGet.org への Push がすべて成功している。

| パッケージ | バージョン |
|---|---|
| [StationeryUI](https://www.nuget.org/packages/StationeryUI/0.4.0) | 0.4.0 |
| [StationeryUI.MonoGame](https://www.nuget.org/packages/StationeryUI.MonoGame/0.4.0) | 0.4.0 |
| [StationeryUI.Windows](https://www.nuget.org/packages/StationeryUI.Windows/0.4.0) | 0.4.0 |

公開前のローカル検証は[検証記録](../validation.md)を参照。レイアウトデザイナーの配布はこの NuGet 公開には含めない。

## 公開後の取得確認

NuGet.org のインデックス反映後、NuGet.org のみをソースとする新しいキャッシュへ3パッケージを復元できた。各パッケージの取得元が `https://api.nuget.org/v3/index.json` であることをメタデータでも確認した。

プロジェクト参照を持たない .NET 8 Windows の利用側プロジェクトで、3パッケージを `PackageReference` に指定して Release ビルド・実行に成功した。読み込んだ3つのアセンブリの版番号はいずれも `0.4.0.0` だった。検証用プロジェクトと専用キャッシュは `obj/NuGet040Verification` に置いた。
