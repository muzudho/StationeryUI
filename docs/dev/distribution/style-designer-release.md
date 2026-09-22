# 繧ｹ繧ｿ繧､繝ｫ險ｭ險医ヤ繝ｼ繝ｫ縺ｮ繝ｪ繝ｪ繝ｼ繧ｹ謇矩・

繝ｪ繝昴ず繝医Μ繝ｼ縺ｮ繝ｫ繝ｼ繝医〒 PowerShell 繧帝幕縺・※螳溯｡後＠縺ｾ縺吶・it縲；itHub CLI縲∝ｯｾ雎｡繧偵ン繝ｫ繝峨〒縺阪ｋ .NET SDK 縺悟ｿ・ｦ√〒縺吶・UI 讀懈渊縺ｯ Windows 縺ｮ繝・せ繧ｯ繝医ャ繝励そ繝・す繝ｧ繝ｳ縺ｧ螳溯｡後＠縺ｾ縺吶・

## 1. 迚医→繧ｽ繝ｼ繧ｹ繧貞崋螳壹☆繧・

1. `samples/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.csproj` 縺ｮ `Version`縲～AssemblyVersion`縲～FileVersion` 繧呈峩譁ｰ縺励∪縺吶ゆｾ具ｼ啻0.1.1`縲～0.1.1.0`縲～0.1.1.0`縲・
2. 蛻ｩ逕ｨ閠・髄縺題ｳ・侭縲・幕逋ｺ譌･隱後～docs/user/releases/style-designer-v<迚・.md` 縺ｮ繝ｪ繝ｪ繝ｼ繧ｹ譛ｬ譁・ｒ譖ｴ譁ｰ縺励∪縺吶・
3. 閾ｪ蜍輔ユ繧ｹ繝医ｒ螳溯｡後＠縲∝ｯｾ雎｡縺ｮ螟画峩縺縺代ｒ繧ｳ繝溘ャ繝医＠縺ｾ縺吶ょ倶ｺｺ逕ｨ繝｡繝｢縺ｪ縺ｩ繧偵∪縺ｨ繧√※霑ｽ蜉縺励↑縺・〒縺上□縺輔＞縲・

```powershell
dotnet run --project tests/StationeryUI.Tests -c Release
git status --short
git rev-parse HEAD
```

繧ｳ繝溘ャ繝・SHA 繧定ｨ倬鹸縺励∪縺吶ら匱陦後せ繧ｯ繝ｪ繝励ヨ縺ｯ菴懈･ｭ繝輔か繝ｫ繝繝ｼ繧偵ン繝ｫ繝峨☆繧九◆繧√？EAD 縺ｮ險倬鹸縺縺代〒縺ｯ譛ｪ繧ｳ繝溘ャ繝亥､画峩縺ｮ豺ｷ蜈･繧帝亟縺偵∪縺帙ｓ縲ゅン繝ｫ繝牙ｯｾ雎｡縺ｨ蜷梧｢ｱ雉・侭縺後さ繝溘ャ繝亥・螳ｹ縺ｨ荳閾ｴ縺吶ｋ縺薙→繧堤｢ｺ隱阪＠縺ｾ縺吶・

## 2. 逋ｺ陦後☆繧・

