# スタイル設定と F12 開発者ウィンドウを一式で取り込む

[目次](README.md)

別リポジトリーの **Windows + MonoGame DesktopGL** アプリへ、スタイル設定・リロード・F12 開発者ウィンドウをまとめて導入する手順です。2026-09-22 の実装を基準にしています。すでにスタイル設定を導入済みなら、手順 1 の版を確認してから手順 3 以降を追加します。

**ライブラリー参照と JSON のコピーだけでは F12 は動きません。** 現在の実装は、実行中のアプリ自身を `--stationery-inspector <パイプ名>` 付きで別プロセスとして起動します。受け取り側の起動分岐と `InspectorGame` は利用アプリへ組み込む必要があります。

## 1. 取り込むものと版をそろえる

StationeryUI のコミットを固定し、次を同じ版から取り込みます。既存の古い NuGet パッケージにすべて入っていると仮定しないでください。

| 取り込むもの | 役割・配置先 |
| --- | --- |
| `StationeryUI` | スタイル解析・配置計算・Inspection のデータ型。下記プロジェクトから参照される |
| `StationeryUI.MonoGame` | `StationeryUiHost`、`StationeryDeveloperView`、`StationeryDeveloperStyle`、開発者ウィンドウ用の埋め込み JSON |
| `StationeryUI.Windows` | 文字描画・IME・クリップボードと `StationeryDeveloperWindow`（親側のプロセス起動・通信） |
| 利用アプリの `App_Data/app.stationery-config.json` | スタイルの読み込み先と監視方針 |
| 利用アプリの `App_Data/app.stationery-style.json` | アプリ固有の models / layouts / bindings |
| [InspectorGame.cs](../../../samples/StationeryUI.Demo/InspectorGame.cs) | **サンプル側にある子プロセスのホスト。利用アプリのソースへコピーする** |
| 利用アプリのエントリーポイント | `--stationery-inspector` の分岐を追加する |
| 利用アプリの Game クラス | F12、スナップショット更新、終了時の Dispose を追加する |
| [dev-window.stationery-style.json](../../../App_Data/dev-window.stationery-style.json) | F12 自身の見た目を編集する場合に、利用アプリの `App_Data` へコピーする。標準表示だけなら DLL 内の既定設定で動く |

ProjectReference の場合は、固定したチェックアウト全体を `vendor/StationeryUI` などへ置きます。`src` だけを抜き出すと、`Directory.Build.props` や `App_Data/dev-window.stationery-style.json` の埋め込み参照が欠けます。NuGet を使う場合も `InspectorGame.cs` とエントリーポイントは別途必要です。StyleDesigner の実行ファイルは人間が JSON を編集するための道具で、F12 の子プロセスホストにはなりません。

## 2. スタイル読み込みと実画面への適用を接続する

[組み込みとパラメーターの渡し方](integration.md)に従い、プロジェクト参照・2 つの JSON・`StyledGame` 相当の処理を用意します。[オートリロードと配布](reload-and-files.md)も合わせて確認します。

1. `StationeryStyleFile` へ設定ファイルのパス、フォールバック、アプリ固有の検証を渡します。
2. JSON のモデルからツリーを作り、既存コントロールをノード指定で登録します。
3. Update で監視または F5 の Reload を行い、検証済みのモデルへ必要に応じて再接続します。
4. 現在の画面サイズで Arrange し、倍率・原点を考慮して実際の Bounds へ適用します。

ここまでで配置とリロードが動きます。次の手順で、その実行中の階層・パス・座標を F12 から調べられるようにします。

## 3. 子プロセスのホストと起動分岐を追加する

固定した版の [InspectorGame.cs](../../../samples/StationeryUI.Demo/InspectorGame.cs) を利用アプリのプロジェクト内へコピーし、必要なら名前空間を合わせます。SDK 形式なら通常は `.cs` が自動でコンパイル対象になります。デモのプロジェクト自体を参照する必要はありません。

このファイルには、名前付きパイプの受信、`StationeryDeveloperView.Refresh / Update / Draw`、SDL の表示・非表示、切断時の終了が含まれます。まずファイル全体を取り込むと接続処理の漏れを防げます。サンプルの検証用処理は `STATIONERYUI_INSPECTOR_TEST_OUTPUT` を指定した場合だけ有効になるため、通常起動ではこの環境変数を設定しません。

