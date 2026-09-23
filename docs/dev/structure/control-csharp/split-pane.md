# スプリットペーン

## C# で利用する

コアの StationeryUI.Controls.SplitPane が、比率と二つのペーン・仕切りの矩形を計算する。
StationeryUiHost.AddSplitPane で splitPane モデルの文房具を作り、子の文房具を作ったあと、
BindSplitContent(splitElement, firstElement, secondElement) で接続する。
子の位置は毎フレーム再計算され、描画と入力に同じ矩形を使う。
Element.Split.Configure に SplitPaneOptions を渡すことでスタイルの更新を反映できる。

下線付きリンクは StationeryUiHost.AddLink で追加できる。クリックと Enter / Space はゲームスレッドでコールバックを実行する。
