# スタイル設計ツールのリリース

## 成果物と版

設計ツールの Version は `samples/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.csproj` で管理する。ライブラリー側の Version とは独立して更新する。
GitHub のタイトルは `スタイル設計ツール v0.1.0`、タグは `style-designer-v0.1.0`。既存のライブラリー用 `v0.1.0` は変更しない。公開済みのタグや同版の配布物は差し替えない。

## 手順

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StyleDesigner.ps1` で、固定した HEAD からの発行、内容確認、ZIP・SHA256SUMS.txt の作成、展開した各ファイルのハッシュ照合まで実行する。出力された `extracted/StationeryUI.StyleDesigner` の EXE を使って下記の GUI 検査を行ってから公開する。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable <展開先のEXE>
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable <展開先のEXE> -Existing -NativeDialog -Dark
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable <展開先のEXE> -Existing -NativeDialog -CancelDialog
```

1. バージョン・開発日誌・利用手順を更新し、自動テストを実行する。リリースに含める変更だけをコミットして対象 SHA を固定する。
2. 新しい出力先へ Release / win-x64 / self-contained で publish する。PDB は含めない。ランタイム、MonoGame の依存 DLL、README、LICENSE、THIRD-PARTY-NOTICES、licenses を同梱する。
3. 配布内容に作業中の JSON、個人用メモ、ログ、証明書・秘密鍵、開発時の画像が混入していないか確認する。第三者の DLL／EXE に独自の署名を追加しない。公開用の証明書がない場合は自作アセンブリを未署名で配布し、その旨をリリース本文に記す。
4. 発行先のアプリを起動して検査し、フォルダー全体を ZIP にする。ZIP を別の空フォルダーへ展開し、展開先からも新規・既存編集の GUI 検査を行う。
5. ZIP の SHA-256 を作成する。GitHub のタグを対象コミットに固定し、ZIP とチェックサムを同じリリースに添付する。
6. 公開後に本文・タグの SHA・添付ファイル名とサイズを確認する。

発行例（使用するランタイム版は配布ごとに固定・記録する）：

```powershell
dotnet publish samples/StationeryUI.StyleDesigner -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=8.0.30 -p:DebugType=None -p:DebugSymbols=false -o artifacts/release/style-designer-v0.1.0/StationeryUI.StyleDesigner
```

ローカルの自己署名証明書は一般利用者の PC で信頼されるものではない。配布のために利用者へ証明書の信頼登録を要求しない。

## リリース本文の冒頭

CircleSpaceCoordinator の配布資料で指定されている形式に合わせ、必ず ZIP の直接リンクを置く。

```markdown
> [!IMPORTANT]
> 通常の利用には、Assets の **[Windows x64 用 ZIP](今回のタグと ZIP 名の直接リンク)** をダウンロードしてください。

`Source code` は開発者向けです。
```

v0.1.0 は .NET 8 ランタイムを含む Windows x64 配布。ZIP をフォルダーごと展開し、`StationeryUI.StyleDesigner.exe` を起動する。別途 .NET のインストールは不要。
