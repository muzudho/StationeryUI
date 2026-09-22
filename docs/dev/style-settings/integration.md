# 組み込みとパラメーターの渡し方

[目次](README.md)

このページはスタイルの読み込みと配置の接続例です。**F12 開発者ウィンドウを含む導入全体は、[一式の取り込み手順](full-integration.md)を参照してください。** 以下の最小例へ、同手順の InspectorGame・起動分岐・F12 とスナップショット更新・Dispose を追加すると、実画面のパスと座標も調べられます。

## 利用するプロジェクト

配置計算だけなら `StationeryUI`、MonoGame の描画・入力接続には `StationeryUI.MonoGame`、現在の Windows の文字描画・IME 接続には `StationeryUI.Windows` を使います。コアの配置計算は OS 非依存ですが、このページの実行例は **Windows + DesktopGL** 用です。

別リポジトリーに固定した StationeryUI のチェックアウトを置き、まず ProjectReference で接続できます。以下はアプリの `.csproj` から `../../vendor/StationeryUI` が見える構成の例です。実際の配置に合わせて変更してください。

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <LangVersion>12</LangVersion>
</PropertyGroup>
<ItemGroup>
  <ProjectReference Include="../../vendor/StationeryUI/src/StationeryUI.MonoGame/StationeryUI.MonoGame.csproj" />
  <ProjectReference Include="../../vendor/StationeryUI/src/StationeryUI.Windows/StationeryUI.Windows.csproj" />
</ItemGroup>
<ItemGroup>
  <None Update="App_Data/app.stationery-config.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
  <None Update="App_Data/app.stationery-style.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

これは SDK 形式のプロジェクト内にあるファイルをコピーする例です。プロジェクト外のファイルは `Include` と `Link` を使います。既存アプリが参照している MonoGame との重複・版違いも確認してください。パッケージ参照へ切り替える場合は、同じ API を含む版を使い、同じライブラリーの ProjectReference と PackageReference を混在させないでください。

## 2 ファイルを用意する

プロジェクト内の `App_Data/app.stationery-config.json`：

```json
{
    "styleFile": "app.stationery-style.json",
    "autoReload": true
}
```

`App_Data/app.stationery-style.json` は次の内容です。入力欄とボタンを上下に配置します。下の C# の `FallbackJson` も同じ設計図にしてあります。

```json
{
    "models": [
        {
            "id": "app",
            "type": "viewport",
            "children": [
                {
                    "id": "nameField",
                    "type": "textBox"
                },
                {
                    "id": "applyButton",
                    "type": "button"
                }
            ]
        }
    ],
    "layouts": [
        {
            "id": "mainGrid",
            "type": "grid-layout",
            "row-definitions": [
                "1rate",
                "64px"
            ],
            "column-definitions": [
                "1rate"
            ],
            "padding": {
                "top": "8px",
                "right": "8px",
                "bottom": "8px",
                "left": "8px"
            },
            "cells": [
                {
                    "row": 0,
                    "col": 0,
                    "slots": [
                        {
                            "id": "nameField"
                        }
                    ]
                },
                {
                    "row": 1,
                    "col": 0,
                    "slots": [
                        {
                            "id": "applyButton"
                        }
                    ]
                }
            ]
        }
    ],
    "bindings": [
        {
            "layout": "/mainGrid",
            "parentModel": "/app",
            "childrenModel": [
                {
                    "model": "nameField",
                    "slot": "nameField"
                },
                {
                    "model": "applyButton",
                    "slot": "applyButton"
                }
            ]
        }
    ]
}
```

## 最小の C# 接続例

以下は新しい `Game` 派生クラスとして使える例です。既存のアプリへ導入するときは `LoadContent` / `Update` / `Draw` / `UnloadContent` の各処理を既存ループへ統合します。初回読み込みに失敗しても起動できるよう、**アプリに必要なモデルを持つフォールバック**を渡しています。

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StationeryUI.Inspection;
using StationeryUI.MonoGame;
using StationeryUI.Styling;
using StationeryUI.Windows;

