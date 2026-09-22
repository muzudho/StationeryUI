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
