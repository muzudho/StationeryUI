# 配布で忘れないこと

2026-09-21 の配布作業と既存ソースに基づく運用メモです。

## アプリとライブラリーの版

設計ツールは独立したアプリで、`IsPackable=false` です。版番号はアプリの csproj で管理します。共通の `Directory.Build.props` のライブラリー版を、アプリ公開のためだけに変更しません。

名称変更後のタグは `ui-editor-v<版>`、ZIP は `StationeryUIEditor-v<版>-win-x64.zip` です。公開済み v0.4.0 以前の `style-designer-v<版>` タグと `StationeryUI.StyleDesigner-v<版>-win-x64.zip` はそのまま保存します。ライブラリー用の `v<版>` タグとは別です。

## 同梱物

EXE 単体ではなく、DLL、ランタイム、README、LICENSE、THIRD-PARTY-NOTICES、`licenses/` を含むフォルダー全体を ZIP にします。利用者にも新しいフォルダーへ全体を展開して起動するよう案内します。

v0.1.0 は Windows x64、.NET 8.0.30 同梱でした。この番号を将来の推奨値とはみなさず、次回の採用版はその時点で確認・固定・記録します。依存パッケージを変更したらライセンス本文と第三者通知も更新します。OpenAL 関連ではパッケージ側の通知と OpenAL Soft の COPYING の両方を確認します。

[アプリの csproj](../../../samples/StationeryUIEditor/StationeryUIEditor.csproj) と [THIRD-PARTY-NOTICES](../../../THIRD-PARTY-NOTICES.md) が同梱物の確認元です。

## 自動検査の範囲

[発行スクリプト](../../../scripts/Publish-StationeryUIEditor.ps1) は EXE の FileVersion、ランタイム同梱設定、主要ファイルの存在、拡張子、JSON 名、ZIP 展開後の各ファイルのハッシュを検査します。

許可した `.txt` や `.md` の内容まで公開可否を判定するわけではありません。すべてのライセンス、ソース履歴、GUI 動作、別 PC での起動、署名状態も自動保証しません。個人用メモ、編集対象の JSON、ログ、画像、証明書・秘密鍵の混入は別途確認します。`build-record.json` は絶対パスを含む作業記録なので通常は添付しません。

テスト出力は `artifacts/ui-editor-test/` に保存されます。配布フォルダーへコピーしません。テスト用環境変数 `STATIONERYUI_EDITOR_TEST_*` は通常起動や利用者の環境へ設定しません。

## 署名

今回の公開では自作 EXE は未署名とし、本文に明記しました。MonoGame、SDL2、OpenAL など第三者の DLL／EXE には独自署名を追加せず、提供元の状態を維持します。今後署名する場合も、自作ファイルの明示的な一覧を対象にします。

CircleSpaceCoordinator のローカル自己署名・証明書の信頼登録を、そのまま一般配布へ持ち込みません。利用者への証明書の信頼登録は配布手順に含めません。継続的な有料署名契約や外部担当者の審査待ちを前提としない同リポジトリーの方針を参考に、今回は未署名で公開しました。

開発機での起動成功は、すべての利用者環境での起動を保証しません。Windows の保護機能による制限について、今回別 PC での検証は実施していません。

## 文字コードと認証

- 日本語資料は UTF-8 で保存・読み込みます。Windows PowerShell の `Get-Content` は `-Encoding UTF8` を指定しないと、正常なファイルでも文字化けして見える場合があります。
- 今回、PowerShell から別プロセスの標準入力へ日本語を渡す経路で `?` になる事象がありました。書き込み後に読み直します。GitHub 本文は UTF-8 のファイルを `--notes-file` で渡します。
- 実行環境の制限下では GitHub 認証や NuGet 復元に失敗し、許可された通常環境で再実行すると成功しました。制限による失敗と認証情報の失効を切り分けます。トークンを表示・記録したり、検証を無効化して回避したりしません。

## 未検証事項

v0.1.0 は開発用 Windows 環境と ZIP の別フォルダー展開で確認しました。.NET 未導入のクリーン PC、Windows ARM64、macOS、Linux での実行検証は今回の記録に含みません。ランタイム同梱は発行設定と配布内容で確認しています。