莉･荳九・ **v0.1.0 縺ｮ蜀咲樟逕ｨ縺ｮ萓・*縺ｧ縺吶よｬ｡縺ｮ蜈ｬ髢九〒縺ｯ繧｢繝励Μ迚医→謗｡逕ｨ縺吶ｋ繝ｩ繝ｳ繧ｿ繧､繝迚医ｒ螟画峩縺励∪縺吶ょ・髢区ｸ医∩縺ｮ迚医・荳頑嶌縺阪＠縺ｾ縺帙ｓ縲・

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish-StyleDesigner.ps1 -Version 0.3.0 -RuntimeVersion 8.0.30
```

[逋ｺ陦後せ繧ｯ繝ｪ繝励ヨ](../../../scripts/Publish-StyleDesigner.ps1) 縺ｯ Release / win-x64 / self-contained 縺ｧ逋ｺ陦後＠縲￣DB 繧帝勁螟悶＠縺ｾ縺吶よ眠縺励＞譌･譎ゆｻ倥″繝輔か繝ｫ繝繝ｼ縺ｸ蜃ｺ蜉帙＠縲〇IP繝ｻSHA-256 繧剃ｽ懈・縲∝挨繝輔か繝ｫ繝繝ｼ縺ｸ螻暮幕縺励※蜈・ヵ繧｡繧､繝ｫ縺ｨ縺ｮ繝上ャ繧ｷ繝･荳閾ｴ縺ｾ縺ｧ讀懈渊縺励∪縺吶・UI 讀懈渊縺ｨ GitHub 蜈ｬ髢九・陦後＞縺ｾ縺帙ｓ縲・

`RELEASE_DIRECTORY` 縺ｫ陦ｨ遉ｺ縺輔ｌ繧句・蜉帛・繧定ｨ倬鹸縺励∪縺吶・

| 蜃ｺ蜉・| 逕ｨ騾・|
| --- | --- |
| `StationeryUI.StyleDesigner/` | 逋ｺ陦後＆繧後◆繧｢繝励Μ蜈ｨ菴・|
| `StationeryUI.StyleDesigner-v<迚・-win-x64.zip` | 豺ｻ莉倥☆繧矩・蟶・黄 |
| `SHA256SUMS.txt` | 豺ｻ莉倥☆繧九メ繧ｧ繝・け繧ｵ繝 |
| `extracted/StationeryUI.StyleDesigner/` | ZIP 螻暮幕蠕後・讀懈渊蟇ｾ雎｡ |
| `build-record.json` | SHA縲∫沿縲√Λ繝ｳ繧ｿ繧､繝縲√ヵ繧｡繧､繝ｫ謨ｰ縲√Ο繝ｼ繧ｫ繝ｫ繝代せ縺ｮ菴懈･ｭ險倬鹸縲よｷｻ莉伜ｯｾ雎｡螟・|

## 3. 驟榊ｸ・・螳ｹ縺ｨ GUI 繧呈､懈渊縺吶ｋ

[豕ｨ諢丈ｺ矩・(notes.md) 縺ｫ蠕薙＞縲√Λ繧､繧ｻ繝ｳ繧ｹ縺ｨ荳崎ｦ√ヵ繧｡繧､繝ｫ縺ｮ豺ｷ蜈･繧堤｢ｺ隱阪＠縺ｾ縺吶ら匱陦悟・縺ｨ ZIP 螻暮幕蜈医・荳｡譁ｹ縺ｧ讀懈渊縺励∪縺吶ゅせ繧ｯ繝ｪ繝励ヨ縺ｯ GUI 讀懈渊繧医ｊ蜈医↓ ZIP 繧剃ｽ懊ｊ縺ｾ縺吶′縲∵､懈渊縺梧ｸ医・縺ｾ縺ｧ縺ｯ蜈ｬ髢九＠縺ｾ縺帙ｓ縲・

`$releaseDir` 縺ｯ螳滄圀縺ｮ蜃ｺ蜉帛・縺ｸ鄂ｮ縺肴鋤縺医∪縺吶・

```powershell
$releaseDir = 'artifacts/release/style-designer-v0.1.0-YYYYMMDD-HHMMSS'
$publishedExe = Join-Path $releaseDir 'StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe'
$extractedExe = Join-Path $releaseDir 'extracted/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.exe'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $publishedExe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe -Existing -NativeDialog -Dark
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/StationeryUI.Windows.Tests/Test-StyleDesigner.ps1 -Executable $extractedExe -Existing -NativeDialog -CancelDialog
```

蜷・さ繝槭Φ繝峨′謌仙粥縺励※縺九ｉ谺｡縺ｸ騾ｲ縺ｿ縺ｾ縺吶よ眠隕丈ｽ懈・縲゛SON 蜃ｺ蜉帙∵里蟄倡ｷｨ髮・√Δ繝・Ν繝ｻbindings 縺ｮ菫晄戟縲＾S 縺ｮ繝輔ぃ繧､繝ｫ驕ｸ謚槭→繧ｭ繝｣繝ｳ繧ｻ繝ｫ繧堤｢ｺ隱阪＠縺ｾ縺吶Ｗ0.2.0 莉･髯阪・繝・せ繝育畑繧ｳ繝斐・繧剃ｽｿ縺・∝・蜀・ｮｹ縺ｮ .bak 騾驕ｿ縺ｨ蜈・ヱ繧ｹ縺ｸ縺ｮ繧ｪ繝ｼ繝医そ繝ｼ繝悶ｒ讀懆ｨｼ縺励∪縺吶Ａ-SavePoints` 縺ｧ繧ｿ繧､繝槭・菫晏ｭ倥→蠕ｩ蜈・～-LayoutEditing panel` 縺ｪ縺ｩ縺ｧ隕∫ｴ邱ｨ髮・ｂ遒ｺ隱阪＠縺ｾ縺吶よ律譛ｬ隱槭√・繧ｿ繝ｳ縲∵・證励ユ繝ｼ繝槭ｂ逕ｻ蜒上∪縺溘・螳溽判髱｢縺ｧ遒ｺ隱阪＠縺ｾ縺吶・

菫ｮ豁｣縺ｧ繧ｽ繝ｼ繧ｹ縺悟､峨ｏ縺｣縺溘ｉ繧ｳ繝溘ャ繝医→逋ｺ陦後°繧峨ｄ繧顔峩縺励∪縺吶よ､懈渊蠕後・繝輔か繝ｫ繝繝ｼ繧剃ｸ咲畑諢上↓蜀榊悸邵ｮ縺励※繝・せ繝亥・蜉帙ｒ豺ｷ蜈･縺輔○縺ｪ縺・〒縺上□縺輔＞縲・

## 4. GitHub 縺ｫ蜈ｬ髢九☆繧・

莉･荳九・蛟､繧剃ｻ雁屓縺ｮ險倬鹸縺ｸ鄂ｮ縺肴鋤縺医∝推繧ｳ繝槭Φ繝峨・螟ｱ謨玲凾縺ｫ縺ｯ荳ｭ譁ｭ縺励※縺上□縺輔＞縲ゅち繧ｰ繝ｻpush繝ｻ蜈ｬ髢九・蜈ｬ髢倶ｾ晞ｼ縺ｮ遽・峇蜀・〒螳溯｡後＠縺ｾ縺吶・

```powershell
$version = '0.1.0'
$tag = "style-designer-v$version"
$revision = '<讀懆ｨｼ縺励◆繧ｳ繝溘ャ繝医・螳悟・縺ｪ SHA>'
$zip = Join-Path $releaseDir "StationeryUI.StyleDesigner-v$version-win-x64.zip"
$checksum = Join-Path $releaseDir 'SHA256SUMS.txt'
gh auth status
git remote -v
git rev-parse HEAD
```

HEAD 縺ｨ蟇ｾ雎｡ SHA 縺御ｸ閾ｴ縺吶ｋ縺薙→縲√ち繧ｰ縺梧里蟄倥〒縺ｪ縺・％縺ｨ縲・∽ｿ｡蜈医′ `muzudho/StationeryUI` 縺ｧ縺ゅｋ縺薙→繧堤｢ｺ隱阪＠縺ｾ縺吶・

```powershell
git tag -a $tag $revision -m "Style Designer v$version"
git push --atomic origin HEAD:refs/heads/main "refs/tags/$tag"
```

push 謌仙粥蠕後↓蜈ｬ髢九＠縺ｾ縺吶・

```powershell
gh release create $tag $zip $checksum --repo muzudho/StationeryUI --verify-tag --title "繧ｹ繧ｿ繧､繝ｫ險ｭ險医ヤ繝ｼ繝ｫ v$version" --notes-file "docs/user/releases/style-designer-v$version.md" --latest
```

`--latest` 縺ｯ繝ｪ繝昴ず繝医Μ繝ｼ蜈ｨ菴薙・譛譁ｰ繝ｪ繝ｪ繝ｼ繧ｹ陦ｨ遉ｺ繧貞､画峩縺励∪縺吶ゅΛ繧､繝悶Λ繝ｪ繝ｼ縺ｨ蜈ｱ逕ｨ縺ｮ縺溘ａ豈主屓諢丞峙繧堤｢ｺ隱阪＠縺ｾ縺吶Ｗ0.1.0 縺ｧ縺ｯ險ｭ險医ヤ繝ｼ繝ｫ繧呈怙譁ｰ縺ｨ縺励※蜈ｬ髢九＠縺ｾ縺励◆縲・

譛ｬ譁・・鬆ｭ縺ｯ谺｡縺ｮ蠖｢蠑上→縺励∽ｻ雁屓縺ｮ繧ｿ繧ｰ繝ｻZIP 蜷阪∈逶ｴ謗･繝ｪ繝ｳ繧ｯ縺励∪縺吶・

```markdown
> [!IMPORTANT]
> 騾壼ｸｸ縺ｮ蛻ｩ逕ｨ縺ｫ縺ｯ縲、ssets 縺ｮ **[Windows x64 逕ｨ ZIP](莉雁屓縺ｮ ZIP 縺ｸ縺ｮ逶ｴ謗･繝ｪ繝ｳ繧ｯ)** 繧偵ム繧ｦ繝ｳ繝ｭ繝ｼ繝峨＠縺ｦ縺上□縺輔＞縲・

