# 検証記録

2026-09-08。Windows、.NET SDK 10.0.400、MonoGame.Framework.DesktopGL 3.8.5.1。ライブラリーは.NET 8、Windows接続は.NET 8 Windowsを対象とする。

| 項目 | 結果 |
|---|---|
| StationeryUI単独のReleaseビルド | 成功（警告0・エラー0） |
| コアの自動検査 | 16/16成功 |
| Windows SDLイベント・寿命管理・文字描画 | 10項目成功 |
| サンプルの起動・描画・終了 | 成功。暗色100%・明色150%のPNGを保存して目視確認。OSのDPI変更とは別の検査 |
| 動的文字キャッシュの描画 | 同一SpriteBatchで140種類を描画し、End後の遅延破棄まで成功 |
| きふわらべの碁2026全体ビルド | 最終パッケージで成功（警告0・エラー0） |
| きふわらべの移植性・GTP/CGOS/SGF回帰検査 | 成功 |
| きふわらべのWindowsサービス非対話検査 | 成功 |
| サークルスペースコーディネーター全体ビルド | 成功（警告0・エラー0） |
| サークルの既存文房具UI検査 | 13/13成功 |
| サークルのデスクトップ検査 | 44/44成功 |
| Linuxでのコア検査 | [GitHub Actions](https://github.com/muzudho/StationeryUI/actions/runs/34235943090)で成功。Windowsジョブのビルド・テスト・packも成功 |
| 公開パッケージ | GitHub Releaseから3ファイルを再ダウンロードし、両アプリの同梱物とSHA-256一致を確認。値は [package-checksums.txt](../user/package-checksums.txt) |
| 実IMEの候補選択・変換確定 | 未実施。SDLイベントの自動検査は実IME操作の代用とはしない |
| Windowsの各DPI・複数モニター | 未実施。サンプル内の論理拡大率とOSのDPI切替は区別する |
| タッチ実機、WindowsDX、Linux/macOSのUI | 未検証・初期版の保証外 |

初回の作業用ビルドではサンドボックスからNuGet脆弱性情報を取得できずNU1900が出た。通常のネットワーク環境で独立リポジトリーを強制復元・ビルドした際は警告0で成功した。

## 2026-09-21 スタイル設定の検証

- ソリューションの Release ビルド、デモの Debug ビルドが成功（警告・エラー 0）。
- コア自動検査 18/18、Windows 自動検査 10 件が成功。
- スタイルの検査では、四辺と小数 px、初期値、不正値、保存途中の JSON、ファイル削除と復旧、原子的なファイル置換、同じ文字数の変更、オートリロード停止と明示的リロードによる再開を確認。
- Debug の原本パスの埋め込みと、Debug 出力／Release publish 出力の JSON が原本と SHA-256 一致することを確認。
- Release 配布物を別のカレントディレクトリから起動し、通常表示、上 40px・右 120px・下 400px・左 80px の表示、変更したパディングでのダイアログ保存操作の描画スモークが成功。
- `artifacts/style-smoke/default.png` と `padding.png` を目視確認。原本の四辺 8px と、非対称パディング・高さ不足時の縮小を確認。
- F5 の実キー操作、実行中のエディター保存による画面変化、実 IME の手動操作は未実施。リロードの状態遷移は自動検査で確認。

## 2026-09-21 起動時の日本語タイトル文字化け

- `Program.cs` は正常な UTF-8。Debug／Release のコンパイル済み文字列も日本語が正常だった。
- 修正前のデモを起動し、`GetWindowTextW` で `StationeryUI ? �z�o…` というタイトルを読み取って文字化けを再現。
- MonoGame 3.8.5.1 の [ウィンドウ再作成処理](https://github.com/MonoGame/MonoGame/blob/v3.8.5.1/MonoGame.Framework/Platform/SDL/SDLGameWindow.cs) は、コンストラクターで設定したタイトルを `SDL_CreateWindow` へ渡す。[SDL バインディング](https://github.com/MonoGame/MonoGame/blob/v3.8.5.1/MonoGame.Framework/Platform/SDL/SDL2.cs) の作成側は string の ANSI マーシャリングで、タイトル更新側は UTF-8 を明示している。
- コンストラクターでは ASCII の `StationeryUI` を使い、ネイティブウィンドウ作成後の `LoadContent` で日本語タイトルを設定するよう修正。ソースや JSON の文字コード変換は不要。
- Debug／Release ビルド成功（警告・エラー 0）。両方を起動して `GetWindowTextW` の結果が `StationeryUI — ホバーで EDIT / POPUP、クリックで編集` と完全一致することを確認。
- 検証スクリプトと取得したタイトルは `artifacts/title-check/` に保存。ウィンドウは検証後に終了。

## 2026-09-21 F12 開発者ウィンドウ

- Release ソリューション／Debug デモのビルド成功（警告・エラー 0）。コア自動検査 19/19 成功。
- 文房具 Id の許可文字、同一親の重複拒否、別ページの同名 Id、大小文字を区別する完全パスの解決と入力フォーカスの分離を検証。
- Release デモに Win32 キーメッセージで F12 を送り、開発者ウィンドウの開閉・同じウィンドウの再表示・開いたままのデモ終了を確認。
- Windows UI Automation で 2 つの `nameField` をそれぞれ選択し、詳細欄に `/demo/mainPage/nameField` と `/demo/editDialog/nameField` が表示されることを確認。
- 再実行用スクリプトは `tests/StationeryUI.Windows.Tests/Test-DeveloperWindow.ps1`。Release デモをビルドしてから、Windows の対話セッションで実行する。
- ツリーの日本語表示、非表示のダイアログ、完全パス、位置、コピー用ボタンを `artifacts/developer-window/inspector.png` で目視確認。
- フォーカスを完全パスへ移行後、通常ホバー・非対称パディング・ダイアログを開く／保存／キャンセルの 5 描画スモークが成功。
- 実 IME の手動入力、クリップボードの実コピー、他 OS での UI はこの検証の対象外。

## 2026-09-21 model / layout 形式への移行

- 旧トップレベル `viewport` を廃止し、`model` と `layout` を必須化。`type: viewport` は部品の種類として継続。
- model のノードを実際のコントロールへ結び付け、layout の viewport 設定から四辺のパディングを取得。現在の layout 対応範囲はルートへの 1 件のパディング設定。
- コア 20/20 成功。モデルの階層・重複 Id・参照先・廃止形式・デモの型検査・不正モデルからの復旧を検証。
- Release ソリューション／Debug デモのビルド成功（警告・エラー 0）。Debug と Release publish のスタイル JSON が原本とハッシュ一致。
- 実行中の検証用ファイルに `inputGroup` を追加し、F12 のパスが `/demo/inputGroup/nameField` に更新されることを Windows UI Automation で確認。layout の左余白を 64px にした結果も詳細欄の X=64 で確認。
- 型を不正にしたモデルへの変更後も、直前の正常なツリーが保たれることを確認。F12 の再表示と終了も成功。
- 検証用ファイル・スクリプト・画像は `artifacts/model-layout/`。原本を試験用の値に書き換えていない。

## 手動確認の手順

### models / layouts の配列化（2026-09-21）

- JSON のトップレベルを `models` / `layouts`、C# のプロパティを `Models` / `Layouts` に統一。現在は各配列に viewport 1 要素。
- 単数形の旧キー、旧オブジェクト形式、空配列、複数ルート、不正な要素を拒否し、リロード失敗時の正常状態維持と復旧を確認。
- 原本 JSON をテスト出力にコピーし、実際のデモ用モデルへ結び付けられることを自動検査に追加。コア 20/20 成功。
- Release ソリューション／Debug デモのビルド成功（警告・エラー 0）。Debug／Release publish の JSON が原本とハッシュ一致。
- この変更では GUI の再検証は実施せず、形式の読み込み・結び付け・リロードを自動検査で確認。

### スタイル読み込み設定の分離（2026-09-21 追補）

- `demo.stationery-config.json` に `styleFile` と `autoReload` を分離し、読み込み設定の監視を常時継続する方式に変更。
- コア 18/18 成功。`false` 中のスタイル変更が反映されず、設定を `true` に戻すだけで反映されることを、F5・再起動なしの Update 呼び出しで検証。
- 無効中のパス切り替え、相対／絶対パス、不正・欠落した読み込み設定からの復旧、切り替え先ファイルの後からの作成、無効状態での起動と明示的リロードも検証。
- Release ソリューション／Debug デモのビルド成功（警告・エラー 0）。Debug 出力と Release publish 出力への設定ファイルのコピーをハッシュ比較で確認。
- この追補では GUI の再検証は実施せず、監視の状態遷移をコアの実ファイルを使った自動検査で確認。

### 実機操作

1. `dotnet run --project samples/StationeryUI.Demo -c Release --no-build` を実行する。
2. 名前欄で日本語IMEを有効にし、「にほんご」を入力して「日本語」へ変換・確定する。未確定表示・候補位置・確定後の文字列を確認する。
3. 変換中にEscを押して取消し、確定文字が失われないことを確認する。変換確定のEnterで別のボタンが実行されないことも確認する。
4. Tab/Shift+Tab、クリック、選択ドラッグ、コピー・貼り付け、Undo/Redoを確認する。変換中に別ウィンドウへ移動し、戻った際に重複入力しないことを確認する。
5. 明暗テーマと拡大率を切り替え、長い文字列のスクロール、候補位置、クリック位置、リサイズ後の表示を確認する。
6. きふわらべとサークルの既存入力画面でも同じ操作を比較する。
7. OSバージョン、IME名、DPI、操作と結果をこの文書へ記録する。

## 描画スモーク

環境変数 `STATIONERYUI_SMOKE_PNG` にPNG出力先を設定すると、サンプルが5回描画して画像を保存・終了する。`STATIONERYUI_SMOKE_THEME=light` と `STATIONERYUI_SMOKE_SCALE=1.5` も使用できる。この実行はGUIデバイスが必要。