public sealed class StyledGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private WindowsTextInputService input = null!;
    private StationeryUiHost ui = null!;
    private StationeryStyleFile styles = null!;
    private StationeryStyleSettings applied = null!;
    private StationeryLayoutResult arranged = null!;
    private StationeryUiHost.Element name = null!, apply = null!;
    private bool previousF5;
    private string? reportedError;

    public const string FallbackJson = """
        {
          "models": [
            {
              "id": "app",
              "type": "viewport",
              "children": [
                {
                  "id": "nameField",
                  "type": "textBox"
                },
                {
                  "id": "applyButton",
                  "type": "button"
                }
              ]
            }
          ],
          "layouts": [
            {
              "id": "mainGrid",
              "type": "grid-layout",
              "row-definitions": [
                "1rate",
                "64px"
              ],
              "column-definitions": [
                "1rate"
              ],
              "padding": {
                "top": "8px",
                "right": "8px",
                "bottom": "8px",
                "left": "8px"
              },
              "cells": [
                {
                  "row": 0,
                  "col": 0,
                  "slots": [
                    {
                      "id": "nameField"
                    }
                  ]
                },
                {
                  "row": 1,
                  "col": 0,
                  "slots": [
                    {
                      "id": "applyButton"
                    }
                  ]
                }
              ]
            }
          ],
          "bindings": [
            {
              "layout": "/mainGrid",
              "parentModel": "/app",
              "childrenModel": [
                {
                  "model": "nameField",
                  "slot": "nameField"
                },
                {
                  "model": "applyButton",
                  "slot": "applyButton"
                }
              ]
            }
          ]
        }
        """;

    public StyledGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Window.Title = "Styled tool";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    public static void ValidateStyle(StationeryStyleSettings settings)
    {
        var root = settings.Models[0].CreateTree();
        if (root.Path != "/app" || root.Children.Count != 2)
            throw new JsonException("This example requires the app root and exactly two controls.");
        foreach (var (path, kind) in new[] {
            ("/app/nameField", "textBox"), ("/app/applyButton", "button") })
        {
            var node = root.Resolve(path);
            if (node?.Kind != kind || node.Children.Count != 0 ||
                !settings.Bindings.Any(b => b.Children.Any(c => c.ModelPath == path)))
                throw new JsonException($"Required control or cell binding is missing: {path}");
        }
        // Validate arrangement too, before accepting this snapshot.
        _ = StationeryLayoutEngine.Arrange(settings, 800, 600);
    }

    protected override void LoadContent()
    {
        var path = Path.Combine(AppContext.BaseDirectory,
            "App_Data", "app.stationery-config.json");
        styles = new StationeryStyleFile(path,
            StationeryStyleSettings.Parse(FallbackJson), ValidateStyle);
        applied = styles.Current;
        var root = applied.Models[0].CreateTree();
        input = new WindowsTextInputService(Window.Handle);
        ui = new StationeryUiHost(GraphicsDevice, input,
            family => new WindowsTextRasterizer(family), root);
        ui.UseStationeryButtons = true;
        name = ui.AddTextBox(root.Resolve("/app/nameField")!, default,
            "名前", "こんにちは");
        apply = ui.AddButton(root.Resolve("/app/applyButton")!, default,
            "タイトルに反映", () => Window.Title = name.Editor!.Text);
        apply.ToolHint = "入力した名前をウィンドウタイトルに反映します。";
        ApplyLayout();
        base.LoadContent();
    }

    private void ApplyLayout()
    {
        if (!ReferenceEquals(applied, styles.Current))
        {
            var root = styles.Current.Models[0].CreateTree();
            ui.RebindModel(root, element => root.Resolve(element.Path)!);
            applied = styles.Current;
        }
        arranged = StationeryLayoutEngine.Arrange(styles.Current,
            GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        // This example uses Scale = 1, Offset = (0, 0).
        name.Bounds = arranged.Bounds[name.Path];
        apply.Bounds = arranged.Bounds[apply.Path];
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var f5 = keyboard.IsKeyDown(Keys.F5);
        if (IsActive && f5 && !previousF5) styles.Reload();
        else styles.Update(gameTime.ElapsedGameTime);
        previousF5 = f5;
        if (styles.LastError != reportedError)
        {
            reportedError = styles.LastError;
            System.Diagnostics.Trace.WriteLine(reportedError ?? "Style reload succeeded.");
        }
        ApplyLayout(); // Resize also requires recalculation, even without a reload.
        ui.Update(gameTime, IsActive, keyboard, Mouse.GetState());
        // Gate other app input with ui.KeyboardConsumed / ui.PointerConsumed.
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(StationeryUiHost.Convert(ui.Theme.Background));
        ui.Draw();
        ui.DrawPanelBorders(arranged);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        ui.Dispose();
        input.Dispose();
        base.UnloadContent();
    }
}
```

エントリーポイントは `[STAThread]` 付きの `Main` で `using var game = new StyledGame(); game.Run();` を呼びます。`Window.Handle` は DesktopGL の SDL ウィンドウを前提とします。WindowsDX の HWND を同じ入力サービスへ渡すことはできません。

この例は Id と階層を固定する契約です。JSON で必須 Id を変更するとエラーとして採用しません。階層変更も受け付けるアプリでは、[デモの接続処理](../../../samples/StationeryUI.Demo/DemoModelBinding.cs) のように、新旧の業務上の役割とノードを対応付けてから `RebindModel` へ渡します。

## 引数・戻り値の使い分け

| API | 渡すもの／受け取るもの |
| --- | --- |
| `StationeryStyleSettings.Parse(json)` | JSON **文字列**。ファイルパスではない。エラーは呼び出し元で処理する |
| `new StationeryStyleFile(path, fallback, validate)` | **読み込み設定ファイル**のパス、起動用設定、アプリ固有の検証。コンストラクター内で初回読み込みする |
| `styles.Update(delta)` | `gameTime.ElapsedGameTime`。監視のため毎フレーム呼ぶ |
| `StationeryLayoutEngine.Arrange(settings, width, height)` | 現在の設定と使用可能な描画領域の幅・高さ。ウィンドウ枠を含めない |
| `settings.Models[0].CreateTree()` | JSON のモデルから `StationeryNode` ツリーを作る。UI コントロールは生成しない |
| `ui.AddTextBox(node, bounds, accessibleName, text, maximumLength)` | 同じホストのツリー内の `textBox` ノード、論理矩形、読み上げ用名、初期値、最大長（既定1024） |
| `ui.AddButton(node, bounds, label, clicked)` | `button` ノード、論理矩形、表示名、`Action`。データの保存などはこのコールバック側 |
| `ui.RebindModel(root, resolve)` | 新しいモデルツリーと、既存 Element に対応する新ノードを返す関数 |

JSON 由来のノードが既にある場合は、`AddButton("applyButton", ...)` などの **文字列 Id 版で同名ノードを追加せず、ノード指定版**を使います。文字列 Id 版はノード自体も作るため、兄弟 Id の重複になります。

`RebindModel` は同じ種類の既存コントロールを再接続し、入力テキスト・コールバック・フォーカス対象を保持します。新しい種類の部品を自動追加する API ではありません。必要ノードがない状態で呼ばず、先に `validate` で検証します。最小例では設定更新のたびに再接続しますが、実用アプリではモデル構造が変化した場合だけにするとドラッグの中断を減らせます。

## px とホストの論理座標

`Bounds` はモデルの外枠、`ContentBounds` は子の配置に使う内側、`BorderBounds` は枠線を外側に広げた領域です。キーはすべて `/app/nameField` のような完全パスです。通常の部品には `Bounds`、C# で子を配置するコンテナーには `ContentBounds` を使います。

`Arrange` の結果はピクセル座標、`Element.Bounds` はホストの論理座標です。倍率や原点が既定値でなければ、結果を変換します。

```csharp
var box = arranged.Bounds[element.Path];
var origin = ui.Viewport.ToLogical(new(box.X, box.Y));
element.Bounds = new(origin.X, origin.Y,
    box.Width / ui.Viewport.Scale, box.Height / ui.Viewport.Scale);
```

プレビューなど画面の一部分をビューポートにする場合、`Arrange` へはその領域の幅・高さを渡します。結果は `(0, 0)` 起点なので、領域の画面上の X/Y を足してから上記変換をします。クリッピングもアプリ側で接続してください。`Arrange` だけでは描画や入力のクリップを設定しません。

テーマ・表示文字列・ツールヒントは C# で渡します。`ui.Theme`、`element.Theme`、`element.Label`、`element.ToolHint` を使い、インスペクターの文言には `ui.Update` 後の `ui.HoveredToolHint` を表示します。`ToolHint` を設定するだけでは下部パネルは生成されません。[インスペクターの設計](../../user/inspector-panel-guide.md)も参照してください。
