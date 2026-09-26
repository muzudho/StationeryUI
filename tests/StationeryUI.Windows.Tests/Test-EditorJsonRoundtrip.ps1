$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $workspace ('artifacts/editor-json-roundtrip/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$style = Join-Path $output 'style.stationery-ui.json'
Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $style
$beforeHash = (Get-FileHash -LiteralPath $style).Hash
$previousOutput = $env:STATIONERYUI_EDITOR_TEST_OUTPUT
$previousJsonPath = $env:STATIONERYUI_EDITOR_TEST_JSON_PATH
$previousRoundtrip = $env:STATIONERYUI_EDITOR_TEST_JSON_ROUNDTRIP
$env:STATIONERYUI_EDITOR_TEST_OUTPUT = $output
$env:STATIONERYUI_EDITOR_TEST_JSON_PATH = '/modelTree'
$env:STATIONERYUI_EDITOR_TEST_JSON_ROUNDTRIP = '1'
try {
    $editor = Start-Process -FilePath (Join-Path $workspace 'samples/StationeryUIEditor/bin/Release/net8.0-windows/StationeryUIEditor.exe') `
        -ArgumentList @('--mode', 'read', '--file', ('"' + $style + '"')) -WindowStyle Hidden -PassThru
    if (!$editor.WaitForExit(20000)) { $editor.Kill(); throw 'JSON roundtrip editor timed out.' }
    if ($editor.ExitCode -ne 0) { throw "JSON roundtrip editor exited with $($editor.ExitCode)." }
    $report = Get-Content -LiteralPath (Join-Path $output 'json-roundtrip-report.json') -Raw | ConvertFrom-Json
    if (!$report.EditJsonMode -or !$report.ReadJsonMode -or $report.ReadHasSaveSession -or
        ($report.EditJsonPath -join '/') -ne 'modelTree' -or ($report.ReadJsonPath -join '/') -ne 'modelTree') {
        throw 'JSON selection did not survive read/edit/read mode changes.'
    }
    if ((Get-FileHash -LiteralPath $style).Hash -ne $beforeHash) { throw 'JSON roundtrip changed the source file.' }
    Write-Output 'PASS JSON selection survives read/edit/read mode changes'
} finally {
    $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_EDITOR_TEST_JSON_PATH = $previousJsonPath
    $env:STATIONERYUI_EDITOR_TEST_JSON_ROUNDTRIP = $previousRoundtrip
}
