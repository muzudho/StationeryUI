# 文房具 UI のスタイル設定 — 他のアプリへの組み込みガイド

このガイドは、別リポジトリーの MonoGame アプリへ文房具 UI のスタイル設定を組み込む人と、その実装を担当する AI 向けです。既存の C# の画面を一度に作り直さず、対応済みの配置から JSON に移していきます。

説明の基準は **2026-09-21 のリポジトリー実装（スタイル設定エディター v0.2.0 公開後）**です。エディターの版とライブラリーの版は別管理です。古い配布済みの 0.1.1 パッケージに、このガイドの API がすべて入っているとは限りません。利用するコミット・パッケージを固定し、`StationeryStyleFile`、`StationeryLayoutEngine`、`StationeryUiHost.RebindModel` などが存在することを確認してください。

## 読む順番

| ガイド | 内容 |
| --- | --- |
| [組み込みとパラメーターの渡し方](integration.md) | 参照設定、最小 JSON、起動時の読み込み、C# コントロールへの接続、座標変換 |
| [スタイル設定ファイルの読み方](file-format.md) | models / layouts / bindings、Id とパス、各レイアウト、対応範囲 |
| [オートリロードと配布](reload-and-files.md) | 設定の分離、Update / Reload、検証とエラー、ファイル配置、埋め込み |
| [既存画面を少しずつ移行する](migration.md) | C# と JSON の分担、移行順序、確認項目、AI への引き継ぎ文 |

## 最初に押さえる役割分担

```text
app.stationery-config.json → 読み込み先・監視方針
app.stationery-style.json  → models + layouts + bindings
                                  ↓ Parse / validate
                        StationeryStyleSettings
                                  ↓ Arrange(幅, 高さ)
                     パスごとの矩形（ピクセル座標）
                                  ↓ アプリの C# が適用
                     既存コントロールの Bounds
```

JSON は画面構成の設計図です。**JSON を読んだだけでは、コントロールの生成、イベント接続、ページ遷移、保存処理は行われません。** 文房具の Id とモデル構造を C# とそろえ、アプリ側で配置結果を適用します。

スタイル設定エディターの現時点の編集対象は主に `layouts` です。`models` と `bindings` を含めたアプリ固有の接続は、AI コーディングなどで補います。既存ファイルのそれらの情報は保持されますが、エディターのプレビューが表示できることと、利用アプリで必要な部品が接続済みであることは別です。

## 実装の参照先

| 調べたいこと | ソース |
| --- | --- |
| JSON の受け付け条件 | [StationeryStyleSettings.cs](../../../src/StationeryUI/Styling/StationeryStyleSettings.cs) |
| ファイルの読み込み・監視 | [StationeryStyleFile.cs](../../../src/StationeryUI/Styling/StationeryStyleFile.cs) |
| 矩形の計算 | [StationeryLayoutEngine.cs](../../../src/StationeryUI/Styling/StationeryLayoutEngine.cs) |
| コントロールの登録と再接続 | [StationeryUiHost.cs](../../../src/StationeryUI.MonoGame/StationeryUiHost.cs) |
| アプリ固有の検証の実例 | [DemoModelBinding.cs](../../../samples/StationeryUI.Demo/DemoModelBinding.cs) |
| リロードから描画までの実例 | [デモの Program.cs](../../../samples/StationeryUI.Demo/Program.cs)、[DemoPages.cs](../../../samples/StationeryUI.Demo/DemoPages.cs) |

デモ専用の必須 Id や部品数を、別アプリの仕様としてコピーしないでください。自分のアプリの契約に置き換えます。

[デモのスタイル設定仕様](../stationery-style-settings.md) ／ [ライブラリー利用ガイド](../README.md) ／ [エディターの操作](../style-designer.md)
