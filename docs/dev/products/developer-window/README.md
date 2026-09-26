# 開発者ウィンドウ

将来の文房具UIエディターとの統合案は[検査と編集の統合計画](../ui-editor/inspection-integration-plan.md)を参照。以下は現行の F12 実装についての説明。

* 📖 [レイアウト・インスペクター](layout-inspector/switch-tree.md)

F12 の別ウィンドウを、StationeryUI のツリー・スプリットペーン・読み取り専用テキスト・ボタンで構成する。
Windows Forms の Form / TreeView / SplitContainer / TextBox による描画は廃止した。
Windows 接続の既存クリップボード実装には System.Windows.Forms.Clipboard を引き続き使用するが、ウィンドウの UI は MonoGame で描画する。

## 責務

| 部品 | 役割 |
|---|---|
| StationeryUI.Inspection.DeveloperInspectionModel | 完全パスによる選択、スナップショット更新、枝の開閉状態、詳細文字列 |
| StationeryUI.MonoGame.StationeryDeveloperView | ツリー、仕切り、詳細、コピーの描画と入力 |
| StationeryUI.Windows.StationeryDeveloperWindow | 検査対象側の Show / Update / Dispose、プロセス起動と通信 |
| samples/StationeryUI.Demo/InspectorGame.cs | 別プロセスでの MonoGame ウィンドウ、描画デバイスと入力サービス、開閉 |

検査対象と開発者ウィンドウは、それぞれ一つの MonoGame / SDL イベントループを持つ。
同じプロセス内で複数の Game と入力サービスを競合させず、元の画面と並べて操作できる構成。
状態はゲームスレッドでスナップショット化し、ランダム名・同一ユーザー限定の名前付きパイプで渡す。
JSON は通信の形式であり、スタイル設定ファイルには検査結果を書き込まない。

## ホストの組み込み

デモの Program.Main は次の引数を受け取る。

```text
--stationery-inspector <パイプ名>
```

引数を受け取った場合は通常のデモを作らず、InspectorGame を作る。
StationeryDeveloperWindow は実行中のプログラムをこの引数で起動する。dotnet 経由の起動時はエントリー DLL も渡す。
**以前の Windows Forms 版から移行するホストでは、この引数の分岐と InspectorGame 相当のホストが必要。**
表示面だけを自前の MonoGame ホストへ組み込む場合は StationeryDeveloperView を直接使える。

検査対象側では従来どおり Show(snapshot)、Update(snapshot)、Dispose() を呼ぶ。
ウィンドウが開いている間、デモは約 0.25 秒ごとに新しいスナップショットを作る。
通信が失敗した場合は LastError と Trace に理由を記録する。

InspectorGame は受け取ったデータをゲームスレッドへ渡し、StationeryDeveloperView.Refresh / Update / Draw を呼ぶ。
Refresh は座標・表示状態の更新だけならツリーを作り直さない。構造が変わった場合も、完全パスで選択と閉じた枝を復元する。
選択したノードが削除された場合は、残ったツリーの先頭へ選択を戻す。

## ［指でつまむ］機能の接続

`Show` / `Update` によるツリー表示に加え、検査対象のアプリ側でキャプチャー入力と桃色の枠の描画を接続する必要がある。NuGet パッケージを参照して開発者ウィンドウを開くだけでは、この接続は追加されない。

ゲームの `Update` で `CaptureEnabled` を確認し、クリック位置を `DeveloperCapture.HitTest` で検査して `SelectCaptured(hit.Path)` を呼ぶ。キャプチャー中は通常の UI・ゲーム操作への入力を抑止する。`Draw` では `SelectedPath` に一致する表示中の部品の `WindowBounds` を取り、画面の描画後に `StationeryUiHost.DrawInspectionOutline` で枠を重ねる。

