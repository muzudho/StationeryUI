$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $workspace ('artifacts/editor-read-json/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$style = Join-Path $output 'style.stationery-ui.json'
Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $style
$beforeHash = (Get-FileHash -LiteralPath $style).Hash
$beforeWrite = (Get-Item -LiteralPath $style).LastWriteTimeUtc
$previousOutput = $env:STATIONERYUI_EDITOR_TEST_OUTPUT
$previousJsonPath = $env:STATIONERYUI_EDITOR_TEST_JSON_PATH
$previousSelection = $env:STATIONERYUI_EDITOR_TEST_SELECT_PATH
$env:STATIONERYUI_EDITOR_TEST_OUTPUT = $output
$env:STATIONERYUI_EDITOR_TEST_JSON_PATH = '/modelTree'
$env:STATIONERYUI_EDITOR_TEST_SELECT_PATH = $null
try {
    $editor = Start-Process -FilePath (Join-Path $workspace 'samples/StationeryUIEditor/bin/Release/net8.0-windows/StationeryUIEditor.exe') `
        -ArgumentList @('--mode', 'read', '--file', ('"' + $style + '"')) -WindowStyle Hidden -PassThru
    if (!$editor.WaitForExit(20000)) { $editor.Kill(); throw 'Read JSON tree editor timed out.' }
    if ($editor.ExitCode -ne 0) { throw "Read JSON tree editor exited with $($editor.ExitCode)." }
    $report = Get-Content -LiteralPath (Join-Path $output 'read-report.json') -Raw | ConvertFrom-Json
    if (!$report.ReadOnly -or $report.HasSaveSession -or !$report.JsonTreeMode -or
        $report.JsonSelectedPath -ne '/modelTree' -or $report.SelectedPath -ne '/mdlDemo') {
        throw 'JSON tree did not select its corresponding model in read mode.'
    }
    if ((Get-FileHash -LiteralPath $style).Hash -ne $beforeHash -or
        (Get-Item -LiteralPath $style).LastWriteTimeUtc -ne $beforeWrite -or
        @(Get-ChildItem -LiteralPath $output -Filter '*.bak').Count -ne 0) {
        throw 'Read JSON tree changed the source or created a backup.'
    }
    Write-Output 'PASS read-only JSON tree selects corresponding model without writing files'
} finally {
    $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_EDITOR_TEST_JSON_PATH = $previousJsonPath
    $env:STATIONERYUI_EDITOR_TEST_SELECT_PATH = $previousSelection
}
