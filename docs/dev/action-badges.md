# アンダーラインのアクションバッジ

## 移植状況（2026-09-19確認）

角丸バッジの共通部品は移植済みです。

- [ActionBadgeComponent](../../src/StationeryUI.MonoGame/Controls/ActionBadge/ActionBadgeComponent.cs)：ラベル、角丸背景、右端への配置、表示・非表示。
- [SinglelineTextUnderline](../../src/StationeryUI.MonoGame/Controls/SinglelineTextUnderline/SinglelineTextUnderlineComponent.cs)：コンストラクターの `actionBadgeLabel` で指定。`Bounds` を設定して `UpdatePointer`、`Draw` を呼びます。
- [LinkUnderline](../../src/StationeryUI.MonoGame/Controls/LinkUnderline/LinkUnderline.cs)：`SetActionBadge` で接続し、`UpdatePointer`、`Draw` を呼びます。領域変更時はバッジにも `SetAnchorBounds` を呼びます。

ラベルは任意の文字列です。ローカルの KifuwarabeGo2026 の `GtpEngineRenderer.GetGtpEngineOptionActionLabel` では、チェック項目に `TOGGLE`、数値・文字列に `EDIT`、選択肢に `SELECT`、ファイルに `CHANGE`、実行に `EXECUTE` を指定しています。確認した Presentation 配下には `POPUP` / `Popup` / `POP UP` のラベル指定はありませんでした。

バッジは操作の説明を描画する部品です。クリック後の編集やダイアログ表示はホスト側で接続します。`SinglelineTextUnderline.SetEditing` だけではバッジは隠れません。元アプリの一部の編集欄では、ホスト側で編集中の `EDIT` バッジを隠しています。

## デモへの接続

従来のデモは別のホストである `DesktopUi` のみを使用しており、バッジを接続していませんでした。[ActionBadgeOverlay](../../samples/StationeryUI.Demo/ActionBadgeOverlay.cs) で既存の `ActionBadgeComponent` を重ねて描画します。DesktopUi自体にバッジの自動表示機能を追加したものではありません。

- 名前とメモには `EDIT`、ダイアログを開く項目には任意ラベルの例として `POPUP` を指定します。
- 編集中の入力欄ではバッジを隠し、拡大率・リサイズに位置と大きさを追従させます。
- ダイアログ表示中は専用のDesktopUiだけに入力を渡し、背景の入力処理を止めます。切り替え時には前の入力セッションを終了します。
- ラベル用SpriteFontはインストール済みフォントから起動時に生成します。フォントファイルの配布やContentの事前ビルドは不要です。

## 描画・動作確認

リポジトリーのルートで実行します。GUIデバイスが必要です。

```powershell
dotnet build samples/StationeryUI.Demo -c Release
$env:STATIONERYUI_SMOKE_PNG = "$PWD/samples/StationeryUI.Demo/obj/badge.png"
$env:STATIONERYUI_SMOKE_CASE = "edit-hover"
dotnet run --project samples/StationeryUI.Demo -c Release --no-build
Remove-Item Env:STATIONERYUI_SMOKE_PNG, Env:STATIONERYUI_SMOKE_CASE
```

`STATIONERYUI_SMOKE_CASE` は `edit-hover`、`popup-hover`、`popup-open`、`popup-save`、`popup-cancel` を指定できます。保存・キャンセルはマウス操作を再現して結果を検査し、失敗時は例外で終了します。これは実IMEの候補選択を検査するものではありません。