`Source code` 縺ｯ髢狗匱閠・髄縺代〒縺吶・
```

讖溯・縲∝ｯｾ雎｡遽・峇縲∬ｵｷ蜍墓婿豕輔√Λ繝ｳ繧ｿ繧､繝蜷梧｢ｱ縲∫ｽｲ蜷咲憾諷九∵､懆ｨｼ邨先棡縲∝茜逕ｨ謇矩・∈縺ｮ繝ｪ繝ｳ繧ｯ繧りｨ倩ｼ峨＠縺ｾ縺吶・蛻晏屓縺ｮ譛ｬ譁Ⅹ(../../user/releases/style-designer-v0.1.0.md) 繧貞盾閠・↓縺励※縺上□縺輔＞縲・

## 5. 蜈ｬ髢句ｾ後ｒ遒ｺ隱阪☆繧・

```powershell
gh release view $tag --repo muzudho/StationeryUI --json url,tagName,name,isDraft,isPrerelease,body,assets
git ls-remote origin "refs/tags/$tag" "refs/tags/$tag^{}"
Get-FileHash -LiteralPath $zip -Algorithm SHA256
```

- 譛ｬ譁・・ `[!IMPORTANT]` 縺ｨ繝ｪ繝ｳ繧ｯ縺御ｿ晄戟縺輔ｌ縲∵ｭ｣蠑丞・髢九〒縺ｯ draft / prerelease 縺・false 縺ｧ縺ゅｋ縺薙→縲・
- 豕ｨ驥井ｻ倥″繧ｿ繧ｰ縺ｮ `^{}` 縺梧､懆ｨｼ縺励◆繧ｳ繝溘ャ繝・SHA 縺ｨ荳閾ｴ縺吶ｋ縺薙→縲・
- ZIP 縺ｨ `SHA256SUMS.txt` 縺・uploaded 縺ｧ縲∝錐蜑阪・繧ｵ繧､繧ｺ縺御ｸ閾ｴ縺吶ｋ縺薙→縲・
- asset 縺ｮ `digest` 縺悟ｾ励ｉ繧後ｋ蝣ｴ蜷医・ ZIP 縺ｮ SHA-256 縺ｨ荳閾ｴ縺吶ｋ縺薙→縲ょ叙蠕励〒縺阪↑縺・ｴ蜷医・蜈ｬ髢・ZIP 繧貞挨縺ｮ蝣ｴ謇縺ｸ繝繧ｦ繝ｳ繝ｭ繝ｼ繝峨＠縺ｦ辣ｧ蜷医☆繧九％縺ｨ縲・

繧｢繝・・繝ｭ繝ｼ繝牙､ｱ謨玲凾縺ｯ縲√∪縺壽里蟄倥Μ繝ｪ繝ｼ繧ｹ縺ｨ豺ｻ莉倡憾諷九ｒ遒ｺ隱阪＠縺ｾ縺吶よ・蜉溘＠縺溘ち繧ｰ繧・ｷｻ莉倥ｒ辟｡譚｡莉ｶ縺ｫ菴懊ｊ逶ｴ縺輔★縲∝・髢区ｸ医∩縺ｮ蜀・ｮｹ菫ｮ豁｣縺悟ｿ・ｦ√↑繧画眠縺励＞迚医〒驟榊ｸ・＠縺ｾ縺吶らｵ先棡繧帝・蟶・ｨ倬鹸縺ｸ霑ｽ險倥＠縺ｾ縺吶・

