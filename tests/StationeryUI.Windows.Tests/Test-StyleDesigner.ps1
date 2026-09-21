param([string]$Executable = 'samples/StationeryUI.StyleDesigner/bin/Release/net8.0-windows/StationeryUI.StyleDesigner.exe')
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testDirectory = Join-Path $workspace ('artifacts/style-designer-test/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null
$previousOutput = $env:STATIONERYUI_DESIGNER_TEST_OUTPUT
$process = $null
try {
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $testDirectory
    $process = Start-Process -FilePath (Join-Path $workspace $Executable) -WindowStyle Hidden -PassThru
    if (!$process.WaitForExit(30000)) { throw 'Style designer test timed out.' }
    if ($process.ExitCode -ne 0) { throw "Style designer failed: $($process.ExitCode)" }
    $json = Get-Content -LiteralPath (Join-Path $testDirectory 'plan.stationery-style.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($json.layouts[0].'column-definitions'[0] -ne '1.5rate' -or $json.layouts[0].'column-definitions'[1] -ne '120px') { throw 'Track export mismatch.' }
    if ($json.models[0].children[0].children[0].type -ne 'button') { throw 'Cell type mismatch.' }
    if (!(Test-Path -LiteralPath (Join-Path $testDirectory 'designer.png'))) { throw 'Screenshot missing.' }
    Write-Output "PASS designer cell editing, mixed units, JSON export and screenshot: $testDirectory"
} finally {
    if ($process -and !$process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:STATIONERYUI_DESIGNER_TEST_OUTPUT = $previousOutput
}
