# ライブラリーの組み込み

MonoGame アプリへ文房具 UI を接続する開発者向けの資料です。

## パッケージ

| パッケージ | 対象 | 内容 |
|---|---|---|
| `StationeryUI` | .NET 8以降 | OS・MonoGame非依存の編集・部品・入力・テーマ・座標モデル |
| `StationeryUI.MonoGame` | .NET 8以降 | DesktopGL 3.8.5.1への描画・入力接続 |
| `StationeryUI.Windows` | .NET 8 Windows以降 | Windows文字描画、クリップボード、SDL2入力・合成監視 |

v0.2.0 は NuGet.org から取得できます：[StationeryUI](https://www.nuget.org/packages/StationeryUI/0.2.0)、[StationeryUI.MonoGame](https://www.nuget.org/packages/StationeryUI.MonoGame/0.2.0)、[StationeryUI.Windows](https://www.nuget.org/packages/StationeryUI.Windows/0.2.0)。公開者は [Muzudho](https://www.nuget.org/profiles/Muzudho) です。

NuGet.org が有効なプロジェクトでは、以下の参照を追加して `dotnet restore` を実行します。`StationeryUI` は依存パッケージとして復元されます。NuGet.org のソース URL は `https://api.nuget.org/v3/index.json` です。ソースを制限する `NuGet.Config` や Package Source Mapping がある場合は、この３パッケージを NuGet.org から取得できる設定にしてください。

手動配布用の `.nupkg` は引き続き [GitHub Release v0.2.0](https://github.com/muzudho/StationeryUI/releases/tag/v0.2.0) から入手できます。既存アプリの `LocalPackages/StationeryUI` を使う運用も継続できます。

```xml
<ItemGroup>
  <PackageReference Include="StationeryUI.MonoGame" Version="0.2.0" />
  <PackageReference Include="StationeryUI.Windows" Version="0.2.0" />
</ItemGroup>
```

Windowsのアプリは `net8.0-windows` 以降を対象とします。`StationeryUI` 自体を参照するためにWindowsは必要ありません。

## リングメニューの配置（0.1.1）

CircleSpaceCoordinator から移植した `StationeryUI.Controls.RingMenuLayout` は、描画エンジンに依存せず、正方形のボタンを円周上に等間隔で配置します。先頭は上、以降は時計回りです。画面端では中心を補正し、小さい画面では全体を縮小して、ボタンの重なりとはみ出しを防ぎます。

```csharp
using StationeryUI.Canvas;
using StationeryUI.Controls;

var layout = RingMenuLayout.Create(
    new ScreenRectangle(100, 100, 44, 44), // 起点ボタンの領域
    1280, 720,                           // 使用可能な画面の幅と高さ
    5);                                  // キャンセルを含む項目数
var buttons = layout.Buttons.Select((bounds, index) =>
    new IconButtonModel(bounds, $"操作 {index + 1}")).ToArray();
// layout.Center と layout.Radius はリングの帯を描く際にも使用できます。
```

幅と高さは有限の正数、項目数は1以上を指定してください。メニューの開閉、操作実行、入力遮断、描画はホスト側が担当します。各ボタンに操作内容を示すアクセシブル名を付け、Tab・矢印キーで移動、Enter・Spaceで実行、Escで閉じる操作を接続してください。色は `StationeryButtonRenderer` と `StationeryTheme` で適用できます。囲碁・会場などのアプリ固有モデルや MonoGame・Windows への依存はありません。

既存の CircleSpaceCoordinator から移行する場合、`CircleSpaceCoordinator.ReusableControls.RingMenuLayout` を `StationeryUI.Controls.RingMenuLayout` に置き換えます。`Create` の引数と戻り値の構造は同じです。

## サンプルを動かす

Windows上で.NET SDK 10を使用する手順です（ライブラリーの対象は.NET 8）。SDL2などのネイティブ依存はMonoGameのNuGetパッケージから復元します。

```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project samples/StationeryUI.Demo -c Release --no-build
```

サンプルの操作は [利用者向けの案内](../user/demo.md)を参照してください。

## MonoGameへの組み込み

次は `Game` 派生クラス内での使用例です。

```csharp
using StationeryUI.MonoGame;
using StationeryUI.Windows;

private WindowsTextInputService input = null!;
private StationeryUiHost ui = null!;

protected override void LoadContent()
{
    input = new WindowsTextInputService(Window.Handle);
    ui = new StationeryUiHost(GraphicsDevice, input,
        family => new WindowsTextRasterizer(family));
    var name = ui.AddTextBox("name", new(24, 24, 600, 64), "名前", "こんにちは");
    ui.AddButton("apply", new(24, 120, 320, 64), "名前を反映",
        () => Window.Title = name.Editor!.Text);
    ui.Focus.Focus(name.Path);
}

protected override void Update(GameTime gameTime)
{
    ui.Update(gameTime, IsActive, Keyboard.GetState(), Mouse.GetState());
    // ゲーム側の入力処理では ui.KeyboardConsumed / PointerConsumed を確認する。
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background));
    ui.Draw();
    base.Draw(gameTime);
}

protected override void UnloadContent()
{
    ui.Dispose(); // UIは借りた入力サービスを破棄しない。
    input.Dispose();
    base.UnloadContent();
}
```

上記では通常のMonoGame名前空間 `Microsoft.Xna.Framework`、`Microsoft.Xna.Framework.Input` も必要です。エントリーポイントに `[STAThread]` を付けてください。

## 非アクティブなウィンドウのマウス入力

別ウィンドウや F12 開発者ウィンドウがアクティブな間は、背後のゲーム画面へのマウス入力を無視してください。`StationeryUiHost.Update` の `active` には所有する `Game.IsActive` を渡します。非アクティブ中も `Update(..., false, ...)` を呼ぶことで、ドラッグ・ポインターキャプチャーの解除と入力履歴の同期が行われます。`StationeryUiHost` はウィンドウのアクティブ状態を自動取得しないため、`true` 固定ではこの抑止が働きません。

**ゲーム側の独自入力も別途抑止が必要です。** `PointerConsumed` は非アクティブ時に `false` へ戻るため、それだけを見てゲームのクリック処理へ進まないでください。独自のプレビュー選択、ホイール処理、右クリック、ドラッグ、`GameComponent` やイベント経由の入力にも同じ条件を適用します。

以下は独自入力へ進む条件と履歴同期の例です。先の基本例の `Update` に組み込みます。`gameDragging` は利用アプリが持つドラッグ状態の例です。

```csharp
private MouseState previousGameMouse;
private bool gameWasActive;
private bool gameDragging;

protected override void Update(GameTime gameTime)
{
    var keyboard = Keyboard.GetState();
    var mouse = Mouse.GetState();
    var active = IsActive;
    ui.Update(gameTime, active, keyboard, mouse);

    if (!active || !gameWasActive)
    {
        gameDragging = false;
        previousGameMouse = mouse;
    }
    else if (!ui.PointerConsumed)
    {
        var clicked = mouse.LeftButton == ButtonState.Pressed
            && previousGameMouse.LeftButton == ButtonState.Released;
        var wheelDelta = mouse.ScrollWheelValue - previousGameMouse.ScrollWheelValue;
        // ここでゲーム側のクリック・ホイールなどを処理する。
    }

    previousGameMouse = mouse;
    gameWasActive = active;
    // GameComponent 側にも IsActive の確認を入れる。
    base.Update(gameTime);
}
```

非アクティブ中の履歴を更新し、復帰した最初のフレームも差分を基準化することで、別ウィンドウでのクリックやホイール移動を復帰後の操作として扱わないようにします。ドラッグの開始には押下の瞬間を使い、押されているだけで再開しないでください。F12 のキャプチャー中やモーダル表示中も、ゲーム側の入力へ進まないよう条件を組み合わせます。入力以外の更新・描画を止める必要はありません。

導入後は別ウィンドウをゲーム画面に重ね、クリック・ホイール・ドラッグで背後の UI が変化しないこと、ドラッグ途中の切り替えで操作がキャンセルされること、復帰直後に誤操作せず次のクリックから通常操作できることを確認してください。

## F12 開発者ウィンドウと［指でつまむ］機能の組み込み

NuGet パッケージを導入する際は、F12 開発者ウィンドウの［指でつまむ］（キャプチャー）機能も利用アプリへ接続してください。**開発者ウィンドウを表示するだけでは、元の画面のクリック判定と桃色の枠の描画は動きません。** アイコンはキャプチャーモードを切り替え、利用アプリが対象を判定して選択を通知し、枠を描画します。

まず [開発者ウィンドウのホスト組み込み](developer-window.md#ホストの組み込み)に従い、エントリーポイントの `--stationery-inspector <パイプ名>` 分岐と `InspectorGame` 相当のホストを用意します。NuGet の参照だけでは、このアプリ側の起動処理は追加されません。すでに F12 で開発者ウィンドウが開くアプリでは、次の入力・描画の接続を確認してください。

以下は、上の例の `ui` 一つを検査する場合です。`using StationeryUI.Inspection;` と `using System.Linq;` を追加し、フィールドと `Update` / `Draw` を次のように組み込みます。すでに `StationeryDeveloperWindow` を持つ場合は、そのインスタンスを使います。

```csharp
private readonly StationeryDeveloperWindow developerWindow = new();
private bool previousDeveloperKey;
private bool captureMouseDown;
private double inspectionElapsed;

protected override void Update(GameTime gameTime)
{
    var keyboard = Keyboard.GetState();
    var mouse = Mouse.GetState();
    var developerKey = keyboard.IsKeyDown(Keys.F12);
    if (IsActive && developerKey && !previousDeveloperKey)
        developerWindow.Show(ui.Inspect());
    previousDeveloperKey = developerKey;

    inspectionElapsed += gameTime.ElapsedGameTime.TotalSeconds;
    if (developerWindow.IsOpen && inspectionElapsed >= .25)
    {
        developerWindow.Update(ui.Inspect());
        inspectionElapsed = 0;
    }

    var captureDown = mouse.LeftButton == ButtonState.Pressed;
    if (developerWindow.CaptureEnabled && IsActive)
    {
        if (captureDown && !captureMouseDown)
        {
            var hit = DeveloperCapture.HitTest(ui.Inspect(), mouse.X, mouse.Y);
            if (hit is not null) developerWindow.SelectCaptured(hit.Path);
        }
        captureMouseDown = captureDown;
        // キャプチャー中は通常の UI 操作へ入力を渡さない。
        ui.Update(gameTime, false, keyboard, mouse);
        base.Update(gameTime);
        return;
    }
    captureMouseDown = captureDown;

    ui.Update(gameTime, IsActive, keyboard, mouse);
    // ゲーム側の通常入力処理もキャプチャー分岐より後に置く。
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background));
    ui.Draw();
    base.Draw(gameTime);

    // 画面の描画を終えた後、選択中の部品に桃色の枠を重ねる。
    if (developerWindow.IsOpen && developerWindow.SelectedPath is { } selected)
    {
        var entry = ui.Inspect().FirstOrDefault(e => e.Path == selected && e.Visible);
        if (entry?.WindowBounds is { } bounds)
            ui.DrawInspectionOutline(bounds);
    }
}

protected override void Dispose(bool disposing)
{
    if (disposing) developerWindow.Dispose();
    base.Dispose(disposing);
}
```

`LoadContent` / `UnloadContent` は上の例と共通です。`Inspect()` はゲームスレッドで呼びます。`DrawInspectionOutline` は内部で `SpriteBatch.Begin` / `End` を呼ぶため、アプリ側の `SpriteBatch.End` を済ませてから呼んでください。`base.Update` から入力を処理する `GameComponent` があるアプリでは、その入力処理もキャプチャー中は抑止します。

複数の `StationeryUiHost` やモーダル画面を持つ場合は、表示・クリック判定・枠の描画に同じ検査ツリーを使います。[デモの実装](../../samples/StationeryUI.Demo/Program.cs)の `InspectStationery()` が接続例です。モーダル表示中は `DeveloperCapture.HitTest(entries, mouse.X, mouse.Y, dialog.Path)` のように `scope` へダイアログの完全パスを渡し、背後の部品を選ばないようにします。

### 桃色の枠が出ない場合

- ［指でつまむ］を有効にし、元のアプリ画面をアクティブにして対象をクリックします。枠は選択した部品の範囲に表示されます。ポインターに追従するカーソルではありません。
- `CaptureEnabled` を読み、クリック時に `DeveloperCapture.HitTest` → `SelectCaptured` を呼んでいるか確認します。
- 対象が検査スナップショットにあり、`Visible` が `true`、`WindowBounds` が正の幅・高さを持つか確認します。自作描画の部品は [モデルと検査情報の接続](model-inspection.md)に従って登録します。
- マウス座標と `WindowBounds` は同じウィンドウ内のピクセル座標で照合します。UI の論理座標へ変換して渡したり、枠へ `Viewport.Scale` を二重に適用したりしないでください。
- `SelectedPath` に対応する部品へ `DrawInspectionOutline` を呼び、その後の画面クリアや描画で枠を覆っていないか確認します。

導入後は「F12 → ［指でつまむ］ → 元の画面の部品をクリック」で、桃色の枠とツリーの選択が一致し、そのクリックで通常のボタン操作が実行されないことを確認します。アイコンをもう一度押すと通常操作に戻ります。

## ドック配置

`dock-layout` は `top` / `right` / `bottom` / `left` を配列順に確保し、`center` が残りを使う配置です。方向の重複と任意個数の要素を扱えます。各要素の `size` を指定し、行・列定義は使いません。[設定例とエラー表示の接続](dock-layout.md)を参照してください。

## 外観の変更

```csharp
ui.Theme = StationeryUI.Theming.StationeryTheme.Light with
{
    Accent = new(30, 110, 190),
    FontFamily = "Meiryo",
    FontSize = 24,
    Padding = 14
};
ui.Viewport.Scale = 1.5;
```

個別の `Element.Theme`、`IconButtonModel.Theme`、MonoGameの `Button.Theme` で上書きできます。既存の `StationeryDrawingTools` にも `Theme` があります。テーマ項目のうち角丸・影は描画方式ごとの対応です。`StationeryUiHost` の入力欄・ボタンは下線主体の表示で、角丸・影を描画しません。

## 入力の所有権と対応範囲

`WindowsTextInputService` の引数は **DesktopGLの `SDL_Window*`** です。WindowsDXのHWNDを渡さないでください。OSとバックエンドの対応は別々です。

- 新規アプリは確定文字も未確定文字も `ITextInputService` から受け取ります。同じ入力欄へ `GameWindow.TextInput` を重ねて接続しないでください。
- `WindowsCompositionObserver` は既存アプリ向けの合成状態専用接続です。確定文字をMonoGameから受け取る既存方式を保つために残しており、新規ホストとは同時使用しません。
- SDL2のテキスト入力はプロセス内で1セッションを使用します。UIスレッドで開始・終了・破棄してください。
- `SetInputArea` はウィンドウ座標です。描画と同じ `UiViewport` で変換し、高DPIでバックバッファー座標とウィンドウ座標が異なるホストではその倍率も接続側で変換してください。
- 日本語はインストール済みのフォントで描画します。絵文字の描画はフォントとWindows描画方式に依存し、カラー絵文字・すべてのグリフを保証しません。
- Windows + DesktopGLでビルド・描画・ネイティブSDLイベント検査を対象とします。実IMEの候補選択・全DPI・タッチの実機検証は [検証記録](validation.md) を参照してください。
- Linux/macOSのIME、WindowsDX、OS画面読み上げ、複数ウィンドウ、GUIデザイナーは未対応です。コアの非WindowsテストとUI全体の非Windows対応は区別します。

## 関連資料

- [他アプリへのスタイル設定組み込みガイド](style-settings/README.md)：AI 向けの接続例、オートリロード、JSON の読み方、既存 C# からの段階的な移行。

- [インスペクターパネルとツールヒント欄](../user/inspector-panel-guide.md)：画面下の80pxに操作説明を集約する、おすすめのスタイルガイドライン（必須ではありません）。

- [下線付きテキストの見分け方](../user/underline-guide.md)
- [リストUI設計の目安](list-ui-guidelines.md)
- [配布パッケージのチェックサム](../user/package-checksums.txt)
- [抽出元のライセンス](../user/CircleSpaceCoordinator-LICENSE.txt)
- [開発者向けドキュメント](README.md)：ライブラリー自体の開発と検証。

きふわらべの碁2026とサークルスペースコーディネーターの実装から抽出・整理しています。元のライセンス表示と依存物については [THIRD-PARTY-NOTICES.md](../../THIRD-PARTY-NOTICES.md) を参照してください。

## コードを読む

[コントロールのプログラム解説](control-guide.md) に、描画境界と部品別の説明をまとめています。
