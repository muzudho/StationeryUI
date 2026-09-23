# 配置結果をアプリへ渡す

## 他の MonoGame アプリへの組み込み

StationeryUI.Styling の StationeryStyleFile に、スタイル自体ではなく読み込み設定ファイルのパスを渡す。
Update からファイルの Update を呼び、Current と現在の描画領域を配置エンジンに渡す。

```csharp
styles.Update(gameTime.ElapsedGameTime);
var arranged = StationeryLayoutEngine.Arrange(
    styles.Current, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
var nameBounds = arranged.Bounds["/demo/topDemoPage/body/nameField"];
var innerBounds = arranged.ContentBounds["/demo"];
```

Bounds は margin を差し引いたモデルごとの外枠、ContentBounds はさらに box-layout のパディングを差し引いた内側。BorderBounds は box-layout の border が外へ広がる描画領域。いずれも完全パスをキーとする。
結果はウィンドウのピクセル座標。StationeryUiHost.Viewport に倍率・オフセットを設定している場合は、論理座標へ変換してから Element.Bounds に渡す。

Current.Models[0].CreateTree() でノードを作り、StationeryUiHost.AddTextBox / AddButton のノード指定版で結び付ける。
モデルの更新には RebindModel を使う。任意の独自アプリでは、配置対象とコードの役割の検証も行う。
StationeryStyleFile の validate コールバックに検証を渡すと、不正な設定の採用を防げる。

Configuration は監視方針、ConfigurationFilePath と FilePath はそれぞれの読み込み先。
layouts の配列順とモデルは独立しているため、Layouts[0] から対象モデルの設定を決めない。
