# 設計と境界

依存方向は `StationeryUI.MonoGame → StationeryUI` と `StationeryUI.Windows → StationeryUI`。両接続の生成と寿命管理はホストが行う。共通部分にはMonoGame型・ネイティブハンドル・Windows診断型を入れない。

## 初期版のAPI

- `UnderlineTextEditor`: UTF-16の位置を保持し、移動・削除では文字要素境界を使用する。最大長はUTF-16コード単位。Undoは最大100件。
- `TextInputUpdate`: 確定／合成と、合成中カーソル・選択長。位置はUTF-16で表す。
- `TextInputSession`: フォーカスと未確定文字を所有し、同じ入力処理内の連続する確定断片をまとめる。サービスを借用する。
- `ITextInputService`: 開始・終了・入力領域・キュー取得・クリップボード。初期版ではサークル側との互換を保つためクリップボードを同じ契約に含む。
- `ITextCompositionService`: サークル側の既存DesktopApplication契約との互換用。新規ホストには `ITextInputService` を使用する。
- `FocusManager`: rootとモーダル範囲内のTab順、無効化、ポインター保持。描画やネイティブフォーカスはホスト側で接続する。
- `StationeryTheme`: 不変な設定。個別指定→アプリテーマ→標準テーマの順。個々の描画方式で未対応の装飾はREADMEへ明記する。
- `StationeryUiHost`: 単一行入力・ボタンの小さなMonoGameホスト。描画をクリップし、長い入力欄を水平スクロールする。より複雑なアプリは共通モデルを直接接続できる。
- `ScreenCanvas` / `StationeryDrawingTools` / `Controls`: 既存画面の移行向けAPI。1920×1080仮想画面とSpriteFontを使用する経路を維持する。任意のSpriteFontを注入可能。
- `RasterTextRenderer`: ホストのSpriteBatchを使う動的文字描画。最大128項目の再利用キャッシュを持ち、追い出したテクスチャは `EndFrame()` で破棄する。必ず `SpriteBatch.End()` 後に呼び、まだ描画キューが参照しているテクスチャを破棄しない。

## Windows入力の2つの経路

新規ホストとサークル側は `WindowsTextInputService` を使う。SDLイベントを監視し、ウィンドウIDで絞り、ゲームスレッドでキューを読む。SDLが所有する拡張編集イベントの文字列を監視側で解放しない。停止時には監視と未処理入力を解除する。

きふわらべ側の既存画面は `WindowsCompositionObserver` を使う。Win32合成メッセージとSDL編集通知を監視する従来の方式を保ち、確定文字はMonoGameが通知する。Windows診断情報はWindowsプロジェクト内に限定し、アプリの古い通知形式への変換はアプリ側の短いアダプターが行う。

これは初回抽出で入力の退行を抑えるための移行措置。きふわらべの全入力欄を新規セッション方式へ置換する作業は、候補位置・変換確定の実機比較後に進める。

## アプリ側に残した責務

きふわらべの碁石・役割アイコン・対局結果表示、画面ごとの付箋配置、背景、診断付き入力ダイアログは、アプリ内の `KifuwarabeGo2026.StationeryUI` アダプタープロジェクトに残す。共通コントロールのコピーは持たず、`StationeryUI.MonoGame` を参照する。

サークル側は会場・机・参加者・最適化の業務処理と画面フローを保持する。既存 `CircleSpaceCoordinator.StationeryUI` プロジェクトは共通パッケージの参照窓口として残し、移行した部品のソースは保持しない。

## 入力単位と表示の制約

文字要素としての編集とフォントの表示能力は別である。Windowsラスタライザーは同じGDI+フォント設定で測定・描画する。全スクリプトの高度な組版、カラー絵文字、フォント自動フォールバックは初期版の保証対象に含めない。

既存 `TextBoxController` はMonoGameのキーリピート・複数行編集契約を維持する移行用コントローラー。新規モデルと独立した履歴を持つが、共通パッケージ内の一箇所で管理する。両者の公開API統合は互換性を確認してから行う。
# UI ホスト名の変更

MonoGame の UI ホストは `StationeryUI.MonoGame.StationeryUiHost`。旧 `DesktopUi` からクラス名とファイル名を変更した。利用側の型名、`new DesktopUi(...)`、`DesktopUi.Element` などは、それぞれ `StationeryUiHost` に置き換えて再ビルドする。描画・入力の動作は変更していない。
