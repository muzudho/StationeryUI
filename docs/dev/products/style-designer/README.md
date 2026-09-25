# ライブラリーと設計ツールの成果物分離

| 成果物 | プロジェクト | 配布方法 |
|---|---|---|
| 文房具 UI | `src/StationeryUI`、`src/StationeryUI.MonoGame`、`src/StationeryUI.Windows` | 各プロジェクトを `dotnet pack -c Release` で NuGet 化 |
| スタイル設計ツール | `samples/StationeryUI.StyleDesigner` | `dotnet publish` で Windows アプリのフォルダーを作る |

依存方向は設計ツール → MonoGame／Windows → コア。ライブラリーから設計ツールを参照しない。
StyleBlueprint など設計・出力専用のコードは設計ツールのプロジェクトに置く。設計ツールは IsPackable=false なのでライブラリーの NuGet 配布物にならない。
配布用アプリには依存 DLL が含まれるが、ライブラリーを使うアプリへ設計ツールを同梱する必要はない。

設計ツールの UI は文房具 UI で描画する。入力中の数値は文字列として保持し、エクスポート前に StationeryStyleSettings.Parse で検証する。
プレビューには同じ StationeryLayoutEngine を使う。新規デザインの出力は models/layouts/bindingsV2 形式の UTF-8 JSON。日本語 label は設計メタデータとして保持する。
エクスポートは隣接する一時ファイルへ完成した JSON を書いてから、新しい名前に移動する。移動時に同名ファイルがあれば失敗し、上書きしない。

## ２ページ構成と既存ファイル

DesignerPages が開始ページと編集ページの移動、左側の JSON ツリー、レイアウト選択を担当する。右側の編集画面と左ツリーは別の StationeryUiHost を持ち、ポインター座標をそれぞれ変換する。キーボード入力は最後にクリックした側へ渡す。
StyleBlueprint.Parse/Open は読み込んだ JsonObject 全体を保持する。既存ファイルの編集では、選択した grid-layout の row-definitions と column-definitions だけを変更する。レイアウトを切り替える前に現在の変更を検証・保持する。
モデルの階層や種類、bindings、未編集のレイアウト、未知の追加プロパティを再構築しない。読み込み元への保存 API は設けず、従来の新規エクスポートを使う。
8×8 を超える表や他のレイアウト種類もツリーへ表示するが、この版の表編集の対象外。編集可能な表がない文書には確認・別名出力の画面を表示する。
StationeryUiHost.ReplaceTree は UI の要素 Id とスクロール位置を保ってツリーのスナップショットを交換する。交換時には旧ノードへのポインターキャプチャを解除する。設計ツール側は展開状態と選択を引き継ぐ。

## 検証

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Existing
```

StyleBlueprintTests は設計ツールのモデルだけをソースリンクし、小数・単位・日本語・モデル Id・サイズ変更・不正入力・新規出力・既存ファイル保護を検査する。
GUI 検査は専用環境変数を指定してテスト用入力を与え、セルの種類と表示名、JSON 出力、スクリーンショットを確認する。通常起動では自動入力を行わない。
既存ファイルの検査では、開始ページからファイルを開き、サイズを編集し、ツリーの別レイアウトをクリックする。出力したモデル・bindings と元ファイルのハッシュが変わらないことも確認する。

## ボタンとネイティブファイル選択

デザイナーの StationeryUiHost は `UseStationeryButtons = true` を指定し、既存の StationeryButtonRenderer を使ってボタンを描く。四辺の枠、押下時の移動、ホバー・フォーカス・無効状態のテーマ色を共通化する。テキスト入力とリンクの描画には影響しない。他のホストはこの設定を有効にしない限り従来どおり。
WindowsStyleFileDialog は Windows の OpenFileDialog を開き、スタイル JSON／JSON のフィルター、単一ファイル選択、存在確認を行う。キャンセル時は null を返す。
所有者にはゲームスレッドの GetActiveWindow が返す Win32 HWND を使う。MonoGame の GameWindow.Handle は SDL_Window* のため渡さない。ダイアログを呼ぶエントリーポイントは STAThread を維持する。
画面本体は文房具 UI のままで、ファイルの選択部分だけ Windows に委譲する。

実際の OS ダイアログを使う検査：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Existing -NativeDialog -Dark
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Existing -NativeDialog -CancelDialog
```

ネイティブダイアログの自動操作は専用テスト環境変数が指定された場合だけ有効。同じプロセスのダイアログを確認してから、テスト用に選択済みのファイルを確定またはキャンセルする。通常起動では自動操作しない。

操作方法と対象範囲は [ユーザー向けガイド](../user/style-designer.md) を参照。
