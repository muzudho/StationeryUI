# StationeryUI — 文房具UI

**MonoGame フレームワークを使い、ゲームよりも、設定・編集・制作などのツール向けのクロスプラットフォーム GUI を作ることを目指すプロジェクトです。** 文房具をモチーフにした GUI ライブラリー「文房具 UI」と、その画面構成を設計する独立アプリ「スタイル設定エディター」を開発しています。

## ユーザー向け — まず使ってみる

スタイル設定エディターでは、ツリーとプレビューを見ながらレイアウトを設計し、`*.stationery-style.json` に保存できます。JSON を AI コーディングへ渡して「この設計で C# の画面を作って」と依頼する使い方を想定しています。

### すぐ使う手順

1. **[スタイル設定エディター v0.2.0 のリリース](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.2.0)** を開き、Assets の `StationeryUI.StyleDesigner-v0.2.0-win-x64.zip` をダウンロードします。`Source code` は開発者向けです。
2. ZIP を新しいフォルダーへすべて展開し、`StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe` を起動します。.NET ランタイムは同梱しています。
3. ［スタイル設定ファイル選択］で、新規作成または既存ファイルの編集を選びます。
4. ツリーの `layouts` から操作対象を選び、列幅・行高や余白を変更してプレビューで確認します。
5. 新規プランは［エクスポート］ダイアログの［書き出す］か［新規作成］で保存先を決めます。以後はオートセーブされます。既存ファイルは読み込み時にバックアップを作り、元ファイルへ自動保存します。戻したいときは［セーブポイントに戻す］を使います。

- **[エディターの使い方](docs/user/style-designer.md)**：操作、保存、バックアップと復元、現在の編集範囲。
- **[スタイル設定ファイルの仕様](docs/user/stationery-style-settings.md)**：`models`・`layouts`・`bindings` と px／rate。
- [インスペクターパネルの設計ガイド](docs/user/inspector-panel-guide.md)：操作説明と保存状態の表示。

現在のエディター配布版は **Windows x64 向け**です。macOS・Linux で動くエディターの配布はまだありません。クロスプラットフォーム対応はプロジェクトの目標であり、すべての OS で動作確認済みという意味ではありません。

## 開発者向け — 組み込む・開発する

OS 非依存のモデル、MonoGame の描画・入力接続、OS 固有の機能を分離しています。現在は Windows の文字描画・クリップボード・日本語 IME 接続を実装しています。ツリー、スプリットペーン、スタイルによる配置、明暗テーマなどを使ってツールの画面を組み立てられます。

- **[利用・組み込みガイド](docs/user/README.md)**：ライブラリーのパッケージ、サンプル、MonoGame への組み込み、対応範囲。
- **[開発者向けガイド](docs/dev/README.md)**：プロジェクト構成、設計、実装、ビルド・検証。
- **[開発日誌（2026年9月）](docs/dev/log/2026/09.md)**：開発の経緯と v0.2.0 の変更。
- [配布・リリース手順](docs/dev/distribution/README.md)、[貢献方法](CONTRIBUTING.md)。

Windows と .NET SDK 10 を使う場合：

```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project samples/StationeryUI.StyleDesigner -c Release --no-build
dotnet run --project tests/StationeryUI.Tests -c Release
```

エディター v0.2.0 とライブラリーの版は別管理です。ライブラリーのソース上の版は 0.1.1 で、公開 API は今後変更する可能性があります。エディターはライブラリーの NuGet パッケージには含めません。

## ライセンス

[MIT License](LICENSE)。

きふわらべの碁2026とサークルスペースコーディネーターの実装から抽出・整理しています。元のライセンス表示と依存物については [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を参照してください。
