# スタイル設定の組み込みガイド

別リポジトリーの MonoGame アプリへ、文房具 UI のスタイル設定を組み込む開発者と、その実装を担当する AI 向けの目次です。

## 初めて導入する

- [役割分担と導入範囲](overview.md)：JSON とコードの分担、対象の版、実装の参照先。
- **[スタイル設定と F12 を一式で取り込む](full-integration.md)**：取り込み対象、子プロセスホスト、起動分岐、更新・終了処理。
- [組み込みとパラメーターの渡し方](integration.md)：参照設定、最小 JSON、C# 接続例、座標変換。

## 詳細を調べる

- [設定ファイルの読み方](file-format.md)：models / layouts / bindings、Id、各レイアウト。
- [配置結果をアプリへ渡す](layout-results.md)：Bounds / ContentBounds / BorderBounds と再接続。
- [オートリロードとファイルの配置・配布](reload-and-files.md)
- [デモのモデルとコードの契約](demo-contract.md)：必須 Id、検証とフォールバック。
- [既存画面を少しずつ移行する](migration.md)：移行順序、確認項目、AI への引き継ぎ。

製品が使われる場面は、利用者向けの[ユースケース](../../user/style-settings/use-case.md)を参照してください。

## ネストを組み込む

- [レイアウトのネストとセル範囲指定](nested-layouts-proposal.md)：`children`、先頭 `/` 付きのスラッシュ区切りのパス、`row`・`col`・`rowspan`・`colspan` の実装と配置基準。

[ライブラリーの組み込み](../library-integration.md) ／ [開発者向け目次](../README.md)
