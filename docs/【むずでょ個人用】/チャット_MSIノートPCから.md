# チャット　＞　MSIノートPCから

わたしのリモートリポジトリーである https://github.com/muzudho/ から、  
StationeryUI リポジトリーをクローンしたぜ（＾▽＾）  


Visual Studio の開発環境を用意できなかったんで、  
わたしは GitHub の Webサイトのドキュメントを信用して指示するぜ（＾～＾）  


src フォルダーの下に  
* StationeryUI.MonoGame
* StationeryUI.Windows
* StationeryUI
と３つあるが、以下のような理解でいいのかだぜ（＾～＾）？    
* `StationeryUI.MonoGame` - MonoGame フレームワークに依存しているコード
* `StationeryUI.Windows` - Windows に依存しているコード
* `StationeryUI` - ピュアな C# のコード


じゃあ、以下の README.md ファイルを src ディレクトリーに置いてくれだぜ（＾▽＾）  
```md
# src フォルダー

* `StationeryUI.MonoGame` - `StationeryUI` と、`MonoGame フレームワーク` に依存しているコード
* `StationeryUI.Windows` - `StationeryUI` と、`Windows 固有機能` に依存しているコード
* `StationeryUI` - `C# net8.0` のコード
```
