# スタイル設定エディターをソースから動かす

ソースからの起動：

```powershell
dotnet run --project samples/StationeryUI.StyleDesigner -c Release
```

リリース用の出力：

```powershell
dotnet publish samples/StationeryUI.StyleDesigner -c Release --self-contained false -o artifacts/release/style-designer
```

上記コマンドによるランタイム非同梱版は、出力フォルダー全体から `StationeryUI.StyleDesigner.exe` を起動する。こちらは Windows 用の .NET 8 Desktop Runtime が必要。
設計ツールは文房具 UI の DLL を利用するが、文房具 UI の NuGet パッケージに設計ツールは含めない。

[開発者向け目次](README.md)
