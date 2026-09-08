# StationeryUI — 文房具UI

MonoGameでPC向けの設定画面・編集画面を作るための、文房具をモチーフにしたGUIライブラリーです。日本語IMEの入力接続、文字編集、アプリに合わせたテーマを提供します。MITライセンス。

初期版 **0.1.0**。公開APIは今後変更する可能性があります。

## できること

- 単一行テキスト入力、未確定文字の表示、選択、コピー・切り取り・貼り付け、Undo/Redo。
- 日本語・サロゲートペア・結合文字を考慮した編集モデル。
- 通常・ホバー・押下・選択・無効・フォーカスの配色、明暗テーマ、個別上書き。
- フォーカス順序、モーダル範囲、ポインター保持を扱う共通モデル。
- MonoGameの `Update` / `Draw` に組み込める `DesktopUi` サンプル用ホスト。
- ボタン、見出し、下線、数値・時間ポップアップ等の既存MonoGameコントロール。
- Windowsのテキスト描画・クリップボード・DesktopGLのIME入力接続。

Windows Formsと同じAPIやデザイナーを提供するものではありません。まずMonoGameの単一ウィンドウ内のGUIを対象とします。

## パッケージ

| パッケージ | 対象 | 内容 |
|---|---|---|
| `StationeryUI` | .NET 8以降 | OS・MonoGame非依存の編集・部品・入力・テーマ・座標モデル |
| `StationeryUI.MonoGame` | .NET 8以降 | DesktopGL 3.8.5.1への描画・入力接続 |
| `StationeryUI.Windows` | .NET 8 Windows以降 | Windows文字描画、クリップボード、SDL2入力・合成監視 |

初回の `.nupkg` はGitHub Releaseで配布します。NuGet.orgへの公開は未実施です。パッケージを任意のローカルフォルダーへダウンロードし、プロジェクトの `NuGet.Config` にそのフォルダーをソースとして追加してください。両利用アプリでは `LocalPackages/StationeryUI` を同梱しています。

```xml
<ItemGroup>
  <PackageReference Include="StationeryUI.MonoGame" Version="0.1.0" />
  <PackageReference Include="StationeryUI.Windows" Version="0.1.0" />
</ItemGroup>
```

Windowsのアプリは `net8.0-windows` 以降を対象とします。`StationeryUI` 自体を参照するためにWindowsは必要ありません。

## サンプルを動かす

Windows上で.NET SDK 10を使用する手順です（ライブラリーの対象は.NET 8）。SDL2などのネイティブ依存はMonoGameのNuGetパッケージから復元します。

```powershell
dotnet build StationeryUI.slnx -c Release
dotnet run --project samples/StationeryUI.Demo -c Release --no-build
```

入力欄へ日本語を入力できます。Tab/Shift+Tabで移動、Ctrl+A/C/X/V/Z/Yで編集、ボタンで明暗テーマと拡大率を変更できます。ウィンドウのサイズを変えると最初の入力欄が追従します。

## MonoGameへの組み込み

次は `Game` 派生クラス内での使用例です。

```csharp
using StationeryUI.MonoGame;
using StationeryUI.Windows;

private WindowsTextInputService input = null!;
private DesktopUi ui = null!;

protected override void LoadContent()
{
    input = new WindowsTextInputService(Window.Handle);
    ui = new DesktopUi(GraphicsDevice, input,
        family => new WindowsTextRasterizer(family));
    var name = ui.AddTextBox("name", new(24, 24, 600, 64), "名前", "こんにちは");
    ui.AddButton("apply", new(24, 120, 320, 64), "名前を反映",
        () => Window.Title = name.Editor!.Text);
    ui.Focus.Focus("name");
}

protected override void Update(GameTime gameTime)
{
    ui.Update(gameTime, IsActive, Keyboard.GetState(), Mouse.GetState());
    // ゲーム側の入力処理では ui.KeyboardConsumed / PointerConsumed を確認する。
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(DesktopUi.Convert(ui.Theme.Background));
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

個別の `Element.Theme`、`IconButtonModel.Theme`、MonoGameの `Button.Theme` で上書きできます。既存の `StationeryDrawingTools` にも `Theme` があります。テーマ項目のうち角丸・影は描画方式ごとの対応です。`DesktopUi` の入力欄・ボタンは下線主体の表示で、角丸・影を描画しません。

## 入力の所有権と対応範囲

`WindowsTextInputService` の引数は **DesktopGLの `SDL_Window*`** です。WindowsDXのHWNDを渡さないでください。OSとバックエンドの対応は別々です。

- 新規アプリは確定文字も未確定文字も `ITextInputService` から受け取ります。同じ入力欄へ `GameWindow.TextInput` を重ねて接続しないでください。
- `WindowsCompositionObserver` は既存アプリ向けの合成状態専用接続です。確定文字をMonoGameから受け取る既存方式を保つために残しており、新規ホストとは同時使用しません。
- SDL2のテキスト入力はプロセス内で1セッションを使用します。UIスレッドで開始・終了・破棄してください。
- `SetInputArea` はウィンドウ座標です。描画と同じ `UiViewport` で変換し、高DPIでバックバッファー座標とウィンドウ座標が異なるホストではその倍率も接続側で変換してください。
- 日本語はインストール済みのフォントで描画します。絵文字の描画はフォントとWindows描画方式に依存し、カラー絵文字・すべてのグリフを保証しません。
- Windows + DesktopGLでビルド・描画・ネイティブSDLイベント検査を対象とします。実IMEの候補選択・全DPI・タッチの実機検証は [検証記録](docs/validation.md) を参照してください。
- Linux/macOSのIME、WindowsDX、OS画面読み上げ、複数ウィンドウ、GUIデザイナーは未対応です。コアの非WindowsテストとUI全体の非Windows対応は区別します。

## 開発と検証

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
dotnet run --project tests/StationeryUI.Windows.Tests -c Release
dotnet pack StationeryUI.slnx -c Release -o artifacts/packages
```

[設計](docs/architecture.md) / [抽出元一覧](docs/extraction-inventory.md) / [検証記録](docs/validation.md) / [引き継ぎ計画](docs/implementation-plan.md) / [貢献方法](CONTRIBUTING.md)

きふわらべの碁2026とサークルスペースコーディネーターの実装から抽出・整理しています。元のライセンス表示と依存物については [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) を参照してください。

## コードを読む

[コントロールのプログラム解説](docs/control-guide.md) に、描画境界と部品別の説明をまとめています。
