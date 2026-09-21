# アンダーラインのアクションバッジ

## 表示ガイドライン

文房具 UI の下線は、編集やページ移動など、操作できるテキストを表します。
操作の種類はテキスト色とホバー時の角丸バッジで伝えます。
利用者への説明は [下線付きテキストの見分け方](../user/underline-guide.md) にまとめています。

標準のバッジは次の三種類とし、ラベルの大文字・小文字もそろえます。

| 操作 | 標準ラベル | テキスト色 | 下線との関係 |
|---|---|---|---|
| その場で編集を始める | `Edit` | ライトモードは黒系、ダークモードは白系 | 下線がフォーカス色になっても本文の文字色は保つ |
| ポップアップ／ダイアログを開く | `Popup` | 値を編集する項目では編集可能テキストと同じ基本色 | ページ移動用の配色にはしない |
| 別ページへ移動する | `Move` | 下線と同じ色 | 通常時・ホバー時・フォーカス時ともテキストと下線の色をそろえる |

ラベルはクリック直後の動作で選びます。ダイアログを開いてから編集する場合は `Popup`、
同じ画面で編集を始める場合は `Edit` です。戻るリンクも `Move` にします。
`Move` をドラッグ移動やスプリットペーンの仕切り操作には使いません。
移植元の独自ラベルを、そのまま文房具 UI の標準ラベルとして増やさない方針です。

### 配色とホバー表示

- 編集テキストにはテーマの本文色（`StationeryTheme.Text`）を使います。純黒・純白への固定ではなく、ライトは黒系、ダークは白系で背景に対して読みやすくします。
- ページ移動リンクでは、テキストと下線に同じ色を使います。`DesktopUi.AddLink` では両方に `StationeryTheme.Accent` を使っています。個別テーマや状態色を追加するときも、この対応を保ちます。
- 操作対象にカーソルが重なったとき、その対象に対応した角丸バッジを表示します。対象の右端に配置し、配置変更・スクロール・表示倍率に追従させます。
- その場で編集中の `Edit` バッジは隠します。非表示ページやモーダルの背後など、操作できない対象にもバッジを表示しません。
- バッジは操作の説明です。バッジ自体に別のクリック動作を割り当てず、対象のテキストが持つ操作と一致させます。
- 色だけに区別を任せず、バッジの文字でも動作を伝えます。既存のキーボード操作・フォーカス表示も維持します。

### ガイドラインと現在の実装

現時点のデモは `EDIT` / `POPUP` を渡しており、`Move` のホバーバッジは未接続です。
今後の実装では `Edit` / `Popup` / `Move` に統一し、トップ／スプリットペーンの往復リンクにも `Move` を接続します。
スプリットペーン内の編集欄なども、編集可能テキストとしてバッジの接続対象です。
今回の変更は表示規約の文書化であり、以下の移植状況やデモの実装状態とは区別します。

## 移植状況（2026-09-19確認）

角丸バッジの共通部品は移植済みです。

- [ActionBadgeComponent](../../src/StationeryUI.MonoGame/Controls/ActionBadge/ActionBadgeComponent.cs)：ラベル、角丸背景、右端への配置、表示・非表示。
- [SinglelineTextUnderline](../../src/StationeryUI.MonoGame/Controls/SinglelineTextUnderline/SinglelineTextUnderlineComponent.cs)：コンストラクターの `actionBadgeLabel` で指定。`Bounds` を設定して `UpdatePointer`、`Draw` を呼びます。
- [LinkUnderline](../../src/StationeryUI.MonoGame/Controls/LinkUnderline/LinkUnderline.cs)：`SetActionBadge` で接続し、`UpdatePointer`、`Draw` を呼びます。領域変更時はバッジにも `SetAnchorBounds` を呼びます。

API は任意のラベル文字列を受け取れますが、標準の表示規約は上記の三種類です。移植元のローカル KifuwarabeGo2026 の `GtpEngineRenderer.GetGtpEngineOptionActionLabel` では、チェック項目に `TOGGLE`、数値・文字列に `EDIT`、選択肢に `SELECT`、ファイルに `CHANGE`、実行に `EXECUTE` を指定していました。確認した Presentation 配下には `POPUP` / `Popup` / `POP UP` のラベル指定はありませんでした。これは移植時の記録であり、今後のラベル選択基準ではありません。

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
