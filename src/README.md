# src フォルダー

* `StationeryUI.MonoGame` - `StationeryUI` と、`MonoGame フレームワーク` に依存しているコード
* `StationeryUI.Windows` - `StationeryUI` と、`Windows 固有機能` に依存しているコード
* `StationeryUI` - `C# net8.0` のコード

## 依存関係

矢印は、利用または参照する方向を表します。

```mermaid
flowchart LR
    user["ユーザー"] --> app["ユーザーアプリ"]
    app --> windows["StationeryUI.Windows"]
    app --> monogame["StationeryUI.MonoGame"]
    windows --> core["StationeryUI"]
    monogame --> core
```
