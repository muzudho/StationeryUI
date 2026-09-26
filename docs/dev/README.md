# 開発者向けドキュメント

文房具 UI をアプリに組み込む方と、このリポジトリーの開発・保守に貢献する方への案内です。製品の動作や用語は、必要に応じて[利用者向けドキュメント](../user/README.md)も参照してください。

## アプリへ組み込む

- [ライブラリーの組み込み](library-integration.md)：パッケージ参照、MonoGame への接続、テーマ、入力の所有権。
- [スタイル設定の組み込みガイド](style-settings/README.md)：JSON の読み込み、F12 のホスト、リロード、移行。
- [ツリーの組み込み](tree-integration.md) ／ [スプリットペーンの組み込み](split-pane-integration.md)
- [文房具モデルと検査情報の接続](model-inspection.md)
- [ツールヒントの接続](tool-hints.md)

## 改善に貢献する

- [貢献方法](CONTRIBUTING.md)
- [ビルドと検証](building.md)
- [開発日誌 — 2026年9月](log/2026/09.md)
- [設計と境界](architecture.md)
- [実装・引き継ぎ計画](implementation-plan.md)
- [抽出元一覧](extraction-inventory.md)

## 実装を調べる

- [コントロールのプログラム解説](control-guide.md)
- [開発者ウィンドウの実装](developer-window.md)
- [ページレイアウト](page-layouts.md)
- [アンダーラインとアクションバッジ](action-badges.md)
- [リスト UI 設計の目安](list-ui-guidelines.md)
- [文房具UIエディターの開発](products/ui-editor/README.md)
- [エディターのオートセーブ](products/ui-editor/autosave.md)
- [検証記録](validation.md)
- 構造
	- [*.style-settings.json ファイルの作り方](structure/style-settings-file/layouts/README.md)

## 配布する

- [NuGet.org への公開](NuGet/README.md)：本人の準備、初回アップロード、継続公開の自動化。
- [配布・リリース資料](distribution/README.md)：作成手順、確認事項、配布記録。

[リポジトリーの入口へ戻る](../../README.md)
