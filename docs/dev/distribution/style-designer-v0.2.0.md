# スタイル設定エディター v0.2.0 の配布記録

対象：Windows x64、Release、self-contained、.NET 8.0.31。

- タグ：`style-designer-v0.2.0`
- 配布物：`StationeryUI.StyleDesigner-v0.2.0-win-x64.zip`、`SHA256SUMS.txt`
- EXE の FileVersion：`0.2.0.0`
- コア自動テスト：28件成功。
- ライブラリー版とは別管理。NuGet.orgへの公開は今回の対象外。

## 公開結果（2026-09-21 追記）

| 項目 | 実測・確認結果 |
| --- | --- |
| 公開先 | [スタイル設定エディター v0.2.0](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.2.0) |
| ソースSHA | `5977119ff2e776e19f8bf41a29ba97126474bef7` |
| ZIPサイズ | 72,013,253 bytes |
| 配布ファイル数 | 482 |
| 作業出力先 | `artifacts/release/style-designer-v0.2.0-20260921-223614` |
| 署名状態 | `NotSigned` |
| GitHubの状態 | draft=false、prerelease=false、両添付ともuploaded |

ZIP の SHA-256：

```text
3eb4b9b4513825268dedbd265053ded4e6caece4c7c8bcf40e793f9259162ba0
```

GitHub asset の digest とローカル SHA-256 の一致、注釈付きタグの参照先と上記ソースSHAの一致を確認した。README・開発日誌・版番号のコミットとタグを atomic push し、リポジトリーの最新リリースとして公開した。個人用チャットメモの未コミット変更は含めていない。

## 検証結果

- ソリューション全体のReleaseビルド：警告0、エラー0。
- コアテスト28件、Windows固有の入力・文字描画検査10件：成功。
- .NET 8.0.31 同梱、EXEのFileVersion、必要なライセンス・DLL、不要ファイルの混入を発行スクリプトで検査。
- ZIP展開後の全482ファイルと発行元のハッシュを照合：一致。
- 最終成果物のGUI検査9シナリオ：すべて成功。各出力は `artifacts/style-designer-test/` 内に保存し、ZIPには含めない。

| 検査 | 出力フォルダー |
| --- | --- |
| 発行先・新規作成 | `65f4d622a15448dcad5317554c11b5a0` |
| 展開先・新規作成／書き出し／モーダル | `45e8c0754f76495f843dac78a5e3d6ac` |
| 既存編集／OSファイル選択／ダークモード | `f8a01bd31c6f44e881449d0a93077c63` |
| OSファイル選択キャンセル | `da676bc7cb2d4bc4b1e563a7631d8e83` |
| タイマー保存／セーブポイント復元 | `6d2a4d3cbafd436594711a515b0a7206` |
| panel追加／余白編集 | `059fb0e3c299435aaf659081bcda377a` |
| floating-layout追加 | `de688c2bcbc44ae7bbd52db9b5db8f01` |
| 要素削除 | `73dad10fea7d4fd8bb05ed29743a2a73` |
| Id変更 | `a660501d213948e3b43950cc03f5a154` |

公開前の検査で、Id変更モーダルを閉じた直後のテストクリックが早すぎる問題を修正し、ソースをコミットしてZIPを再作成した。公開したのは上記の最終成果物のみ。明暗の画面とダイアログのPNGも確認した。

制限環境のNuGet復元でSSL認証エラー、GitHub参照で401が発生したため、許可された通常環境で再実行した。署名検証は無効化していない。スクリプト実行は既存手順どおりプロセス限定の `ExecutionPolicy Bypass` を使用した。

.NET未導入のクリーンPC、macOS、Linux、Windows ARM64での実行は未検証。

[リリース本文](../../user/releases/style-designer-v0.2.0.md) ／ [配布手順](style-designer-release.md)
