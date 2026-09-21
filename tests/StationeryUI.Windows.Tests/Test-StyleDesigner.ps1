param([string]$Executable = 'samples/StationeryUI.StyleDesigner/bin/Release/net8.0-windows/StationeryUI.StyleDesigner.exe', [switch]$Existing)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testDirectory = Join-Path $workspace ('artifacts/style-designer-test/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$previousOutput = $env:STATIONERYUI_DESIGNER_TEST_OUTPUT
$previousInput = $env:STATIONERYUI_DESIGNER_TEST_INPUT
$process = $null
try {
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $testDirectory
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = ''
    if ($Existing) {
        $source = Join-Path $workspace 'App_Data/demo.stationery-style.json'
        $sourceHash = (Get-FileHash -LiteralPath $source).Hash
        $env:STATIONERYUI_DESIGNER_TEST_INPUT = $source
    }
    $process = Start-Process -FilePath (Join-Path $workspace $Executable) -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw 'Style designer test timed out.' }
    if ($process.ExitCode -ne 0) { throw "Style designer failed: $($process.ExitCode)" }
    $json = Get-Content -LiteralPath (Join-Path $testDirectory 'plan.stationery-style.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($Existing) {
        $layout = $json.layouts | Where-Object id -eq 'topDemoLayout'
        if ($layout.'column-definitions'[0] -ne '2.5rate') { throw 'Imported edit missing.' }
        if ((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash) { throw 'Source file changed.' }
        $original = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
        if (($original.models | ConvertTo-Json -Depth 100 -Compress) -ne ($json.models | ConvertTo-Json -Depth 100 -Compress)) { throw 'Models changed.' }
        if (($original.bindings | ConvertTo-Json -Depth 100 -Compress) -ne ($json.bindings | ConvertTo-Json -Depth 100 -Compress)) { throw 'Bindings changed.' }
    } else {
        if ($json.layouts[0].'column-definitions'[0] -ne '1.5rate' -or $json.layouts[0].'column-definitions'[1] -ne '120px') { throw 'Track export mismatch.' }
        if ($json.models[0].children[0].children[0].type -ne 'button') { throw 'Cell type mismatch.' }
    }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'welcome.png'))) { throw 'Welcome screenshot missing.' }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'designer.png'))) { throw 'Screenshot missing.' }
    Write-Output "PASS designer cell editing, mixed units, JSON export and screenshot: $testDirectory"
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_DESIGNER_TEST_INPUT = $previousInput
}
