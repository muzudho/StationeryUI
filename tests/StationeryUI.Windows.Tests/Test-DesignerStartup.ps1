param([string]$Configuration = 'Debug', [switch]$Pages, [switch]$DeletePage)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$directory = Join-Path $workspace ('artifacts/designer-startup/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $directory | Out-Null
$source = Join-Path $directory '日本語 空白.stationery-ui.json'
Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $source
$originalHash = (Get-FileHash -LiteralPath $source).Hash
$previousOutput = $env:STATIONERYUI_DESIGNER_TEST_OUTPUT
$previousInput = $env:STATIONERYUI_DESIGNER_TEST_INPUT
$previousPages = $env:STATIONERYUI_DESIGNER_TEST_PAGES
try {
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $directory
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = ''
    $env:STATIONERYUI_DESIGNER_TEST_PAGES = if ($DeletePage) { 'delete' } elseif ($Pages) { '1' } else { '' }
    # Exercise the copy selected by the demo launcher, including its runtime dependencies.
    $executable = Join-Path $workspace "samples/StationeryUI.Demo/bin/$Configuration/net8.0-windows/StationeryUI.StyleDesigner.exe"
    $process = Start-Process -FilePath $executable -ArgumentList ('"' + $source + '"') -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw 'Designer startup timed out.' }
    if ($process.ExitCode -ne 0) { throw "Designer startup failed. See $directory/error.txt" }
    if (!(Test-Path -LiteralPath (Join-Path $directory 'designer.png'))) { throw 'Screenshot missing.' }
    if ((Get-FileHash -LiteralPath "$source.1.bak").Hash -ne $originalHash) { throw 'Startup savepoint mismatch.' }
    if ($DeletePage) {
        if ((Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json).viewports[0].children.name -contains 'vTestPage') { throw 'Page was not deleted.' }
        Write-Output "PASS page add/delete dialog and autosave: $directory"
    } elseif ($Pages) {
        if (!(Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json).viewports[0].children.name.Contains('vTestPage')) { throw 'Page was not autosaved.' }
        Write-Output "PASS page dialog and autosave: $directory"
    } else {
        if ((Get-FileHash -LiteralPath $source).Hash -ne $originalHash) { throw 'Opening the file modified its contents.' }
        Write-Output "PASS command-line startup, Japanese/space path, editing page, savepoint, unchanged source: $directory"
    }
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = $previousInput
    $env:STATIONERYUI_DESIGNER_TEST_PAGES = $previousPages
}
