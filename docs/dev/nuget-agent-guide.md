# NuGet を導入する AI エージェント向け手順

文房具 UI を他のリポジトリーへ組み込む際は、パッケージ参照の追加に続いて以下を確認してください。この文書は `StationeryUI`、`StationeryUI.MonoGame`、`StationeryUI.Windows` の各 NuGet パッケージへ同梱します。

## インストールした版の説明を読む

復元したパッケージのルートにある `README.md` と、この `docs/dev/nuget-agent-guide.md` を読みます。パッケージキャッシュの場所は `dotnet nuget locals global-packages --list` で確認できます。`NUGET_PACKAGES` やプロジェクト固有の復元先が指定されている場合は、その実際の復元先を使ってください。

同梱資料は、そのパッケージを作成した時点の内容です。以下を順に参照してください。

1. [ライブラリーの組み込み](library-integration.md)：基本の初期化・入力・描画と、［指でつまむ］機能のコード例。
2. [開発者ウィンドウ](developer-window.md)：`--stationery-inspector <パイプ名>` を処理する別プロセスのホストと通信。
3. [モデルと検査情報の接続](model-inspection.md)：検査対象の登録、完全パス、複数ホストの検査ツリー。

これらの資料が参照する追加資料やデモソースは、[ソースリポジトリー](https://github.com/muzudho/StationeryUI)で導入版に対応するタグを開いて確認してください。パッケージキャッシュ内のファイルを編集せず、接続コードは利用アプリのリポジトリーへ追加します。

## 非アクティブなウィンドウの入力を無視する

**別ウィンドウや F12 開発者ウィンドウを操作中に、背後のゲーム画面がマウス入力へ反応しないようにしてください。** `Mouse.GetState()` の値が取得できることを、操作を受け付けてよい根拠にしないでください。

- すべての `StationeryUiHost.Update` に、所有する `Game.IsActive` を反映した `active` を渡します。`true` 固定にしません。モーダルで入力を止める場合は、さらにその条件も組み合わせます。
- 非アクティブなフレームも `ui.Update(gameTime, false, keyboard, mouse)` を呼びます。ホストはクリック・ホイール・ドラッグを処理せず、進行中のドラッグとポインターキャプチャーを解除し、入力履歴を同期します。
- ゲーム側のクリック、右クリック、ホイール、ドラッグ、独自プレビュー、`GameComponent`、イベントハンドラーも `IsActive` で制御します。`ui.PointerConsumed` が `false` でも、非アクティブ中にゲームへ入力を流してはいけません。
- 独自入力の押下・解放・ホイール差分は非アクティブ中と復帰直後に同期し、ドラッグをキャンセルします。復帰時に別ウィンドウでの操作をまとめて実行しないでください。描画や入力以外の更新は継続できます。

実装例は [導入ガイドの非アクティブ時の入力](library-integration.md#非アクティブなウィンドウのマウス入力)を参照してください。導入時は、F12 開発者ウィンドウや別のウィンドウをゲームに重ね、クリック・ホイール・ドラッグで背後の選択・スクロール・操作が変化しないことと、ゲームへ戻った後の通常操作を確認して報告します。

## ［指でつまむ］機能まで接続する

F12 開発者ウィンドウを組み込む場合は、表示に加えて次の接続を行ってください。**アイコンが表示されるだけでは、元のアプリのクリック判定も桃色の枠の描画も動きません。** コアのモデルだけを利用するアプリには、このウィンドウの導入は不要です。

1. F12 で `StationeryDeveloperWindow.Show(snapshot)` を呼び、開いている間は `Update(snapshot)` で検査情報を更新します。別プロセスの起動引数を処理するホストも必要です。
2. 元の画面の `Update` で `CaptureEnabled` とアクティブ状態を確認します。左ボタンを押した瞬間に `DeveloperCapture.HitTest` を呼び、ヒットした完全パスを `SelectCaptured(hit.Path)` へ渡します。
3. キャプチャー中は UI を `ui.Update(gameTime, false, keyboard, mouse)` で更新し、ゲーム側や `GameComponent` 側の通常入力も抑止します。調べるためのクリックでボタンのアクションを実行しないでください。
4. `Draw` では `SelectedPath` に一致する表示中の部品を探し、`WindowBounds` を `ui.DrawInspectionOutline(bounds)` へ渡します。通常描画と `SpriteBatch.End` を終えてから枠を重ねます。
5. アプリの終了時に `StationeryDeveloperWindow.Dispose()` を呼びます。

検査スナップショットはゲームスレッドで取得します。クリック判定・ツリー表示・枠描画には同じ検査ツリーを使い、マウス位置と `WindowBounds` の座標系を揃えます。独自描画の部品も検査情報へ登録し、モーダル表示中は `HitTest` の `scope` を対象の完全パスに限定します。

## ドック配置と設定エラー

`dock-layout` を使う場合は [ドック配置の設定例](dock-layout.md)を読んでください。レイアウトの各 `slots` に `dock` / `size` を設定し、`center` には `remaining` を指定します。エラー時に代替配置する `StationeryStyleFile` を使うアプリは、子の描画後に `ui.DrawLayoutErrors(layoutResult)` を呼び、`LastError` と F12 の検査情報も更新してください。JSON 自体の構文エラーなどは直前の状態を維持します。

## ツリーへレイアウト情報を渡す

スタイル設定がある場合は `ui.Inspect(settings)` を使って検査データにレイアウト情報を含めてください。複数ホストをまとめる場合は結合後に `DeveloperInspectionLayout.Apply(entries, settings)` を呼びます。ツリーは `(文房具Id : 種類) (子としての配置 : 親としてのレイアウト)` で表示します。設定をリロードしたら最新の設定で検査情報も更新してください。詳しくは [モデルと検査情報の接続](model-inspection.md#ツリーノードのレイアウト表示)を参照してください。

## 開発者ウィンドウの操作ログ

`StationeryDeveloperView` は通常起動でも操作ログを自動記録します。独自ホストでは `Update` の直前に `view.OperationLog.HostIsActive = IsActive` を設定してください。Windows の既定保存先は `%LOCALAPPDATA%\StationeryUI\Logs`、実際のファイルパスは `view.OperationLog.FilePath` です。保存失敗は `LastError` で確認できます。詳細は [開発者ウィンドウのログ](developer-window.md#通常操作のログ)を参照してください。

背景へのクリック漏れの報告では、再現時刻の `.jsonl`（あれば `.jsonl.1` も）を読み、ゲーム側の入力ログと照合します。開発者側だけの記録から、背景側が入力を受け付けたと断定しないでください。

## 導入後の確認と報告

- F12 → ［指でつまむ］ → 元の画面の部品をクリックすると、ツリーの該当ノードが選ばれ、同じ部品に桃色の枠が出る。
- キャプチャー中のクリックでは通常のボタン操作が実行されず、アイコンを再度押すと通常操作に戻る。
- 移動・リサイズ後も枠が部品に一致し、非表示の部品を選ばない。

実装箇所と確認結果を利用者へ報告してください。GUI を操作できず実機確認していない場合は、その点を明記します。枠が出ない場合の確認点は [ライブラリーの組み込み](library-integration.md#桃色の枠が出ない場合)にあります。

スタイルを生成・編集する AI エージェントは、１ノードにつきルートレイアウトを最大１つにしてください。box / grid / dock はどれも `padding` を持てます。複合配置はレイアウトの `children` または子コンテナーでネストします。同じノードに余白用 box と配置用 grid を別ルートとして重ねないでください。省略値と移行方法は [ドック配置の仕様](dock-layout.md)を参照してください。

配置は layouts の slots に定義してください。grid の row / col / rowspan / colspan、dock の dock / size を bindings に書かないでください。bindings.childrenModel は slot と model の参照だけです。旧形式の移行と枠の検証規則は [配置枠とモデルの対応](dock-layout.md#配置枠とモデルの対応)を参照してください。


`bindings.layout` は `/frame/grid/inner` のように、先頭 `/` 付きのスラッシュ区切り絶対パスを指定します。最上位も `/mainGrid` と書きます。レイアウトの `id` は `mainGrid` のようなローカル名のままです。旧ドット区切り、先頭 `/` の省略、末尾 `/`、空の区間（`//`）、`.` / `..` は受け付けません。旧設定は `frame.grid` → `/frame/grid` と置き換えてください。モデル参照の既存ルールと slot のローカル Id は変更しません。