コピーして組み込めるコードと枠が出ない場合の確認点は、[ライブラリー導入ガイドの［指でつまむ］機能](library-integration.md#f12-開発者ウィンドウと指でつまむ機能の組み込み)を参照。

## 開閉と寿命

F12 / Esc は開発者ウィンドウを隠す。検査対象で F12 を押すと同じウィンドウを再表示する。
閉じるボタンでプロセスが終了した場合は、次回 F12 で作り直し、最後に受け取った選択・開閉状態・分割比率を復元する。
親が Dispose されたときは通信を終了し、所有する子プロセスも終了させる。通信が切れた開発者ウィンドウは自分で終了する。
起動待ちにはタイムアウトを設け、接続できない子プロセスを残さない。

コピーは入力サービス経由で行い、失敗時はコピーボタンに再試行の案内を出す。
詳細は AddTextBlock による読み取り専用表示で、文字入力や削除を検査対象に反映しない。

## 通常操作のログ

`StationeryDeveloperView` は `DeveloperOperationLog` を所有し、`Update` ごとの入力と処理後の状態を比較して、変化したフレームを JSON Lines で記録する。従来の `STATIONERYUI_INSPECTOR_TEST_OUTPUT` による `report.json` は最新状態の検証用出力で、通常操作の履歴ではない。操作ログは検証用環境変数なしでも有効。

Windows の既定保存先は `%LOCALAPPDATA%\StationeryUI\Logs`。`STATIONERYUI_OPERATION_LOG_DIR` で変更できる。実際のパスは `view.OperationLog.FilePath`、保存失敗は `view.OperationLog.LastError` と `Trace` で確認できる。書き込み失敗でウィンドウ操作を停止しない。各行は直ちにフラッシュし、8 MiB で `.jsonl.1` へローテーションする（直前の一世代を保持）。

独自の開発者ウィンドウホストでも `StationeryDeveloperView.Update` を呼べば記録される。呼び出す直前に `view.OperationLog.HostIsActive = IsActive` を設定すると、実際の `Game.IsActive` と、`Update` に渡した `active`（ログの `After.InputEnabled`）を区別できる。設定しない場合 `HostIsActive` は `null`。合成入力を使うテストは `InputSource = "synthetic-test"` を設定する。デモの `InspectorGame` は両方を設定済み。

ログには UTC 時刻・プロセス ID・連番、マウス座標と各ボタン、ホイール差分、操作用キー、処理前後の選択・操作対象・フォーカス・枝の開閉・スクロール・仕切り・キャプチャー状態を含める。`HitPath` はポインター下の開発者 UI 部品（例：ツリー）、`SelectedPath` / `TargetPath` は検査対象の文房具の完全パス。`Before` は直前に観測した状態で、外部からのキャプチャー選択などによる変更も含む。ログだけでマウス操作が原因と断定しない。

背景への入力漏れを調べるときは、このログとゲーム側の入力ログを UTC 時刻で照合する。開発者ウィンドウのログのみでは、背景側の `IsActive` やクリック実行は分からない。

## 検証

コアでは DeveloperInspectionTests が、同名 Id の区別、ライブ座標、構造変更、削除、選択・開閉状態の保持を検査する。
Release デモをビルド後、Windows の GUI 環境で次を実行する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-DeveloperWindow.ps1
```

テストは検証用のスタイルコピーを作り、原本を変更しない。
F12 の起動とネイティブウィンドウの寿命を確認し、検証用入力で文房具 UI の選択・スクロール・コピー・仕切りを操作する。
STATIONERYUI_INSPECTOR_TEST_OUTPUT が指定された場合だけ、InspectorGame は検証用の入力・状態レポート・画像出力を有効にする。
出力先は artifacts/developer-window-stationery/。通常の起動ではこの検証機能を使用しない。
Windows Forms の UI Automation ツリーではなく、自前描画のスナップショットと画像を検証する。

## 開発者ウィンドウのスタイル設定

`StationeryDeveloperStyle.Load()` は専用 JSON を読み込み、`Settings`、`Width`、`Height`、`SplitOptions` を返す。ホストは作成時に幅・高さを GraphicsDeviceManager へ設定し、同じ Style を StationeryDeveloperView へ渡す。View は StationeryLayoutEngine で配置し、スプリットペーンの比率は実行中の操作を保持する。

読み込み先は次の優先順で決める。

1. `Load(filePath)` で明示したパス。
2. 環境変数 `STATIONERYUI_DEV_WINDOW_STYLE_PATH`。
3. 開発時の `StationeryDeveloperStyleSource` メタデータ、または `AppContext.BaseDirectory/App_Data/dev-window.stationery-ui.json`。
4. 外部ファイルが存在しない、または不正な場合は DLL 内の既定設定。

参照先は `FilePath`、読み込み失敗は `LastError` と Trace で確認する。必須モデルの種類、ツリーと詳細の split binding、インスペクターパネルの配置を検証する。読み込みはウィンドウ作成時に行い、一般の StationeryStyleFile によるオートリロードとは別の機能である。

現在のモデルは `/developerViewport/developerWindow` を作業ページとし、その下の `inspectorPanel/toolHint` に説明を表示する。ルートの viewport と作業ページを分けることで、本文の余白を除外した下端 80px を確保する。

`src/StationeryUI.MonoGame/StationeryUI.MonoGame.csproj` の EmbeddedResource により、既定 JSON を `StationeryUI.dev-window.stationery-ui.json` として DLL に含める。NuGet にもこの DLL が入るため、外部 JSON がなくても既定設定を読み込める。DeveloperStyleTests で埋め込み既定値、外部設定、異常時のフォールバックとリサイズを検査する。

[モデルと検査情報の接続](model-inspection.md) ／ [開発者向け目次](README.md)