既存のエントリーポイントを次の形に変更します。`StyledGame` は利用アプリの Game 型へ置き換えます。トップレベルステートメントを使っている場合も、起動処理をこの分岐に統合してエントリーポイントを一つにします。

```csharp
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--stationery-inspector")
        {
            using var inspector = new InspectorGame(args[1]);
            inspector.Run();
            return;
        }

        using var game = new StyledGame();
        game.Run();
    }
}
```

この分岐は、通常画面の生成・業務データの読み書き・単一起動チェックより先に行います。子プロセスが通常のアプリ起動処理へ進むと、画面の二重起動や接続タイムアウトになります。親と子で別々の MonoGame / SDL イベントループを持つ設計なので、親プロセス内で二つ目の Game を起動する形には変更しません。

## 4. 親画面に F12・更新・終了処理を追加する

[最小接続例の StyledGame](integration.md)には以下を追加します。`using StationeryUI.Windows;` と `using Microsoft.Xna.Framework.Input;` は同例に含まれています。

フィールド：

```csharp
private readonly StationeryDeveloperWindow developerWindow = new();
private bool previousF12;
private double inspectionElapsed;
private string? reportedDeveloperError;
```

以下のメソッドを追加し、既存の Update 内で **ApplyLayout と ui.Update の後、base.Update の前**に `UpdateDeveloperWindow(gameTime, keyboard);` を呼びます。配置が小さい場合などの早期 return でも、F12 と開発者ウィンドウへの更新が止まらない位置に置きます。

```csharp
private void UpdateDeveloperWindow(GameTime gameTime, KeyboardState keyboard)
{
    var f12 = keyboard.IsKeyDown(Keys.F12);
    if (IsActive && f12 && !previousF12)
        developerWindow.Show(ui.Inspect());
    previousF12 = f12;

    inspectionElapsed += gameTime.ElapsedGameTime.TotalSeconds;
    if (developerWindow.IsOpen && inspectionElapsed >= 0.25)
    {
        developerWindow.Update(ui.Inspect());
        inspectionElapsed = 0;
    }

    if (developerWindow.LastError != reportedDeveloperError)
    {
        reportedDeveloperError = developerWindow.LastError;
        System.Diagnostics.Trace.WriteLine(
            reportedDeveloperError ?? "Inspector connection succeeded.");
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing) developerWindow.Dispose();
    base.Dispose(disposing);
}
```

既存の Dispose があればそこへ統合します。元の `UnloadContent` にある UI と入力サービスの Dispose も残します。F12 は押した瞬間だけ処理し、Debug / Release の両方で有効にします。

`ui.Inspect()` は **ゲームスレッド**で呼びます。得られたスナップショットを Show / Update へ渡し、UI オブジェクトを通信スレッドから読みません。F12 がフォーカスを持っている間も親の Update は続け、`IsActive` を理由に送信を止めないでください。

### 複数ページ・ダイアログ・独自描画がある場合

上の例は、常に表示される単一ホスト用です。複数ホストなら [Demo.InspectStationery](../../../samples/StationeryUI.Demo/Program.cs) の構成を参考に、Show と Update の両方へ統合したスナップショットを渡します。

- 共有ルートの下に、各ページやダイアログのノードを置きます。
- 各ホストの `Inspect(visible: ページ等の実際の表示状態)` を呼びます。閉じたダイアログは除外せず、非表示として含めます。
- 完全パスでまとめ、共有の外側のノードも含めます。同じ短い Id で統合しないでください。
- `Inspect` はホストに登録した Element の矩形をウィンドウ座標へ変換します。Element を持たないコンテナーの矩形は null なので、必要なら実際の配置から補います。
- 既存 UI や独自描画は自動収集されません。対応する `StationeryNode` と `StationeryInspectionEntry`（Id、Path、ParentPath、Kind、Label、Visible、WindowBounds）を用意し、実際の表示状態とウィンドウ内の px 座標を登録します。

