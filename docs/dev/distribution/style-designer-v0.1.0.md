# スタイル設計ツール v0.1.0 の配布記録

確認日：2026-09-21。公開後、利用者からもリリースの確認報告がありました。

| 項目 | 記録 |
| --- | --- |
| リリース | [スタイル設計ツール v0.1.0](https://github.com/muzudho/StationeryUI/releases/tag/style-designer-v0.1.0) |
| タグ | `style-designer-v0.1.0` |
| ソース SHA | `e216299902c57f87ead49a475cb88a4d8526a6d8` |
| 対象 | Windows x64、Release、self-contained |
| 同梱ランタイム | .NET 8.0.30 |
| EXE の FileVersion | `0.1.0.0` |
| 自作 EXE の署名状態 | `NotSigned` |
| ZIP | `StationeryUI.StyleDesigner-v0.1.0-win-x64.zip` |
| ZIP サイズ | 71,912,577 bytes |
| ZIP 内のファイル数 | 482 |
| 作業出力先 | `artifacts/release/style-designer-v0.1.0-20260921-180845` |

ZIP の SHA-256：

```text
66406f397d2564be406a5e280393a4c113ba57a46536a4ed08af38f14fafc034
```

GitHub に ZIP と `SHA256SUMS.txt` を添付し、アップロード後の ZIP の digest とローカル SHA-256 の一致を確認しました。draft / prerelease ともに false です。ライブラリー用の既存 `v0.1.0` は変更せず、設計ツールのタグと main を atomic push しました。

## 検証結果

- コア自動テスト：27 件成功。
- 発行フォルダーからの新規作成・編集・JSON 出力：成功。
- ZIP 展開後の 482 ファイルと元ファイルのハッシュ照合：成功。
- 展開先からの新規作成：成功。
- 展開先からの既存編集、ネイティブファイル選択、ダークモード、モデル・bindings の保持と元ファイルの非変更：成功。
- ネイティブファイル選択のキャンセル：成功。
- MonoGame.Framework.dll、SDL2.dll、openal.dll と元の NuGet パッケージのハッシュ照合：一致。
- 配布 JSON は deps と runtimeconfig の 2 ファイル。PDB、作業ログ、個人用資料、画像、証明書・秘密鍵の混入を検査。

最終 ZIP の GUI 検査出力は `artifacts/style-designer-test/` の下の次のフォルダーです。ローカル作業ファイルで、GitHub の添付物には含めていません。

| 検査 | フォルダー |
| --- | --- |
| 新規作成 | `6b0dcd7cb69842389a06b4560efd8bcf` |
| 既存編集・ファイル選択 | `ce658b256f2d413593652bc9632b6112` |
| キャンセル | `30f4adfa987b4e46b4d05c829ce1eeaf` |

[リリース本文](../../user/releases/style-designer-v0.1.0.md) と [開発日誌](../log/2026/09.md) も参照してください。将来の版では、この記録の数値を流用せず再取得します。
