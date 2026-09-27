# 開発者向けドキュメント

文房具 UI をアプリに組み込む方と、このリポジトリーの開発・保守に貢献する方への案内です。ここから参照する共通資料は現在のソースが対象です。公開済みの版を調べる場合は[版別の開発・配布記録](version/README.md)を使ってください。

## アプリへ組み込む

- [ライブラリーの組み込み](../ai-agents/get-started/library-integration.md)：パッケージ参照、MonoGame への接続、テーマ、入力の所有権。
- [設定ファイルの構造](structure/style-settings-file/layouts/README.md)：JSON のレイアウト定義。
- [ツリーの実装](structure/control-csharp/tree.md) ／ [スプリットペーンの実装](structure/control-csharp/split-pane.md)
- [ツールヒントの実装](structure/control-csharp/tool-hints.md)

## 改善に貢献する

- [貢献方法](CONTRIBUTING.md)
- [ビルドと検証](building.md)
- [開発日誌 — 2026年9月](log/2026/09.md)
- [設計と境界](architecture.md)
- [実装・引き継ぎ計画](implementation-plan.md)
- [抽出元一覧](extraction-inventory.md)

## 実装を調べる

- [コントロールのプログラム解説](structure/control-csharp/control-guide.md)
- [開発者ウィンドウの実装](products/developer-window/README.md)
- [ページレイアウト](structure/control-csharp/page-layouts.md)
- [アンダーラインとアクションバッジ](structure/control-csharp/action-badges.md)
- [リスト UI 設計の目安](structure/control-csharp/list-ui-guidelines.md)
- [文房具UIエディターの開発](products/ui-editor/README.md)
- [エディターのオートセーブ](products/ui-editor/autosave.md)
- [検証記録](validation.md)
- 構造
	- [*.style-settings.json ファイルの作り方](structure/style-settings-file/layouts/README.md)

## 配布する

- [NuGet.org への公開](NuGet/README.md)：本人の準備、初回アップロード、継続公開の自動化。
- [配布・リリース資料](distribution/README.md)：作成手順、確認事項、配布記録。
- [版別の開発・配布記録](version/README.md)：公開済みのライブラリーとエディター。

[リポジトリーの入口へ戻る](../../README.md)
