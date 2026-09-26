param([string]$Version = '0.4.0', [string]$RuntimeVersion = '8.0.31')
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $workspace
try {
    $revision = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine source revision.' }
    $project = [xml](Get-Content samples/StationeryUI.StyleDesigner/StationeryUI.StyleDesigner.csproj -Raw)
    if ($project.Project.PropertyGroup.Version -ne $Version) { throw 'Project version mismatch.' }
    $run = Join-Path $workspace ('artifacts/release/style-designer-v' + $Version + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
    $publish = Join-Path $run 'StationeryUI.StyleDesigner'
    if (Test-Path -LiteralPath $run) { throw 'Release output must be a new directory.' }
    New-Item -ItemType Directory -Path $run | Out-Null
    dotnet publish samples/StationeryUI.StyleDesigner -c Release -r win-x64 --self-contained true "-p:RuntimeFrameworkVersion=$RuntimeVersion" -p:DebugType=None -p:DebugSymbols=false "-p:SourceRevisionId=$revision" -o $publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    $exe = Join-Path $publish 'StationeryUI.StyleDesigner.exe'
    if ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -ne "$Version.0") { throw 'EXE version mismatch.' }
    $runtime = Get-Content (Join-Path $publish 'StationeryUI.StyleDesigner.runtimeconfig.json') -Raw | ConvertFrom-Json
    if (!$runtime.runtimeOptions.includedFrameworks -or $runtime.runtimeOptions.frameworks) { throw 'Expected a self-contained runtime.' }
    $required = @('coreclr.dll','hostfxr.dll','SDL2.dll','openal.dll','LICENSE','README.md','THIRD-PARTY-NOTICES.md','licenses/MonoGame-LICENSE.txt','licenses/OpenAL-LICENSE.txt','licenses/DotNet-LICENSE.txt')
    foreach ($name in $required) { if (!(Test-Path -LiteralPath (Join-Path $publish $name))) { throw "Missing distribution file: $name" } }
    foreach ($file in Get-ChildItem -LiteralPath $publish -Recurse -File) {
        if ($file.Extension -notin @('.dll','.exe','.json','.txt','.md','')) { throw "Unexpected distribution file: $($file.Name)" }
        if ($file.Extension -eq '.json' -and $file.Name -notin @('StationeryUI.StyleDesigner.deps.json','StationeryUI.StyleDesigner.runtimeconfig.json')) { throw 'Unexpected JSON in distribution.' }
        if ($file.Extension -eq '' -and $file.Name -ne 'LICENSE') { throw 'Unexpected extensionless file.' }
    }
    $zip = Join-Path $run "StationeryUI.StyleDesigner-v$Version-win-x64.zip"
    Compress-Archive -LiteralPath $publish -DestinationPath $zip -CompressionLevel Optimal
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath (Join-Path $run 'SHA256SUMS.txt') -Encoding ASCII
    $extract = Join-Path $run 'extracted'
    Expand-Archive -LiteralPath $zip -DestinationPath $extract
    $files = Get-ChildItem -LiteralPath $publish -Recurse -File
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($publish.Length + 1)
        $copy = Join-Path (Join-Path $extract 'StationeryUI.StyleDesigner') $relative
        if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $copy).Hash) { throw "ZIP mismatch: $relative" }
    }
    [pscustomobject]@{ Version = $Version; Revision = $revision; Runtime = $RuntimeVersion; Zip = $zip; Extracted = $extract; Files = $files.Count; SHA256 = $hash } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'build-record.json') -Encoding UTF8
    Write-Output "RELEASE_DIRECTORY=$run"
} finally { Pop-Location }
