$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $workspace ('artifacts/editor-read-mode/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
$style = Join-Path $output 'style.stationery-ui.json'
Copy-Item -LiteralPath (Join-Path $workspace 'App_Data/demo.stationery-ui.json') -Destination $style
$beforeHash = (Get-FileHash -LiteralPath $style).Hash
$beforeWrite = (Get-Item -LiteralPath $style).LastWriteTimeUtc
$previousOutput = $env:STATIONERYUI_EDITOR_TEST_OUTPUT
$previousSelection = $env:STATIONERYUI_EDITOR_TEST_SELECT_PATH
$env:STATIONERYUI_EDITOR_TEST_OUTPUT = $output
$env:STATIONERYUI_EDITOR_TEST_SELECT_PATH = '/mdlDemo/mdlTopDemoPage/mdlBody/mdlNameField'
try {
    $editor = Start-Process -FilePath (Join-Path $workspace 'samples/StationeryUIEditor/bin/Release/net8.0-windows/StationeryUIEditor.exe') `
        -ArgumentList @('--mode', 'read', '--file', ('"' + $style + '"')) -WindowStyle Hidden -PassThru
    if (!$editor.WaitForExit(20000)) { $editor.Kill(); throw 'Read-mode editor timed out.' }
    if ($editor.ExitCode -ne 0) { throw "Read-mode editor exited with $($editor.ExitCode)." }
    $report = Get-Content -LiteralPath (Join-Path $output 'read-report.json') -Raw | ConvertFrom-Json
    if (!$report.ReadOnly -or $report.HasSaveSession -or !$report.HasInspection -or !$report.HasPreview -or
        $report.SelectedPath -ne $env:STATIONERYUI_EDITOR_TEST_SELECT_PATH) {
        throw 'Read mode opened an edit session or missed the tree/preview.'
    }
    if ((Get-FileHash -LiteralPath $style).Hash -ne $beforeHash) { throw 'Read mode changed the file.' }
    if ((Get-Item -LiteralPath $style).LastWriteTimeUtc -ne $beforeWrite) { throw 'Read mode changed the timestamp.' }
    if (Get-ChildItem -LiteralPath $output -Filter '*.bak') { throw 'Read mode created a backup.' }
    Write-Output 'PASS standalone read mode has inspection and makes no file or backup changes'
} finally {
    $env:STATIONERYUI_EDITOR_TEST_OUTPUT = $previousOutput
    $env:STATIONERYUI_EDITOR_TEST_SELECT_PATH = $previousSelection
}
