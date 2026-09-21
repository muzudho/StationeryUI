# ライブラリーと設計ツールの成果物分離

| 成果物 | プロジェクト | 配布方法 |
|---|---|---|
| 文房具 UI | `src/StationeryUI`、`src/StationeryUI.MonoGame`、`src/StationeryUI.Windows` | 各プロジェクトを `dotnet pack -c Release` で NuGet 化 |
| スタイル設計ツール | `samples/StationeryUI.StyleDesigner` | `dotnet publish` で Windows アプリのフォルダーを作る |

依存方向は設計ツール → MonoGame／Windows → コア。ライブラリーから設計ツールを参照しない。
StyleBlueprint など設計・出力専用のコードは設計ツールのプロジェクトに置く。設計ツールは IsPackable=false なのでライブラリーの NuGet 配布物にならない。
配布用アプリには依存 DLL が含まれるが、ライブラリーを使うアプリへ設計ツールを同梱する必要はない。

設計ツールの UI は文房具 UI で描画する。入力中の数値は文字列として保持し、エクスポート前に StationeryStyleSettings.Parse で検証する。
プレビューには同じ StationeryLayoutEngine を使う。出力は models/layouts/bindings 形式の UTF-8 JSON。日本語 label は設計メタデータとして保持する。
エクスポートは隣接する一時ファイルへ完成した JSON を書いてから、新しい名前に移動する。移動時に同名ファイルがあれば失敗し、上書きしない。

## 検証

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1
```

StyleBlueprintTests は設計ツールのモデルだけをソースリンクし、小数・単位・日本語・モデル Id・サイズ変更・不正入力・新規出力・既存ファイル保護を検査する。
GUI 検査は専用環境変数を指定してテスト用入力を与え、セルの種類と表示名、JSON 出力、スクリーンショットを確認する。通常起動では自動入力を行わない。

操作方法と対象範囲は [ユーザー向けガイド](../user/style-designer.md) を参照。
