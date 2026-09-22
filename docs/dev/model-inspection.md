# 文房具モデルと検査情報の接続

## C# で組み込む場合

`StationeryNode` を親から作り、`StationeryUiHost` の `root` に渡す。
`AddTextBox` / `AddButton` の `parent` を指定すれば、同じ StationeryUiHost 内にも入れ子のコンテナーを作れる。
この階層は識別のためのもので、コンテナーを追加してもレイアウトや座標は自動変更しない。

```csharp
var root = new StationeryNode("demo");
var page = root.AddChild("mainPage", "page");
var ui = new StationeryUiHost(graphics, input, rasterizerFactory, page);
var name = ui.AddTextBox("nameField", new(0, 0, 300, 64), "名前");
var found = root.Resolve("/demo/mainPage/nameField");
ui.Focus.Focus(name.Path);
```

`Element.Id` はローカル名、`Element.Path` は完全パス。`StationeryUiHost.Focus` の登録・選択・キャプチャも完全パスで管理する。
以前の `ui.Focus.Focus("name")` のような呼び出しは `ui.Focus.Focus(element.Path)` に移行する。
`StationeryUiHost.Inspect` はゲームスレッドで呼び、取得したスナップショットを `StationeryDeveloperWindow.Show` / `Update` に渡す。
複数の StationeryUiHost をまとめるときは、共有の外側のノードを含めて一つのツリーにする。
開発者ウィンドウは別プロセスの MonoGame ホストで動作し、ローカルの名前付きパイプでスナップショットを受け取る。
ゲームの UI オブジェクトを直接操作しない。ホストの起動方法は [開発者向けの組み込み手順](developer-window.md) を参照。

現時点で自動登録の対象は `StationeryUiHost` で生成したコントロール。
描画ヘルパーや個別の MonoGame コントロールは、利用アプリ側で対応する `StationeryNode` とスナップショットを登録する。

デモの F12 は、読み込んだ `models` の階層を表示する。デモの原本には `topDemoPage` と `splitPaneDemoPage` を置き、名前欄は `/demo/topDemoPage/nameField` になる。models と layouts の役割は [スタイル設定ガイド](../user/stationery-style-settings.md) を参照。

[開発者向け目次](README.md)