JSON の Arrange 結果だけを表示しても、実画面への適用漏れは検出できません。調査対象のコントロールへ適用した後の矩形を送ります。F12 はモデル階層と実行時の情報を確認する画面であり、layouts 専用ツリーやスタイル編集画面ではありません。作業ページ下部のヒント用インスペクターパネルも別機能です。

## 5. 開発者ウィンドウ用の設定と配布を確認する

標準設定は `StationeryUI.MonoGame.dll` に埋め込まれています。F12 自身のサイズ・余白・行列・初期分割を調整する場合は、同じ版の `App_Data/dev-window.stationery-style.json` を利用アプリのプロジェクトへコピーし、次を `.csproj` に追加します。

```xml
<ItemGroup>
  <None Update="App_Data/dev-window.stationery-style.json"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

アプリの config / style のコピー設定は [手順 2 の参照先](integration.md)のまま残します。プロジェクト外のファイルを使う場合は `Include` と `Link` に変更します。F12 用 JSON は `app.stationery-config.json` の `styleFile` に指定せず、独立して読み込みます。

`StationeryDeveloperStyle.Load()` の読み込み先は次の順で決まります。

1. `Load(filePath)` に明示したパス。
2. 環境変数 `STATIONERYUI_DEV_WINDOW_STYLE_PATH`。
3. Debug のライブラリーに付いた `StationeryDeveloperStyleSource` メタデータの原本パス。メタデータがなければ `AppContext.BaseDirectory/App_Data/dev-window.stationery-style.json`。

選ばれた外部ファイルがなければ埋め込み設定を使い、不正な場合も埋め込み設定へ戻ります。不正な設定の理由は `StationeryDeveloperStyle.LastError` と Trace で確認します。親側の `StationeryDeveloperWindow.LastError` は起動・通信エラー用です。

Debug で利用アプリ側のコピーを確実に使いたい場合は、上記環境変数にその絶対パスを指定するか、コピーした InspectorGame の `StationeryDeveloperStyle.Load(...)` に明示します。Debug の原本が見つからない場合、次の外部候補を探すのではなく埋め込み設定へ戻る点に注意してください。

F12 用 JSON は **InspectorGame 生成時だけ**読み込みます。オートリロードや親画面の F5 の対象ではありません。変更後は開発者ウィンドウを閉じるボタンで終了し、親画面の F12 で再生成します。F12 / Esc で隠して再表示するだけでは読み直しません。

配布には通常の publish 出力一式（MonoGame / SDL などの依存ファイルを含む）を使います。DLL だけを渡して完了にせず、配布先でも同じアプリの子プロセスが起動できることを確認します。

## 6. 一式を取り込めたか確認する

- [ ] スタイル変更・F5・オートリロード・リサイズが実画面へ反映され、入力中の内容が保たれる。
- [ ] Debug / Release の両方で、親画面の F12 から開発者ウィンドウが開く。通常のアプリ画面は増えない。
- [ ] 実際の models の階層・完全パス・種類・表示状態が一致する。未表示のページやダイアログも確認できる。
- [ ] スタイル変更、拡大率変更、リサイズ後の座標が更新される。複数ホストや独自部品も取り込み対象を確認する。
- [ ] パスのコピー、詳細のスクロール、枝の開閉、仕切りのドラッグが動く。
- [ ] F12 / Esc で隠して再表示でき、選択・開閉状態・分割位置を保つ。閉じるボタンで終了しても F12 で再作成できる。
- [ ] 親アプリ終了時に子プロセスが残らない。接続失敗は LastError / Trace から調べられる。
- [ ] publish 出力を別の場所へ置き、別のカレントディレクトリから起動してもスタイル読み込みと F12 が動く。
- [ ] F12 用の外部 JSON を配布する場合、出力に含まれ、子プロセスの再生成で変更が反映される。

F12 が開かないときは、まず起動引数の分岐、InspectorGame のコンパイル対象への登録、単一起動チェックの順番、`StationeryDeveloperWindow.LastError` を確認します。階層や座標が欠けるときは、スナップショットの収集対象と送信時点を確認します。

操作の詳細は [F12 開発者ウィンドウ](../../user/developer-window.md)、通信と寿命は [開発者向けの説明](../developer-window.md)を参照してください。
